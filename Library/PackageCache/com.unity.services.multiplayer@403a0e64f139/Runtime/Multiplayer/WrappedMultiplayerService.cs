using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.Multiplayer
{
    class WrappedMultiplayerService : IMultiplayerService
    {
        public event Action<ISession> SessionAdded
        {
            add => m_SessionManager.SessionAdded += value;
            remove => m_SessionManager.SessionAdded -= value;
        }

        public event Action<ISession> SessionRemoved
        {
            add => m_SessionManager.SessionRemoved += value;
            remove => m_SessionManager.SessionRemoved -= value;
        }

        IReadOnlyDictionary<string, ISession> IMultiplayerService.Sessions => m_SessionManager.Sessions;

        readonly ISessionQuerier m_SessionQuerier;
        readonly ISessionManager m_SessionManager;
        readonly IMatchmakerManager m_MatchmakerManager;
        readonly IModuleRegistry m_ModuleRegistry;

        internal WrappedMultiplayerService(
            ISessionQuerier sessionQuerier,
            ISessionManager sessionManager,
            IMatchmakerManager matchmakerManager,
            IModuleRegistry moduleRegistry)
        {
            m_SessionQuerier = sessionQuerier;
            m_SessionManager = sessionManager;
            m_MatchmakerManager = matchmakerManager;
            m_ModuleRegistry = moduleRegistry;
        }

        public async Task<IHostSession> CreateSessionAsync(SessionOptions sessionOptions)
        {
            var sessionHandler = await TryHandleSessionCreationException(
                () => m_SessionManager.CreateAsync(sessionOptions));
            return sessionHandler.AsHost();
        }

        public Task<ISession> CreateOrJoinSessionAsync(string sessionId, SessionOptions sessionOptions)
        {
            return TryHandleSessionCreationException(
                () => m_SessionManager.CreateOrJoinAsync(sessionId, sessionOptions));
        }

        public Task<ISession> JoinSessionByIdAsync(string sessionId, JoinSessionOptions sessionOptions)
        {
            return TryHandleSessionCreationException(
                () => m_SessionManager.JoinByIdAsync(sessionId, sessionOptions));
        }

        public Task<ISession> JoinSessionByCodeAsync(string sessionCode, JoinSessionOptions sessionOptions)
        {
            return TryHandleSessionCreationException(
                () => m_SessionManager.JoinByCodeAsync(sessionCode, sessionOptions));
        }

        public Task<ISession> ReconnectToSessionAsync(string sessionId, ReconnectSessionOptions options = default)
        {
            return TryHandleSessionCreationException(
                () => m_SessionManager.ReconnectAsync(sessionId, options));
        }

        public Task<ISession> MatchmakeSessionAsync(QuickJoinOptions quickJoinOptions, SessionOptions sessionOptions)
        {
            return TryHandleSessionCreationException(
                () =>  m_SessionManager.QuickJoinAsync(quickJoinOptions, sessionOptions));
        }

        public Task<ISession> MatchmakeSessionAsync(MatchmakerOptions matchOptions, SessionOptions sessionOptions, CancellationToken cancellationToken = default)
        {
            return TryHandleSessionCreationException(() =>
            {
                if (matchOptions == null)
                {
                    throw new SessionException("MatchmakerOptions cannot be null.", SessionError.InvalidMatchmakerOptions);
                }

                AddMatchmakerInitializationOptionIfMissing(sessionOptions);
                return m_MatchmakerManager.StartAsync(matchOptions, sessionOptions, cancellationToken);
            });
        }

        public Task<QuerySessionsResults> QuerySessionsAsync(QuerySessionsOptions queryOptions)
        {
            try
            {
                return m_SessionQuerier.QueryAsync(queryOptions);
            }
            catch (Exception e) when (e is not SessionException)
            {
                throw new SessionException(e.Message, SessionError.Unknown);
            }
        }

        public async Task<List<string>> GetJoinedSessionIdsAsync()
        {
            try
            {
                return await m_SessionManager.GetJoinedSessionIdsAsync();
            }
            catch (Exception e) when (e is not SessionException)
            {
                throw new SessionException(e.Message, SessionError.Unknown);
            }
        }

        /// <summary>
        /// Register a module provider to the Multiplayer Service.
        /// </summary>
        /// <param name="moduleProvider">The provider to register.</param>
        /// <typeparam name="T">The type of the provider implementing the
        /// <see cref="IModuleProvider"/> interface.</typeparam>
        /// <exception cref="SessionException">Throws with error code
        /// <see cref="SessionError.Unknown"/> if the module cannot be
        /// registered.</exception>
        /// <remarks>Any and all exceptions raised will be wrapped in a
        /// <see cref="SessionException"/>.</remarks>
        public void RegisterModuleProvider<T>(T moduleProvider) where T : IModuleProvider
        {
            try
            {
                m_ModuleRegistry.RegisterModuleProvider(moduleProvider);
            }
            catch (Exception e) when (e is not SessionException)
            {
                throw new SessionException(e.Message, SessionError.Unknown);
            }
        }

        /// <summary>
        /// Adds the <see cref="MatchmakerInitializationOption"/> if it is
        /// missing.
        /// </summary>
        /// <param name="sessionOptions">The current session options.</param>
        /// <remarks>
        /// The <see cref="MatchmakerInitializationOption"/> could have been
        /// already added to allow backfill, but if it is missing, it must be
        /// added to fetch the <see
        /// cref="Unity.Services.Matchmaker.Models.StoredMatchmakingResults"/>.
        /// </remarks>
        private static void AddMatchmakerInitializationOptionIfMissing(SessionOptions sessionOptions)
        {
            var matchmakerInitializationOption = sessionOptions.GetOption<MatchmakerInitializationOption>();
            if (matchmakerInitializationOption != null)
            {
                return;
            }

            var connectionOption = sessionOptions.GetOption<ConnectionOption>();
            if (connectionOption == null)
            {
                sessionOptions.WithOption(new MatchmakerInitializationOption());
            }
            else
            {
                var endpoint = $"{connectionOption.Options.Ip}:{connectionOption.Options.Port}";
                sessionOptions.WithOption(new MatchmakerInitializationOption(
                    false, endpoint));
            }
        }

        static Task<ISession> TryHandleSessionCreationException(Func<Task<ISession>> task)
        {
            return TryHandleSessionCreationException(WrapTask);

            async Task<SessionHandler> WrapTask()
            {
                return await task() as SessionHandler;
            }
        }

        static async Task<ISession> TryHandleSessionCreationException(Func<Task<SessionHandler>> task)
        {
            try
            {
                return await task();
            }
            catch (SessionException)
            {
                // rethrow
                throw;
            }
            catch (Exception e)
            {
                // in the case of aggregated exceptions or other exception types
                throw new SessionException(e.Message, SessionError.Unknown);
            }
        }
    }
}
