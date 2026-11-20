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
    private Button backButton;
    private Label statusText;
    private IVisualElementScheduledItem hideStatus;
    private IVisualElementScheduledItem autoRefresh;
    private bool refreshing;
    private float nextAutoRefresh;
    private bool wasVisible;
    private enum LobbyState { Browsing, Connecting, Hosting, Joined }
    private LobbyState state = LobbyState.Browsing;
    private bool InLobby => state == LobbyState.Hosting || state == LobbyState.Joined;

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
        backButton = ui.Q<Button>("OnlineBack");
        statusText = ui.Q<Label>("StatusText");

        // Checks once a second, refreshes every 5s (well under the service's rate limit)
        autoRefresh?.Pause();
        autoRefresh = ui.schedule.Execute(AutoRefreshTick).Every(1000);
        backButton.clicked += OnBackClicked;

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
        backButton.clicked -= OnBackClicked;
        autoRefresh?.Pause();

        UnbindEvents();
    }

    private void AutoRefreshTick()
    {
        bool visible = ui.Q<VisualElement>("online").resolvedStyle.display == DisplayStyle.Flex;
        bool justOpened = visible && !wasVisible;
        wasVisible = visible;
        if (!visible || state != LobbyState.Browsing || refreshing) return;
        if (justOpened || Time.realtimeSinceStartup >= nextAutoRefresh) RepopulateList();
    }

    public async void RepopulateList()
    {
        if (refreshing) return;
        refreshing = true;
        nextAutoRefresh = Time.realtimeSinceStartup + 5f;
        try
        { 
            var queryOptions = new QuerySessionsOptions(); // Used in the future for specific filters
            listResults = await MultiplayerService.Instance.QuerySessionsAsync(queryOptions);
            lobbyList.itemsSource = (System.Collections.IList)listResults.Sessions;
            lobbyList.RefreshItems();

            // Keep the selection if that lobby still exists, otherwise clear it
            int index = -1;
            for (int i = 0; i < listResults.Sessions.Count; i++)
                if (listResults.Sessions[i].Id == selectedID) { index = i; break; }
            if (index >= 0)
            {
                lobbyList.SetSelectionWithoutNotify(new[] { index });
            }
            else
            {
                lobbyList.ClearSelection();
                selectedID = null;
                quickButton.text = "Quick Join";
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            ShowStatus("Couldn't load lobbies", true);
        }
        finally
        {
            refreshing = false;
        }
    }

    // Shows a message under the lobby list for a few seconds
    private void ShowStatus(string message, bool isError = false)
    {
        statusText.text = message;
        statusText.style.color = isError ? new Color(1f, 0.35f, 0.35f) : Color.white;
        statusText.style.display = DisplayStyle.Flex;
        hideStatus?.Pause();
        hideStatus = statusText.schedule.Execute(() => statusText.style.display = DisplayStyle.None).StartingIn(4000);
    }

    // Turns a service exception into something a player can understand
    private static string FriendlyError(Exception e, string fallback)
    {
        if (e is SessionException se)
        {
            switch (se.Error)
            {
                case SessionError.SessionNotFound:
                case SessionError.SessionDeleted:
                    return "That lobby no longer exists";
                case SessionError.RateLimitExceeded:
                    return "Too many requests, wait a moment";
                case SessionError.NotAuthorized:
                    return "Not signed in to online services";
            }
            string msg = se.Message.ToLowerInvariant();
            if (msg.Contains("not found")) return "That lobby no longer exists";
            if (msg.Contains("full")) return "That lobby is full";
        }
        return fallback;
    }

    // Greys out the online buttons while a host/join request is in flight
    private void SetButtonsEnabled(bool on)
    {
        hostButton.SetEnabled(on);
        quickButton.SetEnabled(on);
        refreshButton.SetEnabled(on);
        backButton.SetEnabled(on);
    }

    private async void OnHostButtonClicked()
    {
        if (state != LobbyState.Browsing) return;
        if (!Session.IsSignedIn) { ShowStatus("Not connected to online services", true); return; }
        state = LobbyState.Connecting;
        SetButtonsEnabled(false);
        try
        {
            await Session.StartSessionAsHost();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            ShowStatus(FriendlyError(e, "Couldn't create lobby"), true);
        }
        state = Session.ActiveSession != null ? LobbyState.Hosting : LobbyState.Browsing;
        SetButtonsEnabled(true);
        BindEvents();
    }

    private async void OnStartGameClicked()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            hostButton.SetEnabled(false); // Stop double clicks while we wait on the service
            await Session.LockSession();
            NetworkManager.Singleton.SceneManager.LoadScene("NetworkGame", LoadSceneMode.Single);
        }
    }
    // This button has two functions
    private async void OnQuickButtonClicked()
    {
        // Selected lobby: join it. Nothing selected: quick join any open lobby, or host one.
        if (state != LobbyState.Browsing) return;
        if (!Session.IsSignedIn) { ShowStatus("Not connected to online services", true); return; }
        bool quick = selectedID == null;
        state = LobbyState.Connecting;
        SetButtonsEnabled(false);
        try
        {
            if (quick) await Session.QuickJoinOrHost();
            else await Session.JoinSessionWithId(selectedID);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            ShowStatus(FriendlyError(e, quick ? "Couldn't find a game" : "Couldn't join lobby"), true);
        }
        var s = Session.ActiveSession;
        state = s == null ? LobbyState.Browsing : s.IsHost ? LobbyState.Hosting : LobbyState.Joined;
        SetButtonsEnabled(true);
        if (state == LobbyState.Browsing) RepopulateList(); // Join failed (e.g. lobby gone): refresh so it disappears
        else if (quick && state == LobbyState.Hosting) ShowStatus("No open lobbies, created one for you");
        BindEvents();
    }

    private void OnRefreshButtonClicked()
    {
        RepopulateList();
    }

    private async void OnBackClicked()
    {
        if (state == LobbyState.Connecting) return; // Button is disabled anyway, belt and braces
        if (!InLobby) // Browsing: go back to the game mode screen
        {
            ui.Q<VisualElement>("online").style.display = DisplayStyle.None;
            ui.Q<VisualElement>("gamemodes").style.display = DisplayStyle.Flex;
            return;
        }

        // In a lobby: leave it and go back to browsing
        backButton.SetEnabled(false);
        hostButton.SetEnabled(false);
        state = LobbyState.Browsing; // Set first, so the disconnects we're about to cause are ignored
        await Session.LeaveSession(); // Host deletes the lobby, client just leaves
        await WaitForNetworkShutdown();
        ResetLobbyUI();
    }

    // Shuts Netcode down and waits until it's fully stopped (max 5s), so the next Host can start cleanly
    private static async Task WaitForNetworkShutdown()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;
        if (nm.IsListening && !nm.ShutdownInProgress) nm.Shutdown();
        float giveUp = Time.realtimeSinceStartup + 5f;
        while (nm != null && (nm.ShutdownInProgress || nm.IsListening) && Time.realtimeSinceStartup < giveUp)
            await Task.Yield();
    }

    // Puts the online screen back to its browsing state
    private void ResetLobbyUI()
    {
        hostButton.text = "Host";
        hostButton.clicked -= OnStartGameClicked;
        hostButton.clicked -= OnHostButtonClicked;
        hostButton.clicked += OnHostButtonClicked;
        hostButton.SetEnabled(true);
        hostButton.visible = true;
        quickButton.visible = true;
        refreshButton.visible = true;
        playerCountText.visible = false;
        backButton.text = "Back";
        backButton.SetEnabled(true);
        RepopulateList();
    }

    // Network event handlers
    private async void HandleDisconnected(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        if (state == LobbyState.Hosting)
        {
            UpdateRoomPlayCount("");
            // Client left: hide Start Game until the lobby is full again
            if (nm.ConnectedClientsIds.Count < SessionManager.MaxPlayers)
                hostButton.visible = false;
        }
        else if (state == LobbyState.Joined)
        {
            // As a client, any disconnect means we lost the host
            state = LobbyState.Browsing;
            await Session.LeaveSession();
            await WaitForNetworkShutdown();
            ResetLobbyUI();
            ShowStatus("Host left the lobby");
        }
    }

    private void HandleServerStarted()
    {
        playerCountText.visible = true;
        hostButton.visible = false;
        refreshButton.visible = false;
        quickButton.visible = false;
        backButton.text = "Leave";
    }

    private void HandleConnected(ulong obj)
    {
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
            backButton.text = "Leave";
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
