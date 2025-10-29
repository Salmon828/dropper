using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.Core.Scheduler.Internal;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine;
using Debug = Unity.Services.Multiplayer.Logger;

namespace Unity.Services.Multiplayer
{
    interface IMultiplayHandler
    {
        public event Action<bool> PlayerReadinessChanged;
        public Task InitServerEventsAsync(MultiplayEventCallbacks callbacks);
        public Task<MatchmakingResults> GetMatchmakingResultsAsync();
        public Task<IServerQueryHandler> StartServerQueryHandlerAsync(MultiplayServerOptions options, ushort maxPlayers);
        public Task SetPlayerReadinessAsync(bool isReady);

        /// <summary>
        /// Gets the payload allocation.
        /// If the payload allocation is a string, it will return the plain text string.
        /// Otherwise, serialises the payload allocation as JSON, and marshalls it to the given type TPayload.
        /// </summary>
        /// <typeparam name="TPayload">The object to be deserialized as.</typeparam>
        /// <returns>An object representing the payload allocation.</returns>
        public Task<TPayload> GetAllocationPayloadAsync<TPayload>();

        public ServerConfig ServerConfig { get; }

        public bool IsReadyForPlayers { get; }

        public void PlayerCountChanged(ushort newPlayerCount);
    }

    class MultiplayHandler : IMultiplayHandler
    {
        public ServerConfig ServerConfig => m_MultiplayService?.ServerConfig;

        public bool IsReadyForPlayers { get; internal set; }

        public event Action<bool> PlayerReadinessChanged;

        private CancellationToken Token => m_TokenSource.Token;

        private CancellationTokenSource m_TokenSource;
        private IServerEvents m_ServerEvents;
        private IServerQueryHandler m_ServerQueryHandler;
        private MultiplayServerOptions m_MultiplayServerOptions;

        private readonly IMultiplayService m_MultiplayService;
        private readonly IActionScheduler m_ActionScheduler;

        internal MultiplayHandler(
            IMultiplayService multiplayService,
            IActionScheduler actionScheduler
        )
        {
            m_MultiplayService = multiplayService;
            m_ActionScheduler = actionScheduler;
        }

        public async Task InitServerEventsAsync(MultiplayEventCallbacks callbacks)
        {
            m_ServerEvents = await m_MultiplayService.SubscribeToServerEventsAsync(callbacks);
        }

        public async Task<MatchmakingResults> GetMatchmakingResultsAsync()
        {
            try
            {
                return await m_MultiplayService.GetPayloadAllocationFromJsonAs<MatchmakingResults>();
            }
            catch (JsonException)
            {
                throw new SessionException("Failed to parse matchmaking results", SessionError.InvalidMatchmakerResults);
            }
            catch (Exception e)
            {
                throw new SessionException(e.Message, SessionError.Unknown);
            }
        }

        private async Task ServerQueryHandlerBackgroundLoopAsync()
        {
            // note(luke): running the UpdateServerCheck loop on a separate thread and then using ContinueWith on
            // main thread context for Debug. Here we link the Application.exitCancellationToken to ensure we stop
            // the thread when the application is quitting.
            var internalTokenSource = new CancellationTokenSource();
            Application.wantsToQuit += () =>
            {
                Debug.LogVerbose("Application.wantsToQuit received, cancelling ServerQueryHandler loop");
                internalTokenSource.Cancel();
                return true;
            };
            m_TokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(internalTokenSource.Token,
                    Application.exitCancellationToken);

            // Save the current synchronization context so we can call the Logger
            var currentContext = TaskScheduler.FromCurrentSynchronizationContext();
            var taskAfterFirstDelay = await Task.Factory.StartNew(async() =>
            {
                while (true)
                {
                    UpdateServerCheck();
                    // note(luke): we need to call this more often than once per second to prompt the handler to deal
                    // with any incoming request packets. Ensure the delay here is sub 1 second, to ensure that incoming
                    // packets are not dropped.
                    // (ref: https://forum.unity.com/threads/allocated-server-had-issues-query-failure-down-stopping-server-and-deallocating.1375152/#post-8916770)
                    await Task.Delay(100, Token);
                }
                // ReSharper disable once FunctionNeverReturns
            }, Token, TaskCreationOptions.None, TaskScheduler.Default);

            // This continuation will only be executed if the task throws but NOT when canceled.
            await taskAfterFirstDelay.ContinueWith(task =>
            {
                Debug.LogException(task.Exception);
            }, Token, TaskContinuationOptions.OnlyOnFaulted, currentContext);
        }

        public async Task<IServerQueryHandler> StartServerQueryHandlerAsync(MultiplayServerOptions options, ushort maxPlayers)
        {
            m_MultiplayServerOptions = options;
            m_MultiplayServerOptions.WithMaxPlayers(maxPlayers);

            m_ServerQueryHandler = await m_MultiplayService.StartServerQueryHandlerAsync(
                maxPlayers,
                options.ServerName,
                options.GameType,
                options.BuildId,
                options.Map
            );

            // This task wraps a background thread and therefore should not be awaited.
            _ = ServerQueryHandlerBackgroundLoopAsync();

            return m_ServerQueryHandler;
        }

        /// <summary>
        /// Gets the payload allocation, in JSON, and deserializes it as the given object.
        /// </summary>
        /// <typeparam name="TPayload">The object to be deserialized as.</typeparam>
        /// <param name="throwOnMissingMembers">Throws an exception if the given class is missing a member.</param>
        /// <returns>An object representing the payload allocation.</returns>
        public async Task<TPayload> GetAllocationPayloadAsync<TPayload>()
        {
            if (typeof(TPayload) == typeof(string))
            {
                object result = await m_MultiplayService.GetPayloadAllocationAsPlainText();
                return (TPayload)result;
            }
            return await m_MultiplayService.GetPayloadAllocationFromJsonAs<TPayload>();
        }

        void UpdateServerCheck()
        {
            if (m_MultiplayServerOptions.HasChanged())
            {
                m_MultiplayServerOptions.UpdateQueryHandler(m_ServerQueryHandler);
                m_MultiplayServerOptions.ResetChanged();
            }
            m_ServerQueryHandler.UpdateServerCheck();
        }

        public void PlayerCountChanged(ushort newPlayerCount)
        {
            m_MultiplayServerOptions.WithCurrentPlayers(newPlayerCount);
        }

        public async Task SetPlayerReadinessAsync(bool isReady)
        {
            if (isReady != IsReadyForPlayers)
            {
                if (isReady)
                {
                    await m_MultiplayService.ReadyServerForPlayersAsync();
                }
                else
                {
                    await m_MultiplayService.UnreadyServerAsync();
                }
                IsReadyForPlayers = isReady;
                PlayerReadinessChanged?.Invoke(IsReadyForPlayers);
            }
        }
    }
}
