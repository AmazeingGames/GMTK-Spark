using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
    [SerializeField] Menu diaryScreen;
    [SerializeField] Menu creditsScreen;
    [SerializeField] Menu gameEndScreen;

    [Header("Cameras")]
    [SerializeField] Camera userInterfaceCamera;


    [field: Header("Button Properties")]
    [field: SerializeField] public float RegularScale { get; private set; } = 1.0f;
    [field: SerializeField] public float HoverScale { get; private set; } = 1.1f;

    [field: Range(0, 1)][field: SerializeField] public float RegularOpacity { get; private set; } = .66f;
    [field: Range(0, 1)][field: SerializeField] public float HoverOpacity { get; private set; } = 1;

    [field: Header("Button Lerp Properties")]
    [field: SerializeField] public float ButtonLerpSpeed { get; private set; } = 8;
    [field: SerializeField] public float UnderlineLerpSpeed { get; private set; } = 8;
    [field: SerializeField] public AnimationCurve ButtonLerpCurve { get; private set; }
    [field: SerializeField] public AnimationCurve UnderlineLerpCurve { get; private set; }

    public enum MenuTypes { None, Previous, MainMenu, Credits, Pause, Settings, LevelSelect, BeatLevel, GameEndScreen, Empty, Diary }

    public static event EventHandler<MenuChangeEventArgs> MenuChangeEventHandler;

    public class MenuChangeEventArgs
    {
        public readonly MenuTypes newMenuType;
        public readonly MenuTypes previousMenuType;
        public MenuChangeEventArgs(MenuTypes newMenuType, MenuTypes previousMenuType)
        {
            this.newMenuType = newMenuType;
            this.previousMenuType = previousMenuType;
        }
    }

    public bool IsGamePaused { get; private set; }

    Menu currentMenu;
    Menu previousMenu;

    MenuTypes currentMenuType;
    MenuTypes previousMenuType;

    readonly Menu emptyMenu = new();
    readonly List<Menu> menuHistory = new();
    int currentHistoryIndex = -1;

    Dictionary<MenuTypes, Menu> MenuTypeToMenu;
    Dictionary<Menu, MenuTypes> MenuToMenuType;

    [Serializable]
    class Menu
    {
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

    void Awake()
    {
        base.Awake();

        MenuTypeToMenu = new()
        {
            { MenuTypes.MainMenu,       mainMenu },
            { MenuTypes.Pause,          pauseMenu},
            { MenuTypes.GameEndScreen,  gameEndScreen},
            { MenuTypes.Settings,       settingsMenu},
            { MenuTypes.BeatLevel,      beatLevelScreen},
            { MenuTypes.LevelSelect,    levelSelectMenu},
            { MenuTypes.Credits,        creditsScreen},
            { MenuTypes.Empty,          emptyMenu},
            { MenuTypes.Diary,          diaryScreen},
        };

        MenuToMenuType = MenuTypeToMenu.ToDictionary(x => x.Value, x => x.Key);

        foreach (var menu in MenuTypeToMenu.Values)
        {
            if (menu.Canvas != null)
                emptyMenu.DisableOnReady.Add(menu.Canvas.gameObject);
        }

        // Ignores following MenuTypes: 'Previous', 'None', 
        if (MenuTypeToMenu.Count < Enum.GetNames(typeof(MenuTypes)).Length - 2)
            throw new Exception("Not all enums are counted for in the MenuTypeToMenu dictionary");
        UpdateMenusToGameAction(GameManager.Instance.LastGameAction);
    }

    void OnEnable()
    {
        GameStateChangeEventHandler += HandleGameStateChange;
        UIButton.UIInteractEventHandler += HandleUIButtonInteract;
        GameManager.GameActionEventHandler += HandleGameAction;
    }
    void OnDisable()
    {
        GameStateChangeEventHandler -= HandleGameStateChange;
        UIButton.UIInteractEventHandler -= HandleUIButtonInteract;
        GameManager.GameActionEventHandler -= HandleGameAction;
    }

    void HandleGameAction(object sender, GameActionEventArgs e)
        => UpdateMenusToGameAction(e.gameAction);

    /// <summary>
    ///     Loads the menu appropriate to the current game state.
    /// </summary>
    void HandleGameStateChange(object sender, GameStateChangeEventArgs e)
    {
    }

    /// <summary>
    ///     Loads a menu appropraite to the current game action.
    /// </summary>
    void UpdateMenusToGameAction(GameManager.GameAction action)
    {
        Menu menuToLoad = action switch
        {
            GameManager.GameAction.EnterMainMenu => mainMenu,
            GameManager.GameAction.PauseGame => pauseMenu,
            GameManager.GameAction.BeatGame => gameEndScreen,
            GameManager.GameAction.CompleteLevel => beatLevelScreen,
            _ => emptyMenu,
        };

        Debug.Log($"Menu Manager: Handled Game Action {action} and loaded menu of type: {(MenuToMenuType.TryGetValue(menuToLoad, out MenuTypes value) == true ? value : MenuTypes.None)}");
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
    ///     Loads a menu type, while unloading the previous menu.
    /// </summary>
    /// <param name="menu"> Menu to load. </param>
    /// <param name="addToHistory"> If we are entering a nested menu. </param>
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
    ///     Loads a menu while unloading the previous menu.
    /// </summary>
    /// <param name="menu"> Menu to load. </param>
    /// <param name="addToHistory"> If we are entering a nested menu.  </param>
    void LoadMenu(Menu menu, bool addToHistory = true)
    {
        if (addToHistory)
        {
            menuHistory.Add(menu);
            currentHistoryIndex++;
        }

        if (menu == currentMenu)
            Debug.LogWarning("Menu Manager: Should not be trying to load an already loaded menu");

        previousMenu = currentMenu;
        currentMenu = menu;

        if (currentMenu != null && MenuToMenuType.TryGetValue(currentMenu, out MenuTypes currentType))
            currentMenuType = currentType;
        if (previousMenu != null && MenuToMenuType.TryGetValue(previousMenu, out MenuTypes previousType))
            previousMenuType = previousType;

        previousMenu?.SetReady(false);
        menu.SetReady(true);
        
        
        if (menu.Canvas != null && !emptyMenu.DisableOnReady.Contains(menu.Canvas.gameObject))
        {
            foreach (GameObject menuObject in menu.EnableOnReady)
                emptyMenu.DisableOnReady.Add(menuObject);
            emptyMenu.DisableOnReady.Add(menu.Canvas.gameObject);
        }
        
        OnMenuChange(currentMenuType, previousMenuType);   
    }

    void OnMenuChange(MenuTypes newMenuType, MenuTypes previousMenuType)
        => MenuChangeEventHandler?.Invoke(this, new(newMenuType, previousMenuType));

    /// <summary>
    ///     Loads the last loaded menu.
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