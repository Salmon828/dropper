using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    private VisualElement ui;

    // Main menu variables
    private VisualElement mainMenu, letterElement;
    private Button playButton, optionsButton, quitButton;

    // Color menu variables
    private VisualElement colorMenu;
    private ColorPicker p1Picker, p2Picker;
    private Button colorBack;

    // Mode select variables
    private VisualElement modeMenu;
    private Button localButton, onlineButton, vscpuButton;
    private Button modeBack;

    // Online menu reference
    private VisualElement onlineMenu;

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

        modeBack = ui.Q<Button>("ModeBack");
        modeBack.clicked += OnModeBackClicked;

        // Online
        onlineMenu = ui.Q<VisualElement>("online");

        // Color
        colorMenu = ui.Q<VisualElement>("color");
        colorBack = ui.Q<Button>("colorBack");
        colorBack.clicked += OnColorBackClicked;

        p1Picker = new ColorPicker(ui, "", 1);  // red, green, blue, colorDisp, rgbValues
        p2Picker = new ColorPicker(ui, "2", 2); // red2, green2, blue2, colorDisp2, rgbValues2
    }

    private void OnPlayButtonClicked()
    {
        hideMain();
        modeMenu.style.display = DisplayStyle.Flex;
    }

    private void OnModeBackClicked()
    {
        modeMenu.style.display = DisplayStyle.None;
        showMain();
    }

    private void OnLocalButtonClicked()
    {
        Manager.vsCPU = false;
        Manager.ResetScores();
        SceneManager.LoadScene("Game");
    }

    private void OnOnlineButtonClicked()
    {
        modeMenu.style.display = DisplayStyle.None;
        onlineMenu.style.display = DisplayStyle.Flex;
    }

    private void OnVsCPUButtonClicked()
    {
        Manager.vsCPU = true;
        Manager.ResetScores();
        SceneManager.LoadScene("Game");
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
        p1Picker.Save();
        p2Picker.Save();
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

    // Wires three sliders + preview + RGB label to one player's saved colour
    private class ColorPicker
    {
        private readonly int player;
        private readonly SliderInt red, green, blue;
        private readonly VisualElement preview;
        private readonly Label rgbText;

        public ColorPicker(VisualElement root, string suffix, int player)
        {
            this.player = player;
            red = root.Q<SliderInt>("red" + suffix);
            green = root.Q<SliderInt>("green" + suffix);
            blue = root.Q<SliderInt>("blue" + suffix);
            preview = root.Q<VisualElement>("colorDisp" + suffix);
            rgbText = root.Q<Label>("rgbValues" + suffix);

            red.RegisterValueChangedCallback(_ => Refresh());
            green.RegisterValueChangedCallback(_ => Refresh());
            blue.RegisterValueChangedCallback(_ => Refresh());
            Load();
        }

        // Show the saved colour (without firing the callbacks)
        public void Load()
        {
            Color32 saved = PlayerColor.Load(player);
            red.SetValueWithoutNotify(saved.r);
            green.SetValueWithoutNotify(saved.g);
            blue.SetValueWithoutNotify(saved.b);
            Refresh();
        }

        public void Save() => PlayerColor.Save(player, red.value, green.value, blue.value);

        private void Refresh()
        {
            rgbText.text = red.value + ", " + green.value + ", " + blue.value;
            preview.style.backgroundColor = (Color)new Color32((byte)red.value, (byte)green.value, (byte)blue.value, 255);
        }
    }
}
