using Unity.Netcode;
using UnityEngine;

// Marker class so we know what to reset and who spawned it
public class SpawnMarker : NetworkBehaviour
{
    public ulong SpawnerClientId;
}
