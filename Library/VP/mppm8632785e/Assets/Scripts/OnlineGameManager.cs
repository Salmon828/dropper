using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;


// Handles score, gamestate 
public class OnlineGameManager : NetworkBehaviour
{

    [SerializeField]
    private GameObject trail;

    NetworkVariable<float> timer = new NetworkVariable<float>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    NetworkVariable<FixedString32Bytes> winText = new NetworkVariable<FixedString32Bytes>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

    NetworkVariable<int> score = new NetworkVariable<int>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    NetworkVariable<int> score2 = new NetworkVariable<int>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> freeze = new NetworkVariable<bool>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> p1Rematch = new NetworkVariable<bool>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> p2Rematch = new NetworkVariable<bool>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

    [SerializeField]
    public int scoreToWin = 3;

    [SerializeField]
    TextMeshProUGUI textScore;
    [SerializeField]
    TextMeshProUGUI textScore2;
    [SerializeField]
    public TextMeshProUGUI textCountdown;

    public int maxPlayers = 2;

    [SerializeField]
    private int countDownLen = 3;
    private bool countDownHappened = false;



    // Spawn positions and directions those positions correspond with, probably a way to do this with 1 data structure
    [SerializeField]
    private Vector3[] spawnPositions = new Vector3[4];
    [SerializeField]
    private Vector3[] spawnDirection = new Vector3[4];
    

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer) freeze.Value = true;

        // Clients react to score variable change
        if (IsClient)
        {
            score.OnValueChanged += onScoreChanged;
            score2.OnValueChanged += onScoreChanged;
            timer.OnValueChanged += onTimerChanged;
            winText.OnValueChanged += onWinTextChanged;

            updateScoreUI();

        }
    }
    // Update is called once per frame, only running on server instance
    void Update()
    {
        if (!IsServer) return;


        if (NetworkManager.Singleton.ConnectedClientsList.Count == maxPlayers && !countDownHappened && !p1Rematch.Value && !p2Rematch.Value)
        {
            // Players are connected and game is ready to start, **Modify later for lobbies**

            setStartPositions();

            timer.Value += Time.deltaTime;

            if (timer.Value >= countDownLen)
            {
                freeze.Value = false;
                countDownHappened = true;
                timer.Value = 0;
            }

           

        }
    }

    // Sets the players starting positions based on serialized data, should only be called from the server due to networktransforms
    void setStartPositions()
    {
        //if (!IsServer) return;
        int currentClient = 0;
        foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
        {
            OnlinePlayer p = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<OnlinePlayer>();
            p.rb.MovePosition(spawnPositions[currentClient]);
            p.netDirection.Value = spawnDirection[currentClient];
            currentClient++;
        }
    }
    void onScoreChanged(int oldVal, int newVal)
    {
        updateScoreUI();
    }
    void onTimerChanged(float oldVal, float newVal)
    {
        updateCountDownUI();
    }
    void onWinTextChanged(FixedString32Bytes oldVal, FixedString32Bytes newVal)
    {
        winScreen();
    }
    public void scoreUpdate(int isP1)
    {

        // 0 = true, 1 = false, others = tie
        if (isP1 == 0)
        {
            score2.Value += 1;
        }
        else if (isP1 == 1)
        {
            score.Value += 1;
        }
        else
        {
            score.Value += 1;
            score2.Value += 1;
        }

        freeze.Value = true;
        checkWin();

        // Reset round 
        setStartPositions();
        countDownHappened = false;

    }

    private void checkWin()
    {
        // Game ending logic
        if (score.Value >= scoreToWin && score2.Value >= scoreToWin)
        {
            // tie
            winText.Value = "TIE!";
            p1Rematch.Value = true;
            p2Rematch.Value = true;
            Debug.Log("Both win?");
        }
        else if (score.Value >= scoreToWin)
        {
            // p1 win
            winText.Value = "P1 Wins!";
            p1Rematch.Value = true;
            p2Rematch.Value = true;
            Debug.Log("P1 win");
        }
        else if (score2.Value >= scoreToWin)
        {
            // p2 win
            winText.Value = "P2 Wins!";
            p1Rematch.Value = true;
            p2Rematch.Value = true;
            Debug.Log("P2 win");
        }
    }

    void winScreen()
    {
        if(!IsClient) return;
        textCountdown.text = winText.Value.ToString();
        textCountdown.enabled = true;
    }
    void updateScoreUI()
    {
        if (!IsClient) return;
        textScore.text = score.Value.ToString();
        textScore2.text = score2.Value.ToString();
    }

    void updateCountDownUI()
    {
        if (!IsClient) return;
        textCountdown.text = timer.Value.ToString("F1");
        if(timer.Value > 0)
        {
            textCountdown.enabled = true;
        }
        else
        {
            textCountdown.enabled = false;
        }
    }
}
