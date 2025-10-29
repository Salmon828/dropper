using Unity.Services.Multiplay;

namespace Unity.Services.Multiplayer
{
    /// <summary>
    /// Allows configuring the server options of a Game Server Hosting server.
    /// </summary>
    public class MultiplayServerOptions
    {
        /// <summary>
        /// When using Server Readiness check, if AutoReady is true, the server will be automatically set to ready once the server is started.
        /// </summary>
        public bool AutoReady { get; set; }

        /// <summary>
        /// The name of the server.
        /// </summary>
        public string ServerName
        {
            get => m_ServerName;
            set
            {
                m_ServerName = value;
                m_UnpushedChanges = true;
            }
        }

        /// <summary>
        /// The type of game running on the allocated server.
        /// </summary>
        public string GameType
        {
            get => m_GameType;
            set
            {
                m_GameType = value;
                m_UnpushedChanges = true;
            }
        }

        /// <summary>
        /// The Id of the build running on the allocated server.
        /// </summary>
        public string BuildId
        {
            get => m_BuildId;
            set
            {
                m_BuildId = value;
                m_UnpushedChanges = true;
            }
        }

        /// <summary>
        /// The map of the game running on the allocated server.
        /// </summary>
        public string Map
        {
            get => m_Map;
            set
            {
                m_Map = value;
                m_UnpushedChanges = true;
            }
        }

        /// <summary>
        /// The maximum number of players that can join the server.
        /// </summary>
        ushort MaxPlayers
        {
            get => m_MaxPlayers;
            set
            {
                m_MaxPlayers = value;
                m_UnpushedChanges = true;
            }
        }

        /// <summary>
        /// The current number of players that are connected to the server.
        /// </summary>
        ushort CurrentPlayers
        {
            get => m_CurrentPlayers;
            set
            {
                m_CurrentPlayers = value;
                m_UnpushedChanges = true;
            }
        }

        string m_ServerName;
        string m_GameType;
        string m_BuildId;
        string m_Map;
        ushort m_MaxPlayers;
        ushort m_CurrentPlayers;
        bool m_UnpushedChanges;

        /// <summary>
        /// Creates a new instance of MultiplayServerOptions that can be used to configure a Game Server Hosting server.
        /// </summary>
        /// <param name="serverName">The name of the server.</param>
        /// <param name="gameType">The type of game running on the allocated server.</param>
        /// <param name="buildId">The id of the build running on the allocated server.</param>
        /// <param name="map">The map of the game running on the allocated server.</param>
        /// <param name="autoReady">When using Server Readiness check, if this is true, the server will be automatically set to ready once the server is started.</param>
        public MultiplayServerOptions(string serverName, string gameType, string buildId, string map, bool autoReady = true)
        {
            AutoReady = autoReady;
            ServerName = serverName;
            GameType = gameType;
            BuildId = buildId;
            Map = map;
            m_UnpushedChanges = true;
        }

        internal MultiplayServerOptions WithMaxPlayers(ushort maxPlayers)
        {
            MaxPlayers = maxPlayers;
            return this;
        }

        internal MultiplayServerOptions WithCurrentPlayers(ushort currentPlayers)
        {
            CurrentPlayers = currentPlayers;
            return this;
        }

        internal IServerQueryHandler UpdateQueryHandler(IServerQueryHandler queryHandler)
        {
            queryHandler.MaxPlayers = MaxPlayers;
            queryHandler.CurrentPlayers = CurrentPlayers;
            queryHandler.ServerName = ServerName;
            queryHandler.GameType = GameType;
            queryHandler.BuildId = BuildId;
            queryHandler.Map = Map;
            return queryHandler;
        }

        internal bool HasChanged()
        {
            return m_UnpushedChanges;
        }

        internal void ResetChanged()
        {
            m_UnpushedChanges = false;
        }
    }
}
