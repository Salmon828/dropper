using System;

namespace Unity.Services.Multiplayer
{
    class ConnectionOption : IModuleOption
    {
        public Type Type => typeof(ConnectionModule);
        internal readonly ConnectionInfo Options;

        public ConnectionOption(ConnectionInfo options)
        {
            Options = options;
        }

        public void Process(SessionHandler session)
        {
            if (session.IsHost)
            {
                var module = session.GetModule<ConnectionModule>();

                if (module == null)
                {
                    throw new Exception("Trying to setup connection in session but the module isn't registered.");
                }

                module.SetConnectionOption(Options);
            }
        }
    }
}
