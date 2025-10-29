using System.Threading.Tasks;

namespace Unity.Services.Multiplayer
{
    class MultiplayerServerServiceImpl : IMultiplayerServerService
    {
        readonly SessionManager m_SessionManager;
        readonly MultiplaySessionManager m_MultiplaySessionManager;

        internal MultiplayerServerServiceImpl(
            SessionManager sessionManager,
            MultiplaySessionManager multiplaySessionManager)
        {
            m_SessionManager = sessionManager;
            m_MultiplaySessionManager = multiplaySessionManager;
        }

        public Task<IServerSession> CreateSessionAsync(SessionOptions sessionOptions)
        {
            if (m_SessionManager == null)
            {
                throw new SessionException("Cannot create server session. Missing manager dependency.", SessionError.MultiplayServerError);
            }

            return m_SessionManager.CreateAsync(sessionOptions).ContinueWith(t => t.Result.AsServer());
        }

        public Task<IMultiplaySessionManager> StartMultiplaySessionManagerAsync(MultiplaySessionManagerOptions options)
        {
            if (m_MultiplaySessionManager == null)
            {
                throw new SessionException("Cannot start a Multiplay session manager. This operation only works on a Multiplay server.", SessionError.MultiplayServerError);
            }

            return m_MultiplaySessionManager.StartMultiplaySessionManagerAsync(options);
        }
    }
}
