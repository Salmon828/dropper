using TMPro;
using Unity.Netcode;
using UnityEngine;

// Handles score, gamestate, and trail instatiation 
public class OnlineGameManager : NetworkBehaviour
{

    [SerializeField]
    private GameObject trail;

    private float timer;

    NetworkVariable<int> score = new NetworkVariable<int>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    NetworkVariable<int> score2 = new NetworkVariable<int>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

    [SerializeField]
    public int scoreToWin = 3;

    [SerializeField]
    TextMeshProUGUI textScore;
    [SerializeField]
    TextMeshProUGUI textScore2;


    public override void OnNetworkSpawn()
    {

        // Clients react to score variable change
        if (IsClient)
        {
            score.OnValueChanged += onScoreChanged;
            score2.OnValueChanged += onScoreChanged;

            updateScoreUI();

        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    void onScoreChanged(int oldVal, int newVal)
    {
        updateScoreUI();
    }
    public void scoreUpdate(bool isP1)
    {
        if (!IsServer) return;

        if (isP1)
        {
            score2.Value += 1;
        }
        else
        {
            score.Value += 1;
        }

        checkWin();
    }

    private void checkWin()
    {
        // Game ending logic
        if (score.Value >= scoreToWin && score2.Value >= scoreToWin)
        {
            // tie
            Debug.Log("Both win?");
        }
        else if (score.Value >= scoreToWin)
        {
            // p1 win
            Debug.Log("P1 win");
        }
        else if (score2.Value >= scoreToWin)
        {
            // p2 win
            Debug.Log("P2 win");
        }
    }
    void updateScoreUI()
    {
        if (!IsClient) return;
        textScore.text = score.Value.ToString();
        textScore2.text = score2.Value.ToString();
    }
    void SpawnTrails()
    {

    }
}
