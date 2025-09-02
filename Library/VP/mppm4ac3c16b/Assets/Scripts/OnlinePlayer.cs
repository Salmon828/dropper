using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class OnlinePlayer : NetworkBehaviour
{
    public NetworkVariable<Vector3> position = new NetworkVariable<Vector3>();
    public NetworkVariable<Vector3> netDirection = new NetworkVariable<Vector3>();

    InputAction Up, Down, Left, Right;
    public InputActionAsset actionAsset;
    private InputActionMap map;

    [SerializeField]
    private Vector3 direction = Vector3.right;
    public float moveSpeed = 10f;
   
    public override void OnNetworkSpawn()
    {

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
        else
        {
           enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
        if (Up.WasPerformedThisFrame())
        {
            direction = Vector3.up;
            changeDirectionRpc(direction);
        }
        else if (Down.WasPerformedThisFrame())
        {
            direction = Vector3.down;
            changeDirectionRpc(direction);
        }
        else if (Left.WasPerformedThisFrame())
        {
            direction = Vector3.left;
            changeDirectionRpc(direction);
        }
        else if (Right.WasPerformedThisFrame())
        {
            direction = Vector3.right;
            changeDirectionRpc(direction);
        }
        
        moveRequestRpc();
    }

    [Rpc(SendTo.Server)]
    void moveRequestRpc(RpcParams rpcParams = default)
    {
        Vector3 pos = position.Value;
        pos += netDirection.Value * moveSpeed * Time.deltaTime;
        position.Value = pos;
        transform.position = pos;
    }

    [Rpc(SendTo.Server)]
    void changeDirectionRpc(Vector3 newDir, RpcParams rpcParams = default)
    {
        netDirection.Value = newDir;
    }


}
