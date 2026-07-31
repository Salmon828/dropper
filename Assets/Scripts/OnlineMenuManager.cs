using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UIElements;
using System;

// Handles the UI components of the online menu
public class OnlineMenuManager : MonoBehaviour
{
    private VisualElement ui; // Root ui reference

    [SerializeField]
    private SessionManager sessionManager;

    private ListView lobbyList;
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

        lobbyList.makeItem = () => rowTemplate.Instantiate();
        //{
        //    var row = new VisualElement();
        //    row.AddToClassList("session-row");
        //    var nameLabel = new Label { name = "session-name" };
        //    var countLabel = new Label { name = "session-count" };
        //    row.Add(nameLabel);
        //    row.Add(countLabel);
        //    return row;
        //};

        lobbyList.bindItem = (element, i) =>
        {
            var session = listResults.Sessions[i];
            string rowName = session.Name;
            if (session != null && session.Name.Length > 10)
            {
                rowName = session.Name.Substring(0, 10);
            }
            element.Q<Label>("session-name").text = rowName;
            element.Q<Label>("session-count").text = $"{session.MaxPlayers - session.AvailableSlots}/{session.MaxPlayers}";
        };
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
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    private void OnQuickButtonClicked()
    {

    }

    private void OnRefreshButtonClicked()
    {
        RepopulateList();
    }
}
