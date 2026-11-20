using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Manager : MonoBehaviour
{
    public GameObject P1;
    private PlayerSquare P1Script;

    public static bool vsCPU = false; // Set by the menu before loading this scene

    [SerializeField] GameObject P2Human; // "Player (1)", used in Local Play
    [SerializeField] GameObject P2CPU;   // "Bot", used in Vs CPU
    private GameObject P2;
    private PlayerSquare P2Script;
    private Bot P2Bot; // Set instead of P2Script in the Vs CPU scene


    public GameObject trail;
    private float timer;
    private Color P1Color;
    private Color P2Color;
    public float dropRate = 0.3f;

    public static int score = 0;
    public static int score2 = 0;
    public int scoreToWin = 3;
    public TextMeshProUGUI textScore;
    public TextMeshProUGUI textScore2;

    public bool local = false;
    private bool roundOver = false; // Flag to make sure only one player scores per round

    [SerializeField]
    ParticleSystem part;

    [SerializeField] TextMeshProUGUI winText;
    [SerializeField] GameObject rematchButton;
    [SerializeField] GameObject quitButton;

    void Awake()
    {
        P2 = vsCPU ? P2CPU : P2Human;
        P2Human.SetActive(!vsCPU);
        P2CPU.SetActive(vsCPU);
        local = !vsCPU;

        // Point P2 at this manager in code, so the Inspector reference can't go missing
        if (P2Human.TryGetComponent(out PlayerSquare ps)) ps.manager = this;
        if (P2CPU.TryGetComponent(out Bot bot)) bot.manager = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!local)
        {
            P1Color = new Color32((byte)PlayerPrefs.GetInt("p1R"), (byte)PlayerPrefs.GetInt("p1G"), (byte)PlayerPrefs.GetInt("p1B"), 255);
            P1.GetComponent<SpriteRenderer>().color = P1Color;
        } else
        {
            P1Color = P1.GetComponent<SpriteRenderer>().color;
        }
        P2Color = P2.GetComponent<SpriteRenderer>().color;
        P1Script = P1.GetComponent<PlayerSquare>();
        P2Script = P2.GetComponent<PlayerSquare>();
        P2Bot = P2.GetComponent<Bot>();

        winText.enabled = false;
        rematchButton.SetActive(false);
        quitButton.SetActive(false);
        rematchButton.GetComponent<Button>().onClick.AddListener(OnRematchClicked);
        quitButton.GetComponent<Button>().onClick.AddListener(OnQuitClicked);
        updateScoreText();
    }

    // Update is called once per frame
    void Update()
    {
        if (timer < dropRate)
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

    public void collisonDetection(int player)
    {
        if (roundOver) return;
        roundOver = true;
        P1Script.direction = Vector3.zero;
        P1Script.freeze = true;
        if (P2Script != null)
        {
            P2Script.direction = Vector3.zero;
            P2Script.freeze = true;
        }
        if (P2Bot != null)
        {
            P2Bot.direction = Vector3.zero;
            P2Bot.freeze = true;
        }

        // Give point to opponent if you collide
        if (player == 1)
        {
            score2 += 1;
            playCrashParticles(P1.transform.position, P1Color);
        }
        else if (player == 2)
        {
            score += 1;
            playCrashParticles(P2.transform.position, P2Color);
        }
        else
        {
            score += 1;
            score2 += 1;
            playCrashParticles(P1.transform.position, P1Color);
            playCrashParticles(P2.transform.position , P2Color);
        }

        updateScoreText();
        string winMessage = checkWin();
        if (winMessage != null) showWinScreen(winMessage);
        else StartCoroutine(ResetWithDelay()); // Next round
    }

    // Returns the win message, or null if nobody has won yet
    private string checkWin()
    {
        bool p1Won = score >= scoreToWin;
        bool p2Won = score2 >= scoreToWin;
        if (p1Won && p2Won) return "Tie!";
        if (p1Won) return P2Bot != null ? "You Win!" : "P1 Wins!";
        if (p2Won) return P2Bot != null ? "CPU Wins!" : "P2 Wins!";
        return null;
    }

    private void showWinScreen(string message)
    {
        winText.text = message;
        winText.enabled = true;
        rematchButton.SetActive(true);
        quitButton.SetActive(true);
    }

    public static void ResetScores()
    {
        score = 0;
        score2 = 0;
    }

    private void OnRematchClicked()
    {
        ResetScores();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnQuitClicked()
    {
        ResetScores();
        SceneManager.LoadScene("Menu");
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
