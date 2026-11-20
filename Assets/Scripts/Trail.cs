using UnityEngine;

// A single trail block. Becomes solid after a short grace period, then does nothing per frame.
public class Trail : MonoBehaviour
{
    [SerializeField] private float solidDelay = 0.2f; // Grace period so you don't hit your own new block
    private BoxCollider2D col;
    private SpriteRenderer sprite;

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    // solid = false skips collisions entirely (online clients only draw blocks, the server checks hits)
    public void Init(Color color, bool solid)
    {
        sprite.color = color;
        col.enabled = false;
        CancelInvoke();                                  // In case this block came back from the pool
        if (solid) Invoke(nameof(MakeSolid), solidDelay);
    }

    private void MakeSolid() => col.enabled = true;
}
