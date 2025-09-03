using System;

namespace Unity.Services.Multiplayer
{
    class NetworkOption : IModuleOption
    {
        public Type Type => typeof(ConnectionModule);
        readonly INetworkHandler NetworkHandler;

        public NetworkOption(INetworkHandler networkHandler)
        {
            NetworkHandler = networkHandler;
        }

        public void Process(SessionHandler session)
        {
            var module = session.GetModule<ConnectionModule>();
            if (module == null)
            {
                throw new Exception("Trying to setup session connection but the module isn't registered.");
            }
            module.SetNetworkHandler(NetworkHandler);
        }
    }
}
