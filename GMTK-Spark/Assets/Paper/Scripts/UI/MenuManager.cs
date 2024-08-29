using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using static GameManager;

public class MenuManager : Singleton<MenuManager>
{
    [Header("Menus")]
    [SerializeField] Menu mainMenu;
    [SerializeField] Menu pauseMenu;
    [SerializeField] Menu settingsMenu;
    [SerializeField] Menu levelSelectMenu;
    [SerializeField] Menu beatLevelMenu;
    [SerializeField] Menu creditsScreen;
    [SerializeField] Menu gameEndScreen;

    [Header("Cameras")]
    [SerializeField] Camera userInterfaceCamera;

    public enum MenuTypes { None, Main, Credits, Pause, Settings, LevelSelect, BeatLevel, GameEndScreen, }

    public bool IsGamePaused { get; private set; }

    readonly Menu emptyMenu = new();
    Menu previousMenu; 
    Menu currentMenu;

    readonly List<Menu> menusToClear = new();
    readonly List<Menu> menuHistory = new();
    int currentHistoryIndex = 0;

    KeyCode pauseKey = KeyCode.Escape;

    Dictionary<MenuTypes, Menu> MenuTypeToMenu;

    [Serializable]
    class Menu
    {
        [field: SerializeField] public Canvas Canvas { get; private set; } = new();
        [field: SerializeField] public List<GameObject> ObjectsToEnable { get; private set; } = new();
        [field: SerializeField] public List<GameObject> ObjectsToDisable { get; private set; } = new();
    }

    void Start()
    {
#if DEBUG
        pauseKey = KeyCode.P;
#endif

        MenuTypeToMenu = new()
        {
            { MenuTypes.Main,           mainMenu },
            { MenuTypes.Pause,          pauseMenu},
            { MenuTypes.GameEndScreen,  gameEndScreen},
            { MenuTypes.Settings,       settingsMenu},
            { MenuTypes.BeatLevel,      beatLevelMenu},
            { MenuTypes.LevelSelect,    levelSelectMenu},
            { MenuTypes.Credits,        creditsScreen},
        };

        RespondToGameStateChange(GameManager.Instance.CurrentState);
    }

    void OnEnable()
    {
        GameStateChangeEventHandler += HandleGameStateChange;
        UIButton.UIInteractEventHandler += HandleUIButtonInteract;
    }
    void OnDisable()
    {
        GameStateChangeEventHandler -= HandleGameStateChange;
        UIButton.UIInteractEventHandler -= HandleUIButtonInteract;
    }
    private void Update()
        => PauseGame();

    /// <summary>
    ///     Loads the menu appropriate to the current game state.
    /// </summary>
    void HandleGameStateChange(object sender, GameStateChangeEventArgs e)
        => RespondToGameStateChange(e.newState);

    void RespondToGameStateChange(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameState.RunGame:
                LoadMenu(mainMenu);
            break;

            case GameState.StartLevel:
            case GameState.RestartLevel:
                LoadMenu(emptyMenu);
            break;

            case GameState.BeatLevel:
                LoadMenu(gameEndScreen);
            break;
        }
    }

    void HandleUIButtonInteract(object sender, UIButton.UIInteractEventArgs e)
    {
        if (e.buttonEvent != UIButton.ButtonEventType.UI)
            return;
        LoadMenu(e.menuToOpen);
    }

    /// <summary>
    ///     Puase and unpause the game on escape press.
    /// </summary>
    void PauseGame()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            if (currentMenu == pauseMenu)
                LoadMenu(emptyMenu);
            else if (currentMenu == emptyMenu)
                LoadMenu(pauseMenu);
        }
    }

    /// <summary>
    ///     Loads the contents for a given menu type, while unloading the last loaded menu.
    /// </summary>
    /// <param name="menu"> Menu to load. </param>
    /// <param name="addToHistory" </param>
    void LoadMenu(MenuTypes menuType, bool addToHistory = true)
    {
        if (MenuTypeToMenu.TryGetValue(menuType, out Menu menu))
            LoadMenu(menu, addToHistory);
    }

    /// <summary>
    ///     Loads the contents for a given menu while unloading the last loaded menu.
    /// </summary>
    /// <param name="menu"> Menu to load. </param>
    /// <param name="addToHistory" </param>
    void LoadMenu(Menu menu, bool addToHistory = true)
    {
        if (addToHistory)
            menuHistory.Add(menu);
        
        previousMenu = currentMenu;
        currentMenu = menu;

        // Ready menu
        Debug.Log(menu.ToString());
        if (menu.Canvas != null)
            menu.Canvas.gameObject.SetActive(true);
        foreach (GameObject menuObject in menu.ObjectsToEnable)
            menuObject.SetActive(true);
        foreach (GameObject menuObject in menu.ObjectsToDisable)
            menuObject.SetActive(false);

        // Unready previous menu
        if (previousMenu != null)
        {
            previousMenu.Canvas.gameObject.SetActive(false);
            foreach (GameObject menuObject in previousMenu.ObjectsToEnable)
                menuObject.SetActive(false);
        }
        
        // Note this as a menu we have loaded
        if (!menusToClear.Contains(menu))
        {
            foreach (GameObject menuObject in menu.ObjectsToEnable)
                emptyMenu.ObjectsToDisable.Add(menuObject);
            menusToClear.Add(menu);
        }
    }

    /// <summary>
    ///     Loads the menu of the previous index in the menu history
    /// </summary>
    void LoadPreviousMenu()
    {
        if (currentHistoryIndex == 0)
            return;

        if (currentHistoryIndex >= menuHistory.Count)
            throw new Exception("Index out of bounds");

        menuHistory.RemoveAt(currentHistoryIndex);

        LoadMenu(menuHistory[currentHistoryIndex--], false);
    }
}