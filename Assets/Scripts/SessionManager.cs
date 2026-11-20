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

    // False if Unity Services or anonymous sign-in failed at startup
    public bool IsSignedIn =>
        UnityServices.State == ServicesInitializationState.Initialized &&
        AuthenticationService.Instance.IsSignedIn;

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

    private static SessionOptions NewSessionOptions() =>
        new SessionOptions { MaxPlayers = MaxPlayers }.WithRelayNetwork();

    public async Task StartSessionAsHost()
    {
        ActiveSession = await MultiplayerService.Instance.CreateSessionAsync(NewSessionOptions());
        Debug.Log($"Session {ActiveSession.Id} created! Join code: {ActiveSession.Code}");
    }

    // Joins any open lobby, or hosts a new one if none are found within a few seconds
    public async Task QuickJoinOrHost()
    {
        var quick = new QuickJoinOptions { CreateSession = true, Timeout = TimeSpan.FromSeconds(3) };
        ActiveSession = await MultiplayerService.Instance.MatchmakeSessionAsync(quick, NewSessionOptions());
    }

    public async Task JoinSessionWithId(string id)
    {
        // Errors are passed up so the menu can tell the player what went wrong
        ActiveSession = await MultiplayerService.Instance.JoinSessionByIdAsync(id);
    }

    // Called when the match starts, so the session drops off the lobby list and can't be joined mid-game.
    public async Task LockSession()
    {
        if (ActiveSession == null || !ActiveSession.IsHost) return;
        try
        {
            var host = ActiveSession.AsHost();
            host.IsPrivate = true;
            host.IsLocked = true;
            await host.SavePropertiesAsync();
        }
        catch (Exception e)
        {
            Debug.LogException(e); // Not fatal, the game still starts
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
