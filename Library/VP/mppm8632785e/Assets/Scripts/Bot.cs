using UnityEngine;

public class Bot : MonoBehaviour
{
    public Vector3 direction = Vector3.left;
    public float moveSpeed = 50f;
    public LayerMask layerMask;
    public float xBound = 19f;
    public float yBound = 10.5f;

    public float consecSwitchTime = 0.2f;

    public Manager manager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.position += direction * moveSpeed * Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (Physics2D.CircleCast(transform.position, 0.3f, direction, Random.Range(0.5f, 3.0f), layerMask))
        {
            //Debug.Log("Object in front");
            direction = randomVector();
        }
    }

    // picks a new vector that isn't either the current direction or directly reverse
    private Vector3 randomVector()
    {
        int random = Random.Range(0, 4);
        Vector3 returnVector = Vector3.zero;

        switch(random)
        {
            case 0:
                returnVector = Vector3.left; break;
            case 1:
                returnVector = Vector3.right; break;
            case 2:
                returnVector = Vector3.up; break;
            case 3:
                returnVector = Vector3.down; break;
        }

        if (returnVector == direction)
        {
            return randomVector();
        }
        else
        {
            return returnVector;
        }

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log(collision);
        if (collision.tag == "Obstacle")
        {
            direction = Vector3.zero;
            this.enabled = false;
            manager.collisonDetection(false);
        }
    }

}
