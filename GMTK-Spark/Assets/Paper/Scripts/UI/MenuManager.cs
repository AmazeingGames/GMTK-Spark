using System;
using System.Collections.Generic;
using System.Data;
using UnityEditor;
using UnityEngine;
using static GameManager;

public class MenuManager : Singleton<MenuManager>
{
    [Header("Menus")]
    [SerializeField] Menu mainMenu;
    [SerializeField] Menu pauseMenu;
    [SerializeField] Menu settingsMenu;
    [SerializeField] Menu levelSelectMenu;
    [SerializeField] Menu beatLevelScreen;
    [SerializeField] Menu creditsScreen;
    [SerializeField] Menu gameEndScreen;

    [Header("Cameras")]
    [SerializeField] Camera userInterfaceCamera;

    public enum MenuTypes { None, Previous, Main, Credits, Pause, Settings, LevelSelect, BeatLevel, GameEndScreen, Empty }

    public bool IsGamePaused { get; private set; }

    Menu currentMenu;
    Menu previousMenu;
    readonly Menu emptyMenu = new();
    readonly List<Menu> menusToClear = new();
    readonly List<Menu> menuHistory = new();
    int currentHistoryIndex = -1;

    Dictionary<MenuTypes, Menu> MenuTypeToMenu;

    [Serializable]
    class Menu
    {
        public bool HasSetMenuType { get; private set; } = false;

        public void SetMenuType(MenuTypes menuType)
        {
            if (HasSetMenuType)
            {
                Debug.LogWarning($"Has already set this menu's type to {menuType}");
                return;
            }
            HasSetMenuType = true;
        }

        public void SetReady(bool ready)
        {
            if (Canvas != null)
                Canvas.gameObject.SetActive(ready);

            foreach (GameObject obj in EnableOnReady)
                obj.SetActive(ready);

            if (!ready)
                return;

            foreach (GameObject obj in DisableOnReady)
                obj.SetActive(false);

        }

        [field: SerializeField] public Canvas Canvas { get; private set; } = new();
        [field: SerializeField] public List<GameObject> EnableOnReady { get; private set; } = new();
        [field: SerializeField] public List<GameObject> DisableOnReady { get; private set; } = new();
    }

    void Start()
    {
        mainMenu.SetMenuType(MenuTypes.Main);
        pauseMenu.SetMenuType(MenuTypes.Pause);
        settingsMenu.SetMenuType(MenuTypes.Settings);
        levelSelectMenu.SetMenuType(MenuTypes.LevelSelect);
        beatLevelScreen.SetMenuType(MenuTypes.BeatLevel);
        creditsScreen.SetMenuType(MenuTypes.Credits);
        gameEndScreen.SetMenuType(MenuTypes.GameEndScreen);
        emptyMenu.SetMenuType(MenuTypes.Empty);

        MenuTypeToMenu = new()
        {
            { MenuTypes.Main,           mainMenu },
            { MenuTypes.Pause,          pauseMenu},
            { MenuTypes.GameEndScreen,  gameEndScreen},
            { MenuTypes.Settings,       settingsMenu},
            { MenuTypes.BeatLevel,      beatLevelScreen},
            { MenuTypes.LevelSelect,    levelSelectMenu},
            { MenuTypes.Credits,        creditsScreen},
            { MenuTypes.Empty,          emptyMenu},
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

    /// <summary>
    ///     Loads the menu appropriate to the current game state.
    /// </summary>
    void HandleGameStateChange(object sender, GameStateChangeEventArgs e)
        => RespondToGameStateChange(e.newState);

    void RespondToGameStateChange(GameManager.GameState newState)
    {
        Menu menuToLoad = newState switch
        {
            GameState.EnterMainMenu => mainMenu,
            GameState.PauseGame => pauseMenu,
            //GameState.ResumeGame or GameState.RestartLevel or GameState.StartLevel => emptyMenu,
            //GameState.BeatLevel => beatLevelScreen,
            _ => emptyMenu,
        };
        LoadMenu(menuToLoad);
    }

    /// <summary>
    ///     Loads the menu told by a clicked ui button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void HandleUIButtonInteract(object sender, UIButton.UIInteractEventArgs e)
    {
        if (e.buttonEvent != UIButton.UIEventTypes.UI)
            return;

        if (e.buttonInteraction != UIButton.UIInteractionTypes.Click)
            return;

        LoadMenu(e.menuToOpen);
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
        else if (menuType == MenuTypes.Previous)
            LoadPreviousMenu();
        else
            Debug.LogWarning($"Menu Type: {menuType} not covered by conditional statements");
    }

    /// <summary>
    ///     Loads the contents for a given menu while unloading the last loaded menu.
    /// </summary>
    /// <param name="menu"> Menu to load. </param>
    /// <param name="addToHistory" </param>
    void LoadMenu(Menu menu, bool addToHistory = true)
    {
        if (addToHistory)
        {
            menuHistory.Add(menu);
            currentHistoryIndex++;
        }

        previousMenu = currentMenu;
        currentMenu = menu;

        if (!menu.HasSetMenuType)
            throw new ArgumentException($"The menu type of {menu} has not been set");

        menu.SetReady(true);
        
        previousMenu?.SetReady(false);
        
        if (!menusToClear.Contains(menu))
        {
            foreach (GameObject menuObject in menu.EnableOnReady)
                emptyMenu.DisableOnReady.Add(menuObject);
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
            throw new Exception("Menu history index exceeds the menu history list");

        LoadMenu(menuHistory[--currentHistoryIndex], false);
        menuHistory.RemoveAt(currentHistoryIndex + 1);
    }
}