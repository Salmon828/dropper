using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class OnlinePlayer : NetworkBehaviour
{
    public NetworkVariable<Vector3> netDirection = new NetworkVariable<Vector3>();
    public NetworkVariable<int> playerNumber = new NetworkVariable<int>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<Color32> playerColor = new NetworkVariable<Color32>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

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

    [SerializeField]
    private float dropRate = 0.3f; // Seconds between trail drops, same as local
    private float trailTimer = 0f;

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
        if (manager == null)
            manager = FindAnyObjectByType<OnlineGameManager>();

        // To allow first frame movement
        sendTimer = sendInterval;

        rb = GetComponent<Rigidbody2D>();

        // Color handling
        if (IsOwner)
        {
            changeColorServerRpc(PlayerColor.Load());
        }
        playerColor.OnValueChanged += ApplyColor;
        ApplyColor(playerColor.Value, playerColor.Value);


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

            if (trailTimer >= dropRate)
            {
                trailTimer = 0;
                manager.DropTrailRpc(transform.position, playerColor.Value);
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

    [ServerRpc]
    void changeColorServerRpc(Color32 newColor)
    {
        playerColor.Value = newColor;
    }

    // Applys the current color set in the color networkvariable
    private void ApplyColor(Color32 prevColor, Color32 newColor)
    {
        GetComponent<SpriteRenderer>().color = newColor;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        if (manager.freeze.Value) return; // Round already over (matches local: one crash per round)

        if (collision.tag == "Obstacle")
        {
            int p1 = 0;
            if (playerNumber.Value == 1) p1 = 1;

            manager.PlayCrashRpc(transform.position, playerColor.Value);
            manager.scoreUpdate(p1);
        }
    }

    // Need to also use collision enter for player collisions, which are ties
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return;

        // Stop collisions happening before the game starts **look into more robust gamestate triggers**
        if (!manager.freeze.Value)
        {

            if (collision.collider.tag == "Player")
            {
                // Head-on: both players explode
                manager.PlayCrashRpc(transform.position, playerColor.Value);
                if (collision.collider.TryGetComponent(out OnlinePlayer other))
                    manager.PlayCrashRpc(other.transform.position, other.playerColor.Value);
                manager.scoreUpdate(2);
            }
        }
    }

}
