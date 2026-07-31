using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    private VisualElement ui;
    public InputActionAsset asset;

    // Main menu variables
    private VisualElement mainMenu, letterElement;
    private Button playButton, optionsButton, quitButton;

    // Color menu variables
    private VisualElement colorMenu, colorDisp;
    private SliderInt red, green, blue;
    private Label rgbVals;
    private Button colorBack;

    // Mode select variables
    private VisualElement modeMenu;
    private Button localButton, onlineButton, vscpuButton;

    private void Awake()
    {
        ui = GetComponent<UIDocument>().rootVisualElement;
    }

    private void OnEnable()
    {
        // Main
        letterElement = ui.Q<Image>("Title");
        ui.schedule.Execute(RevealTitle).StartingIn(2000);

        mainMenu = ui.Q<VisualElement>("main");
        playButton = ui.Q<Button>("PlayButton");
        playButton.clicked += OnPlayButtonClicked;

        optionsButton = ui.Q<Button>("OptionsButton");
        optionsButton.clicked += OnOptionsButtonClicked;

        quitButton = ui.Q<Button>("QuitButton");
        quitButton.clicked += OnQuitButtonClicked;

        // Mode
        modeMenu = ui.Q<VisualElement>("gamemodes");
        localButton = ui.Q<Button>("LocalButton");
        localButton.clicked += OnLocalButtonClicked;

        onlineButton = ui.Q<Button>("OnlineButton"); 
        onlineButton.clicked += OnOnlineButtonClicked;

        vscpuButton = ui.Q<Button>("VsCPUButton");
        vscpuButton.clicked += OnVsCPUButtonClicked;

        // Color
        colorMenu = ui.Q<VisualElement>("color");
        colorBack = ui.Q<Button>("colorBack");
        colorBack.clicked += OnColorBackClicked;

        red = ui.Q<SliderInt>("red");
        green = ui.Q<SliderInt>("green");
        blue = ui.Q<SliderInt>("blue");

        colorDisp = ui.Q<VisualElement>("colorDisp");

        rgbVals = ui.Q<Label>("rgbValues");
        red.RegisterValueChangedCallback(v => { rgbVals.text = rgbLabel(v.newValue, green.value, blue.value); 
            updateColor(v.newValue, green.value, blue.value); 
        });
        green.RegisterValueChangedCallback(v => { rgbVals.text = rgbLabel(red.value, v.newValue, blue.value);
            updateColor(red.value, v.newValue, blue.value);
        });
        blue.RegisterValueChangedCallback(v => { rgbVals.text = rgbLabel(red.value, green.value, v.newValue);
            updateColor(red.value, green.value, v.newValue);
        });
    }

    private void OnPlayButtonClicked()
    {
        hideMain();
        modeMenu.style.display = DisplayStyle.Flex;
    }

    private void OnLocalButtonClicked()
    {

    }

    private void OnOnlineButtonClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 2);
    }

    private void OnVsCPUButtonClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private void RevealTitle()
    {
        letterElement.AddToClassList("letter-shown");
    }
    
    private void OnOptionsButtonClicked()
    {
        hideMain();
        colorMenu.style.display = DisplayStyle.Flex;
    }

    private void OnQuitButtonClicked()
    {
        Application.Quit();
    }

    private void OnColorBackClicked()
    {
        showMain();
        colorMenu.style.display = DisplayStyle.None;
        PlayerPrefs.SetInt("p1R", red.value);
        PlayerPrefs.SetInt("p1G", green.value);
        PlayerPrefs.SetInt("p1B", blue.value);
    }

    private void hideMain()
    {
        mainMenu.style.display = DisplayStyle.None;
        letterElement.style.display = DisplayStyle.None;
    }

    private void showMain()
    {
        mainMenu.style.display = DisplayStyle.Flex;
        letterElement.style.display = DisplayStyle.Flex;
    }

    // returns rgb string to set text to 
    private string rgbLabel (int red, int green, int blue)
    {
        return red + ", " + green +  ", " + blue;
    }

    private void updateColor(int red, int green, int blue)
    {
        Color c = new Color32((byte)red, (byte)green, (byte)blue, 255);
        colorDisp.style.backgroundColor = c;
    }

    private IEnumerator EnableAndLoad()
    {
        yield return null;

        InputActionMap ui = asset.FindActionMap("UI");
        InputActionMap player = asset.FindActionMap("Player", true);

        player.Enable();
        ui.Disable();

        Time.timeScale = 1.0f;

        Debug.Log(player.enabled);
        yield return SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1);

        
        if (!player.enabled)
        {
            player.Enable();
            Debug.Log(player.enabled);
        }
    }
}
