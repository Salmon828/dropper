using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class OnlinePlayer : NetworkBehaviour
{
    public NetworkVariable<Vector3> netDirection = new NetworkVariable<Vector3>();

    InputAction Up, Down, Left, Right;
    public InputActionAsset actionAsset;
    private InputActionMap map;

    [SerializeField]
    private Vector3 direction = Vector3.right;
    public float moveSpeed = 10f;

    // Variables that limit the rate at which clients can send data to the server
    private float sendInterval = 1f / 20f;
    private float sendTimer;

    private Rigidbody2D rb;
    public override void OnNetworkSpawn()
    {
        // To allow first frame movement
        sendTimer = sendInterval;
        rb = GetComponent<Rigidbody2D>();

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

        Vector3 newPos = netDirection.Value * moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(transform.position + newPos);
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


}
