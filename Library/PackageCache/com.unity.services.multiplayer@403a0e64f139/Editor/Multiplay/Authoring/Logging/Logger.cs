using Unity.Services.Multiplay.Authoring.Core.Logging;

namespace Unity.Services.Multiplay.Authoring.Editor.Logging
{
    class Logger : ILogger
    {
        public void LogInfo(object message)
        {
            Multiplayer.Editor.Shared.Logging.Logger.Log(message);
        }

        public void LogWarning(object message)
        {
            Multiplayer.Editor.Shared.Logging.Logger.LogWarning(message);
        }

        public void LogError(object message)
        {
            Multiplayer.Editor.Shared.Logging.Logger.LogError(message);
        }

        public void LogVerbose(object message)
        {
            Multiplayer.Editor.Shared.Logging.Logger.LogVerbose(message);
        }
    }
}
