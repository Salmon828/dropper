using Unity.Netcode;
using UnityEngine;

// Marker class so we know what to reset and who spawned it
public class SpawnMarker : NetworkBehaviour
{
    public ulong SpawnerClientId;

    // Set by the server before Spawn() so it travels with the spawn payload, which
    // keeps each trail instance its own colour instead of sharing the prefab's.
    public NetworkVariable<Color32> trailColor = new NetworkVariable<Color32>(
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        trailColor.OnValueChanged += ApplyColor;
        ApplyColor(trailColor.Value, trailColor.Value);
    }

    public override void OnNetworkDespawn()
    {
        trailColor.OnValueChanged -= ApplyColor;
        base.OnNetworkDespawn();
    }

    private void ApplyColor(Color32 prevColor, Color32 newColor)
    {
        var sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.color = newColor;
    }
}
