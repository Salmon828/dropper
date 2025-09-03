using System;

namespace Unity.Services.Multiplayer
{
    class ConnectionProvider : IModuleProvider
    {
        public Type Type => typeof(ConnectionModule);
        public int Priority => 1000;

        readonly INetworkBuilder m_NetworkBuilder;
        readonly IRelayBuilder m_RelayBuilder;
        readonly IDaBuilder m_DaBuilder;

        internal ConnectionProvider(INetworkBuilder networkBuilder, IDaBuilder daBuilder, IRelayBuilder relayBuilder)
        {
            m_NetworkBuilder = networkBuilder;
            m_DaBuilder = daBuilder;
            m_RelayBuilder = relayBuilder;
        }

        public IModule Build(ISession session)
        {
            return new ConnectionModule(session, m_NetworkBuilder, m_DaBuilder, m_RelayBuilder);
        }
    }
}
