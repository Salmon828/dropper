using System;
using Unity.Services.Multiplay;
using UnityEngine;
using Debug = Unity.Services.Multiplayer.Logger;

namespace Unity.Services.Multiplayer
{
    /// <summary>
    /// Class for providing your callbacks, which are used by the Multiplayer SDK when a MultiplaySessionManager Event occurs.
    /// </summary>
    public class MultiplaySessionManagerEventCallbacks
    {
        /// <summary>
        /// Callback which will be invoked when the Server receives an Allocation.
        /// </summary>
        public event Action<IMultiplayAllocation> Allocated;

        /// <summary>
        /// Callback which will be invoked when the Server receives a Deallocation.
        /// </summary>
        public event Action<IMultiplayAllocation> Deallocated;

        /// <summary>
        /// Callback which will be invoked when the Server state changes.
        /// </summary>
        public event Action<MultiplaySessionManagerState> StateChanged;

        /// <summary>
        /// Callback which will be invoked when the Server player readiness changes.
        /// </summary>
        public event Action<bool> PlayerReadinessChanged;

        internal void InvokeAllocate(MultiplayAllocation allocation)
        {
            Debug.LogVerbose("MultiplaySessionManagerEventCallbacks.InvokeAllocate");
            Allocated?.Invoke(new ServerAllocation(allocation));
        }

        internal void InvokeDeallocate(MultiplayDeallocation deallocation)
        {
            Debug.LogVerbose("MultiplaySessionManagerEventCallbacks.InvokeDeallocate");
            Deallocated?.Invoke(new ServerDeallocation(deallocation));
        }

        internal void InvokeStateChanged(MultiplaySessionManagerState state)
        {
            Debug.LogVerbose("MultiplaySessionManagerEventCallbacks.InvokeStateChanged");
            StateChanged?.Invoke(state);
        }

        internal void InvokePlayerReadinessChanged(bool isReady)
        {
            Debug.LogVerbose("MultiplaySessionManagerEventCallbacks.InvokePlayerReadinessChanged");
            PlayerReadinessChanged?.Invoke(isReady);
        }
    }
}
