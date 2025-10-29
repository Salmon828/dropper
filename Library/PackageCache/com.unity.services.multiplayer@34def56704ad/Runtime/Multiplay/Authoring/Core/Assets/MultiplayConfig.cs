using System;
using System.Collections.Generic;

namespace Unity.Services.Multiplay.Authoring.Core.Assets
{
    /// <summary>
    /// Represents the model of a Multiplay configuration file
    /// </summary>
    public class MultiplayConfig
    {
        /// <summary>
        /// Version of the multiplay schema
        /// </summary>
        public string Version { get; init; }

        /// <summary>
        /// Build definitions for this configuration
        /// </summary>
        public IDictionary<BuildName, BuildDefinition> Builds { get; init; } = new Dictionary<BuildName, BuildDefinition>();

        /// <summary>
        /// Build Config definitions for this configuration
        /// </summary>
        public IDictionary<BuildConfigurationName, BuildConfigurationDefinition> BuildConfigurations { get; init; } = new Dictionary<BuildConfigurationName, BuildConfigurationDefinition>();

        /// <summary>
        /// Fleet definitions for this configuration
        /// </summary>
        public IDictionary<FleetName, FleetDefinition> Fleets { get; init; } = new Dictionary<FleetName, FleetDefinition>();

        /// <summary>
        /// Represents a Build definition
        /// </summary>
        public class BuildDefinition
        {
            /// <summary>
            /// The path to the executable of the build
            /// </summary>
            public string ExecutableName { get; init; } = "Server";

            /// <summary>
            /// The path to the Build location
            /// </summary>
            public string BuildPath { get; set; }

            /// <summary>
            /// Paths or patterns to be excluded from the upload
            /// </summary>
            public List<string> ExcludePaths { get; set; } = new List<string>();
        }

        /// <summary>
        /// Represents the definition of a build configuration
        /// </summary>
        public class BuildConfigurationDefinition
        {
            /// <summary>
            /// The name of the Build that the Build Configuration uses
            /// </summary>
            public BuildName Build { get; init; }

            /// <summary>
            /// The query type for the build configuration
            /// </summary>
            public Query? QueryType { get; init; }

            /// <summary>
            /// The path to the binary from the build
            /// </summary>
            public string BinaryPath { get; init; }

            /// <summary>
            /// The command line this build configuration should use for the associated build
            /// </summary>
            public string CommandLine { get; init; }

            /// <summary>
            /// Additional environment variables to use with this build configuration
            /// </summary>
            public IDictionary<string, string> Variables { get; init; } = new Dictionary<string, string>();

            /// <summary>
            /// Deprecated - The number of cores that should be used for this build configuration
            /// </summary>
            public int Cores { get; init; } = 1;

            /// <summary>
            /// Deprecated - The CPU utilisation per core.
            /// </summary>
            public int SpeedMhz { get; init; } = 750;

            /// <summary>
            /// Deprecated - Memory required per server.
            /// </summary>
            public int MemoryMiB { get; init; } = 800;

            /// <summary>
            /// A boolean to indicate allocation readiness status.
            /// </summary>
            public bool Readiness { get; init; } = true;
        }

        /// <summary>
        /// Definition of a fleet
        /// </summary>
        public class FleetDefinition
        {
            /// <summary>
            /// A list of region associations and scaling settings with the fleet
            /// </summary>
            public IDictionary<string, ScalingDefinition> Regions { get; init; } = new Dictionary<string, ScalingDefinition>();

            /// <summary>
            /// A list of build configuration to associate with the fleet.
            /// </summary>
            public IList<BuildConfigurationName> BuildConfigurations { get; init; } = new List<BuildConfigurationName>();

            /// <summary>
            /// A list of usage settings associated with the fleet.
            /// </summary>
            public IList<FleetUsageSetting> UsageSettings { get; init; } = new List<FleetUsageSetting>();
        }

        /// <summary>
        /// A usage setting associated with the fleet.
        /// </summary>
        public class FleetUsageSetting
        {
            /// <summary>
            /// ID of the Fleet Usage.
            /// </summary>
            public long FleetUsageID { get; init; }

            /// <summary>
            /// The hardware type of a machine.
            /// </summary>
            public HardwareTypeOptions HardwareType { get; init; }

            /// <summary>
            /// Machine type to be associated with these setting.   * For &#x60;CLOUD&#x60; setting: In most cases, the only machine type available for your fleet is GCP-N2.   * For &#x60;METAL&#x60; setting: Please omit this field. All metal machines will be using the same setting, regardless of its type.
            /// </summary>
            public string MachineType { get; init; }

            /// <summary>
            /// Maximum speed to be allocated per server in MHz.   * For &#x60;CLOUD&#x60; setting: This field is not available, please omit.   * For &#x60;METAL&#x60; setting: This is a required field. Minimum speed is 100 MHz.
            /// </summary>
            public long Speed { get; init; }

            /// <summary>
            /// Maximum RAM to be allocated per server in MB.   * For &#x60;CLOUD&#x60; setting: This field is not available, please omit.   * For &#x60;METAL&#x60; setting: This is a required field. Minimum RAM is 100 MB.
            /// </summary>
            public long Memory { get; init; }

            /// <summary>
            /// Maximum number of servers to be allocated per machine.   * For &#x60;CLOUD&#x60; setting: This is a required field.   * For &#x60;METAL&#x60; setting: This is an optional field.
            /// </summary>
            public long MaxServersPerMachine { get; init; }

            /// <summary>
            /// Maximum number of servers to be allocated per physical CPU core.   * For &#x60;CLOUD&#x60; setting: This field is not available, please omit.   * For &#x60;METAL&#x60; setting: This is an optional field.
            /// </summary>
            public long MaxServersPerCore { get; init; }

            /// <summary>
            /// The hardware type of a machine.
            /// </summary>
            /// <value>The hardware type of a machine.</value>
            public enum HardwareTypeOptions
            {
                /// <summary>
                /// Enum CLOUD for value: CLOUD
                /// </summary>
                CLOUD = 1,
                /// <summary>
                /// Enum METAL for value: METAL
                /// </summary>
                METAL = 2
            }
        }

        /// <summary>
        /// Represents scaling settings for a fleet
        /// </summary>
        public class ScalingDefinition
        {
            /// <summary>
            /// Minimum number of servers to keep free for new game sessions.
            /// </summary>
            public int MinAvailable { get; init; }

            /// <summary>
            /// Maximum number of servers to host in the fleet region
            /// </summary>
            public int MaxServers { get; init; }

            /// <summary>
            /// Is the Region online
            /// </summary>
            public bool Online { get; init; } = true;
        }

        /// <summary>
        /// Available query protocols: https://docs.unity.com/ugs/en-us/manual/game-server-hosting/manual/concepts/query-protocols
        /// </summary>
        public enum Query
        {
            /// <summary>
            /// The Server Query Protocol (SQP) allows clients to retrieve information about a running game server using UDP/IP packets.
            /// </summary>
            Sqp,
            /// <summary>
            /// A2S is a UDP-based game server query protocol that Valve Software keeps as a part of the Steam SDK.
            /// </summary>
            A2s,
            /// <summary>
            /// No query protocol
            /// </summary>
            None
        }
    }
}
