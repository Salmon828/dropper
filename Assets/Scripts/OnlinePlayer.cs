using Dropper;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

public class OnlinePlayer : NetworkBehaviour
{
    public NetworkVariable<Vector3> netDirection = new NetworkVariable<Vector3>();
    public NetworkVariable<int> playerNumber = new NetworkVariable<int>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

    InputAction Up, Down, Left, Right;
    public InputActionAsset actionAsset;
    private InputActionMap map;

    [SerializeField]
    private Vector3 direction = Vector3.right;
    public float moveSpeed = 10f;

    // Variables that limit the rate at which clients can send data to the server
    private float sendInterval = 1f / 20f;
    private float sendTimer;

    public Rigidbody2D rb;
    public bool isP1, fired = false;

    [SerializeField]
    private int trailSpawnRate = 1;
    private float trailTimer = 0f;
    [SerializeField]
    private NetworkObject trailPrefab;

    [SerializeField]
    OnlineGameManager manager;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Give IDs to every connected player starting from 0
        if (IsServer && playerNumber.Value == 0)
        {
            int index = 1;
            var list = NetworkManager.ConnectedClientsList;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].ClientId == OwnerClientId)
                {
                    index = i + 1; break;
                }
            }
            playerNumber.Value = index;
        }
        Debug.Log(playerNumber.Value);
        manager = GameObject.Find("OnlineGameManager").GetComponent<OnlineGameManager>();

        // To allow first frame movement
        sendTimer = sendInterval;

        rb = GetComponent<Rigidbody2D>();

        // Only let the player control themselves
        if (IsOwner)
        {
            map = actionAsset.FindActionMap("Player");
            if (map == null) Debug.LogWarning("Action Map is Null");


            // Allow keyboard movement
            Up = map.FindAction("P1Up", true);
            Down = map.FindAction("P1Down", true);
            Left = map.FindAction("P1Left", true);
            Right = map.FindAction("P1Right", true);

            if (!map.enabled)
            {
                map.Enable();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        if (Up.WasPerformedThisFrame())
        {
            direction = Vector3.up;
            changeDir(direction);
        }
        else if (Down.WasPerformedThisFrame())
        {
            direction = Vector3.down;
            changeDir(direction);
        }
        else if (Left.WasPerformedThisFrame())
        {
            direction = Vector3.left;
            changeDir(direction);
        }
        else if (Right.WasPerformedThisFrame())
        {
            direction = Vector3.right;
            changeDir(direction);
        }
        sendTimer += Time.deltaTime;
    }

    private void FixedUpdate()
    {
        // Server controls movement
        if (!IsServer) return;

        if (!manager.freeze.Value)
        {
            Vector3 newPos = netDirection.Value * moveSpeed * Time.fixedDeltaTime;

            rb.MovePosition(transform.position + newPos);
            trailTimer += Time.deltaTime;

            if (trailTimer > trailSpawnRate)
            {
                trailTimer = 0;
                Vector3 spawnPos = transform.position;
                var t = Instantiate(trailPrefab, spawnPos, transform.rotation);
                var marker = t.GetComponent<SpawnMarker>();
                marker.SpawnerClientId = (ulong)playerNumber.Value;
                t.Spawn();
            }
        }

    }
    void changeDir(Vector3 dir)
    {
        if (sendTimer >= sendInterval)
        {
            changeDirectionServerRpc(dir);
            sendTimer = 0;
        }
    }

    [ServerRpc]
    void changeDirectionServerRpc(Vector3 newDir)
    {
        netDirection.Value = newDir;
    }

    private void clearTrails()
    {
        if (!IsServer) return;

        var spawnList = NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.ToList();
        foreach (var t in spawnList)
        {
            if(t != null && t.IsSpawned && t.TryGetComponent<SpawnMarker>(out _))
            { 

                if (t == NetworkObject) continue;

                t.Despawn(true);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        Debug.Log("TriggerEnter:" + collision);
        if (collision.tag == "Obstacle")
        {
            int p1 = 0;
            if (playerNumber.Value == 1) p1 = 1;

            manager.scoreUpdate(p1);
            fired = false;
            clearTrails();
        }
    }

    // Need to also use collision enter for player collisions, which are ties
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return;

        // Stop collisions happening before the game starts **look into more robust gamestate triggers**
        if (!manager.freeze.Value)
        {
            Debug.Log("CollisonEnter!! Name:" + collision.collider.name);

            if (collision.collider.tag == "Player")
            {
                manager.scoreUpdate(2);
                fired = false;
                clearTrails();
            }
        }
    }

}
