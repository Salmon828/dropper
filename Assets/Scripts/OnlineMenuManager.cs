using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Handles the UI components of the online menu
public class OnlineMenuManager : MonoBehaviour
{
    private VisualElement ui; // Root ui reference

    [SerializeField]
    private SessionManager sessionManager;

    // SessionManager is DontDestroyOnLoad, so prefer the live instance over the serialized
    // scene reference, which goes stale once we've been to the game scene and back.
    private SessionManager Session =>
        SessionManager.Instance != null ? SessionManager.Instance : sessionManager;

    // Ui elements
    private ListView lobbyList;
    private string selectedID; // Currently selected lobby's ID, used for joining.
    private Button hostButton, quickButton, refreshButton;
    private Label playerCountText;

    private QuerySessionsResults listResults;

    [SerializeField]
    private VisualTreeAsset rowTemplate;
    

    private void Awake()
    {
        ui = GetComponent<UIDocument>().rootVisualElement;
    }

    private void OnEnable()
    {
        lobbyList = ui.Q<ListView>("LobbyList");
        quickButton = ui.Q<Button>("QJButton");
        hostButton = ui.Q<Button>("HostButton");
        refreshButton = ui.Q<Button>("RefreshButton");
        playerCountText = ui.Q<Label>("Playercount");
        hostButton.clicked += OnHostButtonClicked;
        quickButton.clicked += OnQuickButtonClicked;
        refreshButton.clicked += OnRefreshButtonClicked;

        // Uses the template uxml to create each row.
        lobbyList.makeItem = () => rowTemplate.Instantiate();

        // Binds the data from querysessions to the specific text areas in the row.
        lobbyList.bindItem = (element, i) =>
        {
            var session = listResults.Sessions[i];
            string rowName = session?.Name ?? string.Empty;
            if (rowName.Length > 10)
            {
                rowName = rowName.Substring(0, 10);
            }
            element.Q<Label>("session-name").text = rowName;
            element.Q<Label>("session-count").text = $"{session.MaxPlayers - session.AvailableSlots}/{session.MaxPlayers}";
        };

        lobbyList.selectionChanged += OnSelectionChanged;

        BindEvents();
    }

    private void Start()
    {
        // NetworkManager assigns its Singleton in its own OnEnable, so this should def catch it
        BindEvents();
    }

    private bool networkBound;

    // We need to bind events with two different main sources that activate at different times
    private void BindEvents()
    {
        var nm = NetworkManager.Singleton;
        if (!networkBound && nm != null)
        {
            nm.OnClientConnectedCallback += HandleConnected;
            nm.OnServerStarted += HandleServerStarted;
            nm.OnClientDisconnectCallback += HandleDisconnected;
            networkBound = true;
        }

        var session = Session?.ActiveSession;
        if (session != null)
        {
            session.PlayerJoined -= UpdateRoomPlayCount; // Drop before adding, so repeat calls are safe
            session.PlayerHasLeft -= UpdateRoomPlayCount;
            session.PlayerJoined += UpdateRoomPlayCount;
            session.PlayerHasLeft += UpdateRoomPlayCount;
        }
    }

    private void UnbindEvents()
    {
        var nm = NetworkManager.Singleton;
        if (networkBound && nm != null)
        {
            nm.OnClientConnectedCallback -= HandleConnected;
            nm.OnServerStarted -= HandleServerStarted;
            nm.OnClientDisconnectCallback -= HandleDisconnected;
        }
        networkBound = false;

        var session = Session?.ActiveSession;
        if (session != null)
        {
            session.PlayerJoined -= UpdateRoomPlayCount;
            session.PlayerHasLeft -= UpdateRoomPlayCount;
        }
    }

    private void OnDisable()
    {
        hostButton.clicked -= OnHostButtonClicked;
        quickButton.clicked -= OnQuickButtonClicked;
        refreshButton.clicked -= OnRefreshButtonClicked;

        UnbindEvents();
    }

    public async void RepopulateList()
    {
        try
        { 
            var queryOptions = new QuerySessionsOptions(); // Used in the future for specific filters
            listResults = await MultiplayerService.Instance.QuerySessionsAsync(queryOptions);
            lobbyList.itemsSource = (System.Collections.IList)listResults.Sessions;
            lobbyList.RefreshItems();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async void OnHostButtonClicked()
    {
        try
        {
            await Session.StartSessionAsHost();
            BindEvents();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void OnStartGameClicked()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("NetworkGame", LoadSceneMode.Single);
        }
    }
    // This button has two functions
    private async void OnQuickButtonClicked()
    {
        // Join room functionality
        if (selectedID != null)
        {
            try
            {
               await Session.JoinSessionWithId(selectedID);
               BindEvents();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    private void OnRefreshButtonClicked()
    {
        RepopulateList();
    }

    // Network event handlers
    private async void HandleDisconnected(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        if (nm.IsServer)
        {
            UpdateRoomPlayCount("");
            return;
        }

        // Our own connection dropped, so release the session instead of leaving it stranded.
        if (clientId == nm.LocalClientId && Session != null)
        {
            await Session.LeaveSession();
        }
    }

    private void HandleServerStarted()
    {
        playerCountText.visible = true;
        hostButton.visible = false;
        refreshButton.visible = false;
        quickButton.visible = false;
    }

    private void HandleConnected(ulong obj)
    {
        Debug.Log(obj + " Handle Connected called");
        if (NetworkManager.Singleton.IsHost)
        {
            UpdateRoomPlayCount("");
            if (NetworkManager.Singleton.ConnectedClientsIds.Count >= SessionManager.MaxPlayers)
            {
                hostButton.text = "Start Game";
                hostButton.visible = true;
                hostButton.clicked -= OnHostButtonClicked;
                hostButton.clicked -= OnStartGameClicked; // Protection to avoid double subscribing
                hostButton.clicked += OnStartGameClicked;
            }
        }
        else if (NetworkManager.Singleton.IsConnectedClient)
        {
            hostButton.visible = false;
            refreshButton.visible = false;
            quickButton.visible = false;
            playerCountText.visible = true;
        }
    }
    private void UpdateRoomPlayCount(String player)
    {
        var nm = NetworkManager.Singleton;

        // ConnectedClientsIds is server-only, so clients fall back to the lobby roster.
        int connected = nm != null && nm.IsServer
            ? nm.ConnectedClientsIds.Count
            : Session?.ActiveSession?.Players.Count ?? 0;

        playerCountText.text = "Players: " + connected + "/" + SessionManager.MaxPlayers;
    }

    private void OnSelectionChanged(IEnumerable<object> selected)
    {
        //Debug.Log($"Selected: {string.Join(", ", selected)}");
        ISessionInfo info = selected.FirstOrDefault() as ISessionInfo;
        // Item deselected
        if (info == null)
        {
            selectedID = null;
            quickButton.text = "Quick Join";
        } else // Item selected
        {
            selectedID = info.Id;
            quickButton.text = "Join Room";
        }
    }
}
