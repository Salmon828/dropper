using System;
using System.Collections.Generic;

namespace Unity.Services.Multiplayer
{
    /// <summary>
    /// Common options for all session flows
    /// </summary>
    public abstract class BaseSessionOptions
    {
        /// <summary>
        /// The session type is a client-side key used to uniquely identify a session
        /// </summary>
        public string Type { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Custom game-specific properties that apply to an individual player (e.g. 'role' or 'skill').
        /// </summary>
        /// <remarks>
        /// Up to 10 properties may be set per player. <see cref="PlayerProperty">Player properties</see> have different
        /// visibility levels.
        /// </remarks>
        public Dictionary<string, PlayerProperty> PlayerProperties { get; set; } = new Dictionary<string, PlayerProperty>();

        internal Dictionary<Type, IModuleOption> Options { get; set; } = new Dictionary<Type, IModuleOption>();
        internal bool HasOption<T>() where T : class, IModuleOption => Options.ContainsKey(typeof(T));
        internal T GetOption<T>() where T : class, IModuleOption => Options.ContainsKey(typeof(T)) ? Options[typeof(T)] as T : null;
    }

    public static partial class SessionOptionsExtensions
    {
        /// <summary>
        /// Provide custom options to the session operation.
        /// </summary>
        /// <param name="options">The <see cref="BaseSessionOptions"/> this extension method applies to.</param>
        /// <param name="moduleOptions">The module setup implementation.</param>
        /// <typeparam name="T">The options' type.</typeparam>
        /// <typeparam name="U">The module options' type.</typeparam>
        /// <returns>The session module options.</returns>
        internal static T WithOption<T, U>(this T options, U moduleOptions)
            where T : BaseSessionOptions where U : IModuleOption
        {
            var type = typeof(U);

            if (options.Options.ContainsKey(type))
            {
                throw new Exception($"{type.Name} Option is already included");
            }

            options.Options.Add(type, moduleOptions);
            return options;
        }
    }
}
