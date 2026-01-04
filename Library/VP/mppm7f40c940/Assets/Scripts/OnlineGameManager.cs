using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


// Handles score, gamestate 
public class OnlineGameManager : NetworkBehaviour
{

    [SerializeField]
    private GameObject trail;

    NetworkVariable<float> timer = new NetworkVariable<float>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

    NetworkVariable<int> score = new NetworkVariable<int>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    NetworkVariable<int> score2 = new NetworkVariable<int>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> freeze = new NetworkVariable<bool>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> p1Rematch = new NetworkVariable<bool>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> p2Rematch = new NetworkVariable<bool>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    private bool rematchAllowed = true;

    [SerializeField]
    public int scoreToWin = 3;

    [SerializeField]
    TextMeshProUGUI textScore;
    [SerializeField]
    TextMeshProUGUI textScore2;
    [SerializeField]
    public TextMeshProUGUI textCountdown;

    [SerializeField]
    public GameObject rematchButton;

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
            rematchButton.GetComponent<Button>().onClick.AddListener(onRematchClick);

            updateScoreUI();

        }

        // Want the server to check whether both people are ready to rematch whenever the rematch button/value is changed.
        if (IsServer)
        {
            p1Rematch.OnValueChanged += onRematchChanged;
            p2Rematch.OnValueChanged += onRematchChanged;
        }
    }
    // Update is called once per frame, only running on server instance
    void Update()
    {
        if (!IsServer) return;


        if (NetworkManager.Singleton.ConnectedClientsList.Count == maxPlayers && !countDownHappened && rematchAllowed)
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

    [Rpc(SendTo.ClientsAndHost)]
    void hideRematchRpc()
    {
        rematchButton.SetActive(false);
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

    void onRematchChanged(bool oldVal, bool newVal)
    {
        Debug.Log("p1: " + p1Rematch.Value + " p2: " + p2Rematch);
        if (p1Rematch.Value && p2Rematch.Value)
        {
            rematchAllowed = true;
            hideRematchRpc();
            score.Value = 0;
            score2.Value = 0;
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
        //winScreen();
    }
    void onRematchClick()
    {
        rematchValueServerRpc((int)NetworkManager.LocalClientId, true);
    }

    [Rpc(SendTo.Server)]
    void rematchValueServerRpc(int player, bool state)
    {
        switch (player)
        {
            case 0:
                p1Rematch.Value = state; break;

            case 1:
                p2Rematch.Value = state; break ;
        }
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
            winScreen("TIE!");
            Debug.Log("Both win?");
        }
        else if (score2.Value >= scoreToWin)
        {
            // p1 win
            winScreen("P1 Wins!");
            Debug.Log("P1 win");
        }
        else if (score.Value >= scoreToWin)
        {
            // p2 win
            winScreen("P2 Wins!");
            Debug.Log("P2 win");
        }
    }

    // Is called when only when winning text is changed / a player wins
    void winScreen(string winMessage)
    {
        rematchAllowed = false;
        p1Rematch.Value = false;
        p2Rematch.Value = false;
        updateWinScreenRPC(winMessage);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void updateWinScreenRPC(string winMessage)
    {
        textCountdown.text = winMessage;
        textCountdown.enabled = true;
        rematchButton.SetActive(true);
    }
    void updateScoreUI()
    {
        if (!IsClient) return;
        textScore.text = score2.Value.ToString();
        textScore2.text = score.Value.ToString();
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
