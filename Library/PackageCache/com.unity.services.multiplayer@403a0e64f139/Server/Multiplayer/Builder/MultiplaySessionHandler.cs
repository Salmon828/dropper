using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine;

namespace Unity.Services.Multiplayer
{
    /// <summary>
    /// Provides methods to manage a Game Server Hosting server.
    /// </summary>
    public interface IMultiplaySessionManager
    {
        /// <summary>
        /// The server allocation information.
        /// </summary>
        public ServerAllocation Allocation { get; }

        /// <summary>
        /// The server ID.
        /// Null when the server is not allocated.
        /// </summary>
        public long? ServerId { get; }

        /// <summary>
        /// The Server Query Protocol Port.
        /// Null when the server is not allocated.
        /// </summary>
        public ushort? QueryPort { get; }

        /// <summary>
        /// The connection port for the session.
        /// Null when the server is not allocated.
        /// </summary>
        public ushort? ConnectionPort { get; }

        /// <summary>
        /// The connection IP for the session.
        /// Null when the server is not allocated.
        /// </summary>
        [CanBeNull]
        public string IpAddress { get; }

        /// <summary>
        /// The directory on the server Multiplay will write logs to.
        /// Null when the server is not allocated.
        /// </summary>
        [CanBeNull]
        public string ServerLogDirectory { get; }

        /// <summary>
        /// Gets the payload allocation, in JSON, and deserializes it as the given object.
        /// </summary>
        /// <typeparam name="TPayload">The object to be deserialized as.</typeparam>
        /// <returns>An object representing the payload allocation.</returns>
        public Task<TPayload> GetAllocationPayloadFromJsonAsAsync<TPayload>();

        /// <summary>
        /// Gets the payload allocation as plain text.
        /// </summary>
        /// <returns>The payload allocation as plain text.</returns>
        public Task<string> GetAllocationPayloadFromPlainTextAsync();

        /// <summary>
        /// Set the server as ready to receive players.
        /// </summary>
        /// <param name="isReady">True if the server is ready to receive players, false otherwise.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task SetPlayerReadinessAsync(bool isReady);

        /// <summary>
        /// Whether the server is ready to accept players.
        /// </summary>
        /// <seealso cref="SetPlayerReadinessAsync"/>
        public bool IsReadyForPlayers { get; }

        /// <summary>
        /// Current state of the Multiplay session handler.
        /// </summary>
        public MultiplaySessionManagerState State { get; }

        /// <summary>
        /// The Session being hosted by this server.
        /// </summary>
        public IServerSession Session { get; }
    }

    internal interface IMultiplaySessionManagerInternal : IMultiplaySessionManager
    {
        public Task<IMultiplaySessionManager> StartMultiplaySessionHandlerAsync(MultiplaySessionManagerOptions options);
    }

    class MultiplaySessionHandler : IMultiplaySessionManagerInternal
    {
        public ServerAllocation Allocation { get; internal set; }

        public MultiplaySessionManagerState State
        {
            get => m_State;
            internal set
            {
                m_State = value;
                m_MultiplaySessionManagerEventCallbacks?.InvokeStateChanged(value);
            }
        }

        public bool IsReadyForPlayers => m_GameServerHostingHandler.IsReadyForPlayers;

        ServerConfig _mServerConfig => m_GameServerHostingHandler?.ServerConfig;

        MultiplayAllocation MultiplayAllocation
        {
            get => m_MultiplayAllocation;
            set
            {
                m_MultiplayAllocation = value;
                if (value != null)
                    Allocation = new ServerAllocation(m_MultiplayAllocation);
            }
        }

        public long? ServerId => _mServerConfig?.ServerId;
        public ushort? QueryPort => _mServerConfig?.QueryPort;
        public ushort? ConnectionPort => _mServerConfig?.Port;
        public string IpAddress => _mServerConfig?.IpAddress;
        public string ServerLogDirectory => _mServerConfig?.ServerLogDirectory;

        MultiplayAllocation m_MultiplayAllocation;
        MultiplaySessionManagerState m_State;
        MultiplaySessionManagerOptions m_SessionManagerOptions;
        IServerEvents m_MultiplayEvents;
        MultiplaySessionManagerEventCallbacks m_MultiplaySessionManagerEventCallbacks => m_SessionManagerOptions?.Callbacks;

        string m_ConnectionString => $"{IpAddress}:{ConnectionPort}";

        readonly IMultiplayHandler m_GameServerHostingHandler;
        readonly IMatchmakerService m_MatchmakerService;
        readonly IMultiplayService m_MultiplayService;
        readonly ISessionManager m_SessionManager;

        IServerSession m_Session;
        MatchmakingResults m_MatchmakingResults;
        // TODO: should we surface this to users?
        IServerQueryHandler m_ServerQueryHandler;

        public IServerSession Session
        {
            get
            {
                if (State != MultiplaySessionManagerState.Allocated)
                {
                    throw new SessionException(
                        "The session has not been initialized yet. Please use MultiplaySessionManagerEventCallbacks.Allocated callback to access the initialized session.",
                        SessionError.InvalidOperation);
                }

                return m_Session;
            }
            internal set => m_Session = value;
        }

        internal MultiplaySessionHandler(IMultiplayService multiplayService, ISessionManager sessionManager, IMultiplayHandler gameServerHostingHandler, IMatchmakerService matchmakerService)
        {
            m_MultiplayService = multiplayService;
            m_SessionManager = sessionManager;
            m_MatchmakerService = matchmakerService;

            m_GameServerHostingHandler = gameServerHostingHandler;

            State = MultiplaySessionManagerState.Uninitialized;

            m_GameServerHostingHandler.PlayerReadinessChanged += isReady =>
            {
                m_MultiplaySessionManagerEventCallbacks?.InvokePlayerReadinessChanged(isReady);
            };
        }

        MultiplayEventCallbacks MultiplayEventCallbacks()
        {
            var callbacks = new MultiplayEventCallbacks();
            callbacks.Allocate += OnAllocate;
            callbacks.Deallocate += OnDeallocate;
            callbacks.Error += e => throw new SessionException(e.Detail, SessionError.MultiplayServerError);
            return callbacks;
        }

        public async Task<IMultiplaySessionManager> StartMultiplaySessionHandlerAsync(MultiplaySessionManagerOptions options)
        {
            Logger.LogVerbose("StartMultiplaySessionHandlerAsync");

            m_SessionManagerOptions = options;
            try
            {
                await m_GameServerHostingHandler.InitServerEventsAsync(MultiplayEventCallbacks());

                Logger.LogVerbose("starting server query handler");
                m_ServerQueryHandler = await m_GameServerHostingHandler
                    .StartServerQueryHandlerAsync(m_SessionManagerOptions.MultiplayServerOptions, (ushort)m_SessionManagerOptions.SessionOptions.MaxPlayers);
            }
            catch (Exception e) when (e is not SessionException)
            {
                Logger.LogVerbose($"GameServerHostingHandler is valid? {m_GameServerHostingHandler != null}");

                throw new SessionException(e.Message, SessionError.MultiplayServerError);
            }

            State = MultiplaySessionManagerState.AwaitingAllocation;

            return this;
        }

        async Task<IServerSession> CreatePlayerSessionAsync()
        {
            Logger.LogVerbose("CreatePlayerSessionAsync");

            if (MultiplayAllocation == null)
            {
                throw new SessionException("player session can only be created when the server is allocated", SessionError.Forbidden);
            }

            // Multiplay: try to get Matchmaking Results
            var lobbyId = MultiplayAllocation.AllocationId;
            try
            {
                // if this was a matchmaker allocation, use the match ID as lobby ID
                m_MatchmakingResults = await m_GameServerHostingHandler.GetMatchmakingResultsAsync();
                lobbyId = m_MatchmakingResults.MatchId;

                Allocation.IsMatchmakerAllocation = true;
            }
            catch (JsonException)
            {
                throw new SessionException("Failed to parse matchmaking results", SessionError.InvalidMatchmakerResults);
            }
            catch (Exception e)
            {
                // otherwise, proceed as a non-matchmaker allocation with the allocation ID as lobby ID
                Logger.LogVerbose($"failed to get matchmaking results from payload allocation, so proceeding " +
                    $"as a non-matchmaker allocation. Error: {e.Message}");
            }

            var connectionOption = m_SessionManagerOptions.SessionOptions.GetOption<ConnectionOption>();
            if (connectionOption?.Options.Network == NetworkType.Direct)
            {
                // Multiplay: override session connection metadata
                // TODO: throw if port is not defined?
                connectionOption.Options.OverrideDirectConnectionInfo(IpAddress, ConnectionPort.Value, "0.0.0.0");
            }

            if (Allocation.IsMatchmakerAllocation)
            {
                // backfilling allowed is != enabled !!!
                m_SessionManagerOptions.SessionOptions.AllowBackfilling();
            }

            try
            {
                Session = (await m_SessionManager.CreateOrJoinAsync(lobbyId, m_SessionManagerOptions.SessionOptions))
                    .AsServer();
            }
            catch (Exception sessionCreationException)
            {
                if (Allocation.IsMatchmakerAllocation)
                {
                    await m_MultiplayService.UnreadyServerAsync();

                    // disable any ongoing backfilling using the ticket id from the matchmaker allocation
                    await m_MatchmakerService.DeleteBackfillTicketAsync(m_MatchmakingResults.BackfillTicketId);

                    // stop players from joining
                    m_ServerQueryHandler.MaxPlayers = 0;

                    try
                    {
                        // dispose server connection
                        m_ServerQueryHandler.Dispose();
                    }
                    catch (Exception disposalException)
                    {
                        Logger.LogError("Error while disposing the ");
                        Logger.LogException(disposalException);
                    }
                }
                if (sessionCreationException is SessionException { Error : SessionError.InvalidSessionIdentifier })
                {
                    Logger.LogException(sessionCreationException);
                    Logger.LogError($"The server is not able to allocate a new session with the given {(Allocation.IsMatchmakerAllocation ? "Matchmaker MatchId" : "Multiplay AllocationId")}. \r\n" +
                        "Each session must have a unique identifier, please ensure that there is no lingering Sessions in your hosted Fleet matching the id below!\r\n" +
                        $"Conflicting AllocationId: :{lobbyId}");

                    // Quit will shut the server down, and the client will time out eventually
                    Application.Quit((int)SessionError.InvalidSessionIdentifier);
                    return null;
                }

                // re-throw the exception if it has not triggered an application shutdown
                throw;
            }

            Logger.LogVerbose($"Session {Session.Id} created successfully.");

            Session.PlayerJoined += playerId =>
            {
                Logger.LogVerbose($"PlayerJoined event fired. Setting player count to {Session.Players.Count}");

                // Update SQP and matchmaker with new player data
                m_GameServerHostingHandler.PlayerCountChanged((ushort)Session.Players.Count);
            };

            Session.PlayerLeaving += playerId =>
            {
                Logger.LogVerbose($"PlayerLeaving event fired. Setting player count to {Session.Players.Count}");

                // Update SQP and matchmaker with new player data
                m_GameServerHostingHandler.PlayerCountChanged((ushort)Session.Players.Count);
            };

            // Multiplay: mark the server as ready to accept new players if auto-ready is enabled. If not, the caller can handle
            // readiness with SetPlayerReadiness.
            if (m_SessionManagerOptions.MultiplayServerOptions.AutoReady)
            {
                Logger.LogVerbose($"{DateTime.UtcNow} Server is Ready for Players.");
                await SetPlayerReadinessAsync(true);
            }

            return Session;
        }

        public async Task SetPlayerReadinessAsync(bool isReady)
        {
            await m_GameServerHostingHandler.SetPlayerReadinessAsync(isReady);
        }

        public async Task<TPayload> GetAllocationPayloadFromJsonAsAsync<TPayload>()
        {
            try
            {
                return await m_MultiplayService.GetPayloadAllocationFromJsonAs<TPayload>();
            }
            catch (Exception e)
            {
                throw new SessionException(e.Message, SessionError.MultiplayServerError);
            }
        }

        public async Task<string> GetAllocationPayloadFromPlainTextAsync()
        {
            try
            {
                return await m_MultiplayService.GetPayloadAllocationAsPlainText();
            }
            catch (Exception e)
            {
                throw new SessionException(e.Message, SessionError.MultiplayServerError);
            }
        }

        async void OnAllocate(MultiplayAllocation alloc)
        {
            Logger.LogVerbose($"server {alloc.ServerId} allocated with allocation {alloc.AllocationId}");

            // TODO: revisit the possible null reference
            // Update connection port
            m_ServerQueryHandler.Port = ConnectionPort.Value;

            MultiplayAllocation = alloc;
            State = MultiplaySessionManagerState.Allocated;
            await CreatePlayerSessionAsync();

            m_MultiplaySessionManagerEventCallbacks?.InvokeAllocate(alloc);
        }

        async void OnDeallocate(MultiplayDeallocation dealloc)
        {
            Logger.LogVerbose($"server {dealloc.ServerId} deallocated from allocation {dealloc.AllocationId}");

            MultiplayAllocation = null;

            // if the server is still ready for players, unready it
            if (IsReadyForPlayers)
                await SetPlayerReadinessAsync(false);

            await Session.AsHost().DeleteAsync();
            m_MultiplaySessionManagerEventCallbacks?.InvokeDeallocate(dealloc);
        }
    }
}
