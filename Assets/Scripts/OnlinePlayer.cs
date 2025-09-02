using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class OnlinePlayer : NetworkBehaviour
{
    //public NetworkVariable<Vector3> position = new NetworkVariable<Vector3>();

    InputAction Up, Down, Left, Right;
    public InputActionAsset actionAsset;
    private InputActionMap map;
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
        }
        else
        {
            Destroy(this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!map.enabled)
        {
            map.Enable();
        }
        if (Up.WasPerformedThisFrame())
        {
            direction = Vector3.up;
        }
        else if (Down.WasPerformedThisFrame())
        {
            direction = Vector3.down;
        }
        else if (Left.WasPerformedThisFrame())
        {
            direction = Vector3.left;
        }
        else if (Right.WasPerformedThisFrame())
        {
            direction = Vector3.right;
        }
        //position.Value = gameObject.transform.position;
        gameObject.transform.position += direction * moveSpeed * Time.deltaTime;
    }
}
