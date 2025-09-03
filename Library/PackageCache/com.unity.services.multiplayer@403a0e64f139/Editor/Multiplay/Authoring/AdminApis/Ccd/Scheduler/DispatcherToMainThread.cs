using System;
using Unity.Services.Multiplay.Authoring.Core.Threading;
using Unity.Services.Multiplayer.Editor.Shared.Threading;

namespace Unity.Services.Multiplay.Authoring.Editor.AdminApis.Ccd.Scheduler
{
    class DispatcherToMainThread : IDispatchToMainThread
    {
        public void DispatchAction(Action action,
            string filePath = null,
            string member = null,
            int num = 0)
        {
            Sync.RunNextUpdateOnMain(action, filePath, member, num);
        }
    }
}
