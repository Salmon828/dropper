using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSquare : MonoBehaviour
{
    public float moveSpeed = 10;
    public Vector3 direction = Vector3.right;
    public bool isP1 = true;

    public InputActionAsset actionAsset;
    InputAction Up, Down, Left, Right;

    public Manager manager;

    private InputActionMap map;

    void Awake()
    {
        map = actionAsset.FindActionMap("Player");
        if (isP1)
        {
            Up = map.FindAction("P1Up", true);
            Down = map.FindAction("P1Down", true);
            Left = map.FindAction("P1Left", true);
            Right = map.FindAction("P1Right", true);
        }
        else
        {
            Up = InputSystem.actions.FindAction("P2Up");
            Down = InputSystem.actions.FindAction("P2Down");
            Left = InputSystem.actions.FindAction("P2Left");
            Right = InputSystem.actions.FindAction("P2Right");
        }
    }

    private void Start()
    {
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
            gameObject.transform.position += direction * moveSpeed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Obstacle")
        {
            direction = Vector3.zero;
            manager.collisonDetection(isP1);
            
        }
    }

}
