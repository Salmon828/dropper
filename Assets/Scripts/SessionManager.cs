using UnityEngine;
using System;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Netcode;

public class SessionManager : MonoBehaviour
{
    public const int MaxPlayers = 2;

    // Survives the Menu -> NetworkGame load so the session can still be cleaned up on exit.
    public static SessionManager Instance { get; private set; }

    public ISession ActiveSession { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void Start()
    {
        if (Instance != this) return; // Duplicate from a scene reload, already being destroyed.

        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            Debug.Log($"Sign in anonymously succeeded! PlayerID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public async Task StartSessionAsHost()
    {
        var options = new SessionOptions
        {
            MaxPlayers = MaxPlayers

        }.WithRelayNetwork();

        ActiveSession = await MultiplayerService.Instance.CreateSessionAsync(options);
        Debug.Log($"Session {ActiveSession.Id} created! Join code: {ActiveSession.Code}");
    }

    public async Task JoinSessionWithId(string id)
    {
        try
        {
            ActiveSession = await MultiplayerService.Instance.JoinSessionByIdAsync(id);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    // The host deletes the session so it stops showing up in the lobby list, a client just leaves it.
    public async Task LeaveSession()
    {
        var session = ActiveSession;
        ActiveSession = null;

        if (session == null) return;

        try
        {
            if (session.IsHost) await session.AsHost().DeleteAsync();
            else await session.LeaveAsync();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    // Wire this to the "Quit to Menu" button.
    public void QuitToMenu()
    {
        StartCoroutine(QuitToMenuRoutine());
    }

    private IEnumerator QuitToMenuRoutine()
    {
        var leaving = LeaveSession();
        while (!leaving.IsCompleted) yield return null;

        var nm = NetworkManager.Singleton;
        if (nm != null)
        {
            if (nm.IsServer || nm.IsClient) nm.Shutdown();

            // NGO never cleans up duplicate NetworkManagers, so drop the DontDestroyOnLoad copy
            // and let the Menu scene's own instance claim the Singleton again.
            Destroy(nm.gameObject);
        }

        // Let the Destroy resolve before the Menu scene's NetworkManager runs its OnEnable,
        // otherwise it sees a non-null Singleton and refuses to take over.
        yield return null;

        SceneManager.LoadScene("Menu", LoadSceneMode.Single);
    }

    private async void OnApplicationQuit()
    {
        await LeaveSession();
    }
}
