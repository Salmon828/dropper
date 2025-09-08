using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Manager : MonoBehaviour
{
    public GameObject P1;
    public GameObject P2;
    public GameObject trail;
    private float timer;
    private Color P1Color;
    private Color P2Color;

    public static int score = 0;
    public static int score2 = 0;
    public int scoreToWin = 3;
    public TextMeshProUGUI textScore;
    public TextMeshProUGUI textScore2;

    [SerializeField]
    ParticleSystem part;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        P1Color = new Color32((byte)PlayerPrefs.GetInt("p1R"), (byte)PlayerPrefs.GetInt("p1G"), (byte)PlayerPrefs.GetInt("p1B"), 255);
        P1.GetComponent<SpriteRenderer>().color = P1Color;
        P2Color = P2.GetComponent<SpriteRenderer>().color;
        updateScoreText();
    }

    // Update is called once per frame
    void Update()
    {
        if (timer < 2)
        {
            timer += Time.deltaTime;
        }
        else
        {
            dropTrail(P1, P1Color);
            dropTrail(P2, P2Color);
            timer = 0;
        }
    }

    void dropTrail(GameObject player, Color color)
    {
        GameObject tinst = Instantiate(trail, new Vector3(player.transform.position.x, player.transform.position.y, 0.0f), Quaternion.identity);
        tinst.GetComponent<SpriteRenderer>().color = color;
    }

    public void collisonDetection(bool isP1)
    {
        // Give point to opponent if you collide
        if (isP1)
        {
            score2 += 1;

            playCrashParticles(P1.transform.position, P1Color);
            P1.GetComponent<BoxCollider2D>().enabled = false;
            P2.GetComponent<BoxCollider2D>().enabled = false;
        }
        else
        {
            score += 1;

            playCrashParticles(P2.transform.position, P2Color);
            P1.GetComponent<BoxCollider2D>().enabled = false;
            P2.GetComponent<BoxCollider2D>().enabled = false;
        }

        // Game ending logic
        if (score >= scoreToWin && score2 >= scoreToWin)
        {
            // tie
            Debug.Log("Both win?");
        }
        else if (score >= scoreToWin)
        {
            // p1 win
            Debug.Log("P1 win");
        }
        else if (score2  >= scoreToWin)
        {
            // p2 win
            Debug.Log("P2 win");
        }

        updateScoreText();
        StartCoroutine(ResetWithDelay());
    }

    IEnumerator ResetWithDelay()
    {
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void playCrashParticles(Vector3 position, Color color)
    {
        part.transform.position = position;
        var main = part.main;
        main.startColor = color;
        part.Play();
    }
    private void updateScoreText()
    {
        textScore.text = score.ToString();
        textScore2.text = score2.ToString();
    }
}
