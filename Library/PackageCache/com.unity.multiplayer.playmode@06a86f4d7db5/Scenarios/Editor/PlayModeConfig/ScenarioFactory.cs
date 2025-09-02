#if UNITY_USE_MULTIPLAYER_ROLES
using Unity.Multiplayer.Editor;
#endif
using System.Collections.Generic;
using System.Linq;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Editor;
using UnityEngine.Assertions;
using UnityEditor.Build.Profile;
using System.Text.RegularExpressions;
using Unity.Multiplayer.PlayMode.Configurations.Editor;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Local;
using Unity.Multiplayer.PlayMode.Editor.Bridge;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Api;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Local.Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Local;
#if MULTIPLAY_API_AVAILABLE
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Multiplay;
#endif

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor
{
    /// <summary>
    /// Creates a scenario graph from a list of instance descriptions.
    /// </summary>
    internal static class ScenarioFactory
    {
#if UNITY_USE_MULTIPLAYER_ROLES
        public static MultiplayerRoleFlags GetRoleForInstance(InstanceDescription instance)
        {
            Assert.IsNotNull(instance, $"Null instance used");
            switch (instance)
            {
                case EditorInstanceDescription editorInstance: return editorInstance.RoleMask;
                case IBuildableInstanceDescription buildableInstance: return buildableInstance.BuildProfile == null ? MultiplayerRoleFlags.Client : EditorMultiplayerRolesManager.GetMultiplayerRoleForBuildProfile(buildableInstance.BuildProfile);
            }
            return MultiplayerRoleFlags.Client;
        }
#endif
        internal static class RemoteNodeConstants
        {
            internal const string k_BuildNodePostFix = "- Build";
            internal const string k_DeployBuildNodePostfix = "- Deploy Build";
            internal const string k_DeployConfigBuildNodePostfix = "- Deploy Build Configuration";
            internal const string k_DeployFleetNodePostfix = "- Deploy Fleet";
            internal const string k_RunNodePostfix = "- Run";
            internal const string k_AllocateNodePostfix = "- Allocate";
        }

        private static void CategorizeInstances(
            List<InstanceDescription> instances,
            out List<InstanceDescription> servers,
            out List<InstanceDescription> clients)
        {
            servers = new List<InstanceDescription>();
            clients = new List<InstanceDescription>();

            foreach (var instance in instances)
            {
#if UNITY_USE_MULTIPLAYER_ROLES
                if (GetRoleForInstance(instance).HasFlag(MultiplayerRoleFlags.Server))
                    servers.Add(instance);
                else
#endif
                {
                    clients.Add(instance);
                }
            }
        }

        public static Scenario CreateScenario(string name, List<InstanceDescription> instanceDescriptions)
        {
            var scenario = Scenario.Create(name);

            CategorizeInstances(instanceDescriptions, out var serverDescriptList, out var clientDescriptList);

            // [TODO] It's good to report the error if multiple servers are selected but we also need to report it earlier, directly in the configuration UI.
            Assert.IsTrue(serverDescriptList.Count() <= 1, "There can only be one server in a scenario");

            // This will ensure the server instance is the first to be added in the scenario.
            var serverDescript = serverDescriptList.FirstOrDefault();
            IConnectableNode serverRunNode = null;
            if (serverDescript != null)
            {
                var serverInstance = ConnectOrCreateInstance(serverDescript, out serverRunNode);
                scenario.AddInstance(serverInstance);
            }

            // Grab server connection configuration to set up with clients.
            // This will Ensure the server will pass its connection Data (IP + Port, potentially transport type
            // in the future (UDP / WSS) , secure/ Not Secure) to all the clients.
            var serverConnectionData = new ConnectionData();
            if (serverRunNode != null && serverRunNode.ConnectionDataOut.GetValue<ConnectionData>() != null)
            {
                serverConnectionData = serverRunNode.ConnectionDataOut.GetValue<ConnectionData>();
            }

            // Finally, iterate through the rest of the instance descriptions, construct client instances
            // and configure the connection data for when they run.
            foreach (var clientDescript in clientDescriptList)
            {
                var clientInstance = ConnectOrCreateInstance(clientDescript, out IConnectableNode clientRunNode);
                scenario.AddInstance(clientInstance);

                var graph = clientInstance.GetExecutionGraph();
                graph.ConnectConstant(clientRunNode.ConnectionDataIn, serverConnectionData, true);
            }
            return scenario;
        }

        private static Instance ConnectOrCreateInstance(InstanceDescription instanceDescription, out IConnectableNode runNode)
        {
            // If an Existing Instance is Actively Free Running, we connect that instance to this new Scenario.
            if (PlayModeManager.instance.ActivePlayModeConfig is ScenarioConfig config &&
                config.Scenario != null)
            {
                var activeFreeInstance = config.Scenario.GetInstanceByName(instanceDescription.Name, true);
                if (activeFreeInstance != null)
                {
                    // Grab the run node for Connection Data reconfiguration if in case the
                    // created scenario has different server connection inputs.
                    var runNodes = activeFreeInstance.GetExecutionGraph().GetNodes(ExecutionStage.Run);
                    runNode = (IConnectableNode)runNodes.LastOrDefault();

                    // Ensure we remove it from the old Scenario
                    config.Scenario.RemoveInstance(activeFreeInstance);

                    // Finally return the Free run instance to be attached to the new one.
                    return activeFreeInstance;
                }
            }

            // Else, create and rebuild the instance from the description as per usual.
            return CreateInstance(instanceDescription, out runNode);
        }

        private static Instance CreateInstance(InstanceDescription instanceDescription, out IConnectableNode runNode)
        {
            if (instanceDescription is EditorInstanceDescription editorDescription)
            {
                return CreateEditorInstance(editorDescription, out runNode);
            }
            if (instanceDescription is LocalInstanceDescription localDescription)
            {
                return CreateLocalInstance(localDescription, out runNode);
            }
            if (instanceDescription is RemoteInstanceDescription remoteDescription)
            {
#if MULTIPLAY_API_AVAILABLE
                return CreateRemoteInstance(remoteDescription, out runNode);
#else
                throw new System.Exception("The Multiplay API is not available. It is not possible to create a InsertRemoteInstance instance without it.");
#endif
            }
            throw new System.NotImplementedException();
        }

        static Instance CreateEditorInstance(EditorInstanceDescription editorInstanceDescription, out IConnectableNode runNode)
        {
            var instance = Instance.Create(editorInstanceDescription);
            var executionGraph = instance.GetExecutionGraph();
            var editorRunNode = new EditorMultiplayerPlaymodeRunNode($"{editorInstanceDescription.Name}|{editorInstanceDescription.PlayerInstanceIndex}_run");
            var deployNode = new EditorMultiplayerPlaymodeDeployNode($"{editorInstanceDescription.Name}|{editorInstanceDescription.PlayerInstanceIndex}_deploy");

            executionGraph.AddNode(deployNode, ExecutionStage.Deploy);
            executionGraph.ConnectConstant(deployNode.PlayerInstanceIndex, editorInstanceDescription.PlayerInstanceIndex);
            executionGraph.ConnectConstant(deployNode.PlayerTags, editorInstanceDescription.PlayerTag);
#if UNITY_USE_MULTIPLAYER_ROLES
            executionGraph.ConnectConstant(deployNode.MultiplayerRole, editorInstanceDescription.RoleMask);
#endif
            executionGraph.ConnectConstant(deployNode.InitialScene, editorInstanceDescription.InitialScene);

            // [TODO]: We need to remove this line, since 1 instance could have multiple nodes
            editorInstanceDescription.CorrespondingNodeId = editorRunNode.Name;
            editorInstanceDescription.SetCorrespondingNodes(editorRunNode, deployNode);

            executionGraph.AddNode(editorRunNode, ExecutionStage.Run);
            executionGraph.ConnectConstant(editorRunNode.PlayerInstanceIndex, editorInstanceDescription.PlayerInstanceIndex);
            executionGraph.ConnectConstant(editorRunNode.PlayerTags, editorInstanceDescription.PlayerTag);

            if (editorInstanceDescription is VirtualEditorInstanceDescription virtualEditorInstanceDescription)
            {
                executionGraph.ConnectConstant(editorRunNode.StreamLogs, virtualEditorInstanceDescription.AdvancedConfiguration.StreamLogsToMainEditor);
                executionGraph.ConnectConstant(editorRunNode.LogsColor, virtualEditorInstanceDescription.AdvancedConfiguration.LogsColor);
            }

            runNode = editorRunNode;
            return instance;
        }

        private static Instance CreateLocalInstance(LocalInstanceDescription description, out IConnectableNode runNode)
        {
            var instance = Instance.Create(description);
            var executionGraph = instance.GetExecutionGraph();

            // TODO: We need to share the build nodes between instances that share the same build profile and role.
            var buildNode = new EditorBuildNode($"{description.Name} - Build");
            executionGraph.AddNode(buildNode, ExecutionStage.Prepare);

            executionGraph.ConnectConstant(buildNode.BuildPath, GenerateBuildPath(description.BuildProfile));
            executionGraph.ConnectConstant(buildNode.Profile, description.BuildProfile);

            var deviceRunNode = new LocalDeviceRunNode($"{description.Name} - Run");

            // TODO: UUM-50144 - There is currently a bug in windows dedicated server where screen related
            // arguments cause a crash. As a temporary workaround we detect that case and remove any
            // of those arguments that, in any case, take no effect on that platform.
            var arguments = description.AdvancedConfiguration.Arguments;
            if (InternalUtilities.IsServerProfile(description.BuildProfile))
                arguments = CleanupScreenArguments(arguments);

            if (InternalUtilities.IsAndroidBuildTarget(description.BuildProfile))
            {
                executionGraph.AddNode(deviceRunNode, ExecutionStage.Run);
                executionGraph.ConnectConstant(deviceRunNode.Arguments, arguments);
                executionGraph.ConnectConstant(deviceRunNode.StreamLogs, description.AdvancedConfiguration.StreamLogsToMainEditor);
                executionGraph.ConnectConstant(deviceRunNode.LogsColor, description.AdvancedConfiguration.LogsColor);
                executionGraph.ConnectConstant(deviceRunNode.DeviceName, description.AdvancedConfiguration.DeviceID);

                executionGraph.Connect(buildNode.ExecutablePath, deviceRunNode.ExecutablePath);
                executionGraph.Connect(buildNode.BuildReport, deviceRunNode.BuildReport);

                // [TODO]: We need to remove this line, since 1 instance could have multiple nodes
                description.CorrespondingNodeId = deviceRunNode.Name;

                description.SetCorrespondingNodes(buildNode, deviceRunNode);
                runNode = deviceRunNode;
                return instance;

            }

            var localRunNode = new LocalRunNode($"{description.Name} - Run");
            executionGraph.AddNode(localRunNode, ExecutionStage.Run);

            executionGraph.ConnectConstant(localRunNode.Arguments, arguments);
            executionGraph.ConnectConstant(localRunNode.StreamLogs, description.AdvancedConfiguration.StreamLogsToMainEditor);
            executionGraph.ConnectConstant(localRunNode.LogsColor, description.AdvancedConfiguration.LogsColor);
            executionGraph.Connect(buildNode.ExecutablePath, localRunNode.ExecutablePath);

            // [TODO]: We need to remove this line, since 1 instance could have multiple nodes
            description.CorrespondingNodeId = localRunNode.Name;

            description.SetCorrespondingNodes(buildNode, localRunNode);
            runNode = localRunNode;
            return instance;
        }

        private static string CleanupScreenArguments(string arguments)
        {
            // We need to remove -screen-fullscreen -screen-width and -screen-height arguments
            arguments = Regex.Replace(arguments, @"-screen-fullscreen\s+\d*", "");
            arguments = Regex.Replace(arguments, @"-screen-width\s+\d*", "");
            arguments = Regex.Replace(arguments, @"-screen-height\s+\d*", "");
            return arguments;
        }

        private static Instance CreateRemoteInstance(RemoteInstanceDescription description, out IConnectableNode runNode)
        {

#if !MULTIPLAY_API_AVAILABLE
            throw new System.Exception("The Multiplay API is not available. It is not possible to create the corresponding Deploy and Run nodes without it.");
#else

#if UNITY_USE_MULTIPLAYER_ROLES
            var role = GetRoleForInstance(description);

            // We assume the remote instance is a server.
            Assert.AreEqual(role, MultiplayerRoleFlags.Server);
#endif
            var buildPath = GenerateBuildPath(description.BuildProfile);

            var instance = Instance.Create(description);
            var executionGraph = instance.GetExecutionGraph();
            var buildNode = new EditorBuildNode($"{description.Name} {RemoteNodeConstants.k_BuildNodePostFix}");
            executionGraph.AddNode(buildNode, ExecutionStage.Prepare);
            executionGraph.ConnectConstant(buildNode.BuildPath, buildPath);
            executionGraph.ConnectConstant(buildNode.Profile, description.BuildProfile);


            var advancedConfiguration = description.AdvancedConfiguration;
            var multiplayName = RemoteInstanceDescription.ComputeMultiplayName(advancedConfiguration.Identifier);
            var buildName = multiplayName;
            var buildConfigurationName = multiplayName;
            var fleetName = multiplayName;

            var deployBuildNode = new DeployBuildNode($"{description.Name} {RemoteNodeConstants.k_DeployBuildNodePostfix}");
            executionGraph.AddNode(deployBuildNode, ExecutionStage.Deploy);
            executionGraph.ConnectConstant(deployBuildNode.BuildName, buildName);
            executionGraph.Connect(buildNode.OutputPath, deployBuildNode.BuildPath);
            executionGraph.Connect(buildNode.ExecutablePath, deployBuildNode.ExecutablePath);
            executionGraph.Connect(buildNode.BuildHash, deployBuildNode.BuildHash);

            var deployBuildConfigNode = new DeployBuildConfigurationNode($"{description.Name} {RemoteNodeConstants.k_DeployConfigBuildNodePostfix}");
            executionGraph.AddNode(deployBuildConfigNode, ExecutionStage.Deploy);
            executionGraph.ConnectConstant(deployBuildConfigNode.BuildConfigurationName, buildConfigurationName);
            executionGraph.ConnectConstant(deployBuildConfigNode.BuildName, buildName);
            executionGraph.ConnectConstant(deployBuildConfigNode.Settings, description.GetBuildConfigurationSettings());
            executionGraph.Connect(deployBuildNode.BuildId, deployBuildConfigNode.BuildId);
            executionGraph.Connect(buildNode.RelativeExecutablePath, deployBuildConfigNode.BinaryPath);

            var deployFleetNode = new DeployFleetNode($"{description.Name} {RemoteNodeConstants.k_DeployFleetNodePostfix}");
            executionGraph.AddNode(deployFleetNode, ExecutionStage.Deploy);
            executionGraph.ConnectConstant(deployFleetNode.FleetName, fleetName);
            executionGraph.ConnectConstant(deployFleetNode.Region, advancedConfiguration.FleetRegion);
            executionGraph.ConnectConstant(deployFleetNode.BuildConfigurationName, buildConfigurationName);
            executionGraph.Connect(deployBuildConfigNode.BuildConfigurationId, deployFleetNode.BuildConfigurationId);


            var allocateNode = new AllocateNode($"{description.Name} {RemoteNodeConstants.k_AllocateNodePostfix}");
            executionGraph.AddNode(allocateNode, ExecutionStage.Run);
            executionGraph.ConnectConstant(allocateNode.FleetName, fleetName);
            executionGraph.ConnectConstant(allocateNode.BuildConfigurationName, buildConfigurationName);

            var remoteRunNode = new RunServerNode($"{description.Name} {RemoteNodeConstants.k_RunNodePostfix}");
            executionGraph.AddNode(remoteRunNode, ExecutionStage.Run);
            executionGraph.ConnectConstant(remoteRunNode.StreamLogs, description.AdvancedConfiguration.StreamLogsToMainEditor);
            executionGraph.ConnectConstant(remoteRunNode.LogsColor, description.AdvancedConfiguration.LogsColor);
            executionGraph.Connect(allocateNode.ServerId, remoteRunNode.ServerId);
            executionGraph.Connect(allocateNode.ConnectionDataOut, remoteRunNode.ConnectionData);

            // [TODO]: We need to remove this line, since 1 instance could have multiple nodes
            description.CorrespondingNodeId = remoteRunNode.Name;

            description.SetCorrespondingNodes(buildNode,deployBuildNode, deployBuildConfigNode, deployFleetNode, allocateNode,remoteRunNode);
            runNode = remoteRunNode;
            return instance;
#endif
        }

        private static string GenerateBuildPath(BuildProfile profile)
        {
            // It is important that all builds are in its own folder because when we upload the build to the Multiplay service,
            // we upload the whole folder. If we have multiple builds in the same folder, we will upload all of them.
            var escapedProfileName = EscapeProfileName(profile.name);
            return $"Builds/PlayModeScenarios/{escapedProfileName}/{escapedProfileName}";
        }

        private static string EscapeProfileName(string path) => Regex.Replace(path, @"[^\w\d]", "_");
    }
}
