using Unity.Services.Multiplayer.Editor.Shared.Analytics;
using UnityEngine.Analytics;

namespace Unity.Services.Multiplay.Authoring.Editor.Analytics
{
    interface IAnalyticsUtils
    {
        AnalyticsResult SendCommonEvent(ICommonAnalytics.CommonEventPayload payload);
    }
}
