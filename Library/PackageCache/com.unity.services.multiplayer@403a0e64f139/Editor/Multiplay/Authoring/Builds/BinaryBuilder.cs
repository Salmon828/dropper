using System;
using System.Collections.Generic;
using System.IO;
using Unity.Services.Multiplay.Authoring.Core;
using Unity.Services.Multiplay.Authoring.Core.Builds;
using UnityEditor;
using UnityEditor.Build.Reporting;
using Logger = Unity.Services.Multiplayer.Editor.Shared.Logging.Logger;
using BuildFailedException = Unity.Services.Multiplay.Authoring.Core.Exceptions.BuildFailedException;

namespace Unity.Services.Multiplay.Authoring.Editor.Builds
{
    class BinaryBuilder : IBinaryBuilder
    {
        readonly IBuildsAnalytics m_BuildsAnalytics;

        bool m_OriginalBuildTargetUndefined = true;
        BuildTargetGroup m_OriginalTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        BuildTarget m_OriginalTarget = EditorUserBuildSettings.activeBuildTarget;
        StandaloneBuildSubtarget m_OriginalSubtarget = EditorUserBuildSettings.standaloneBuildSubtarget;

        public BinaryBuilder(IBuildsAnalytics buildsAnalytics)
        {
            m_BuildsAnalytics = buildsAnalytics;
        }

        public ServerBuild BuildLinuxServer(string outDir, string executable)
        {
            using var timer = m_BuildsAnalytics.BeginBuild();
            var path = Path.Combine(outDir, executable);

            // We explicitly specify the scenes in the build, to avoid adding any untitled / freshly made scenes
            var scenesInBuild = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    scenesInBuild.Add(scene.path);
                }
            }

            // Note that this must be called from the Main Thread
            if (m_OriginalBuildTargetUndefined)
            {
                m_OriginalTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                m_OriginalTarget = EditorUserBuildSettings.activeBuildTarget;
                m_OriginalSubtarget = EditorUserBuildSettings.standaloneBuildSubtarget;
                m_OriginalBuildTargetUndefined = false;
            }

            m_BuildsAnalytics.BuildDetails(m_OriginalTarget, m_OriginalSubtarget, BuildTarget.StandaloneLinux64);
            Logger.LogVerbose($"Starting build from originalTargetGroup[{m_OriginalTargetGroup}] originalTarget[{m_OriginalTarget}], to a linux server build");

            var buildOptions = new BuildPlayerOptions
            {
                target = BuildTarget.StandaloneLinux64,
                subtarget = (int)StandaloneBuildSubtarget.Server,
                locationPathName = path,
                scenes = scenesInBuild.ToArray()
            };

            var res = BuildPipeline.BuildPlayer(buildOptions);
            if (res.summary.result != BuildResult.Succeeded)
            {
                string buildFailed = $"Building the binaries for platform '{res.summary.platform}' failed. "
                    + $"Summary result: '{res.summary.result}'. Check the Console for details.{Environment.NewLine}"
                    + "Make sure scripts compile and the necessary platform development modules are installed.";
                throw new BuildFailedException(buildFailed);
            }

            timer.Dispose();

            return new ServerBuild(path);
        }

        public void WarnBuildTargetChanged()
        {
            if (m_OriginalTarget != EditorUserBuildSettings.activeBuildTarget
                || m_OriginalSubtarget != EditorUserBuildSettings.standaloneBuildSubtarget
                || m_OriginalTargetGroup != EditorUserBuildSettings.selectedBuildTargetGroup)
            {
                Logger.LogWarning("Build target was changed for the build process. " +
                    "You may need to switch targets to continue other work.");
            }

            m_OriginalBuildTargetUndefined = true;
            m_OriginalTargetGroup = default;
            m_OriginalTarget = default;
            m_OriginalSubtarget = default;
        }
    }
}
