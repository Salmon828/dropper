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

    private ListView lobbyList;
    private string selectedID; // Currently selected lobby's ID, used for joining.
    private Button hostButton, quickButton, refreshButton;

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
            await sessionManager.StartSessionAsHost();
            CheckIfInSession();
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
               await sessionManager.JoinSessionWithId(selectedID);
               CheckIfInSession();
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
        CheckIfInSession();
    }

    private void CheckIfInSession()
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                hostButton.text = "Start Game";
                refreshButton.visible = false;
                quickButton.visible = false;
                hostButton.clicked -= OnHostButtonClicked;
                hostButton.clicked += OnStartGameClicked;
            } else if (NetworkManager.Singleton.IsConnectedClient)
            {
                hostButton.visible = false;
                refreshButton.visible = false;
                quickButton.visible = false;
            }
        }
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
