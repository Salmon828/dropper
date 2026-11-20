using UnityEngine;

public class Trail : MonoBehaviour
{
    private float timer;
    // Update is called once per frame
    void Update()
    {
        if (timer < 0.2)
        {
            timer += Time.deltaTime;
        }
        else if (gameObject.GetComponent<BoxCollider2D>().enabled == false)
        {
            gameObject.GetComponent<BoxCollider2D>().enabled = true;
        }
    }
}
