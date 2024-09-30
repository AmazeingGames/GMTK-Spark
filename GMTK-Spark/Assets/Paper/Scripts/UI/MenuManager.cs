using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
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
    [SerializeField] LayerMask uIOnlyCullingMask;
    [SerializeField] LayerMask allSeeingCullingMask;

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

    MenuTypes nextInQueue;

    public class MenuChangeEventArgs
    {
        public readonly MenuTypes newMenuType;
        public readonly MenuTypes previousMenuType;
        public readonly bool isAMenuEnabled;
        public MenuChangeEventArgs(MenuTypes newMenuType, MenuTypes previousMenuType, bool isAMenuEnabled)
        {
            this.newMenuType = newMenuType;
            this.previousMenuType = previousMenuType;
            this.isAMenuEnabled = isAMenuEnabled;
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
    readonly List<Menu> menus = new();


    [Serializable]
    class Menu
    {
        [field: SerializeField] public Canvas Canvas { get; private set; } = new();
        [field: SerializeField] public List<GameObject> EnableOnReady { get; private set; } = new();
        [field: SerializeField] public List<GameObject> DisableOnReady { get; private set; } = new();
        [field: SerializeField] public ScreenTransitions.OthogonalDirection SlideDirection { get; private set; }
        [field: SerializeField] public bool canSeePaper { get; private set; }
        public Transform Elements { get; private set; }

        bool isReady = false;

        public static event Action<bool> SetCanvas;

        public void SetReady(bool ready)
        {              
            if (ready == isReady)
            {
                Debug.Log($"Trying to set ready to {ready}, when ready is already {isReady}");
                return;
            }

            isReady = ready;
            if (Canvas != null)
            {
                Debug.Log($"{Canvas.name} starting transition to being set : {ready}");
                ScreenTransitions.Instance.StartTransition(Elements, ready, SlideDirection);
            }
            ScreenTransitions.Instance.StartCoroutine(SetObjectsAndCanvas(ready));

            if (!ready)
                return;

            string disabledObjects = string.Empty;
            foreach (GameObject obj in DisableOnReady)
            {
                disabledObjects += obj.name + ", ";
                obj.SetActive(false);
            }
            Debug.Log($"Disabled the following objects on ready: {(disabledObjects == string.Empty ? "none" : disabledObjects[..^2])}");
        }

        IEnumerator SetObjectsAndCanvas(bool ready)
        {
            if (ready)
            {
                if (Canvas != null)
                {
                    Canvas.gameObject.SetActive(ready);
                    Debug.Log($"Invoked set canvas {ready}");
                    SetCanvas?.Invoke(ready);
                }
                foreach (GameObject obj in EnableOnReady)
                    obj.SetActive(ready);
            }
               
            while (ScreenTransitions.Instance.IsTransitioning)
                yield return null;

            if (!ready)
            {
                if (Canvas != null)
                {
                    Canvas.gameObject.SetActive(ready);
                    Debug.Log($"Invoked set canvas {ready}");
                    SetCanvas?.Invoke(ready);
                    Debug.Log($"Disabled canvas: {Canvas.name}");
                }
                foreach (GameObject obj in EnableOnReady)
                    obj.SetActive(ready);
            }
        }

       public void Init()
        {
            if (Canvas == null)
            {
                Debug.Log("Could not initialize null canvas");
                return;
            }
            
            for (int i = 0; i < Canvas.transform.childCount; i++)
            {
                var child = Canvas.transform.GetChild(i);

                if (child.name == "Elements")
                    Elements = child;
            }
        }
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
            { MenuTypes.Diary,          diaryScreen},
        };

        MenuToMenuType = MenuTypeToMenu.ToDictionary(x => x.Value, x => x.Key);
        menus.AddRange(MenuTypeToMenu.Values);

        // Identifies menu elements to be cleared when we're loading a new menu
        foreach (var menu in MenuTypeToMenu.Values)
        {
            if (menu.Canvas != null)
                emptyMenu.DisableOnReady.Add(menu.Canvas.gameObject);
        }

        foreach (var menu in menus)
            menu.Init();

        // Ignores following MenuTypes: 'Previous', 'None', 'Empty'
        if (MenuTypeToMenu.Count < Enum.GetNames(typeof(MenuTypes)).Length - 3)
            throw new Exception("Not all enums are counted for in the MenuTypeToMenu dictionary");
        UpdateMenusToGameAction(GameManager.Instance.LastGameAction);

    }

    private void Update()
    {
        if (nextInQueue != MenuTypes.None && !ScreenTransitions.Instance.IsTransitioning)
        {
            LoadMenu(nextInQueue);
            nextInQueue = MenuTypes.None;
        }    
    }

    void OnEnable()
    {
        GameStateChangeEventHandler += HandleGameStateChange;
        UIButton.UIInteractEventHandler += HandleUIButtonInteract;
        GameManager.GameActionEventHandler += HandleGameAction;
        Menu.SetCanvas += HandleSetCanvas;
    }
    void OnDisable()
    {
        GameStateChangeEventHandler -= HandleGameStateChange;
        UIButton.UIInteractEventHandler -= HandleUIButtonInteract;
        GameManager.GameActionEventHandler -= HandleGameAction;
        Menu.SetCanvas -= HandleSetCanvas;
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
    ///     Checks if there's currently an active canvas in the scene.
    ///     Sets the UI camera and level camera active based on that.
    /// </summary>
    /// <param name="ready"> The SetActive() property the canvas was set to. </param>
    void HandleSetCanvas(bool ready)
    {
        bool isAMenuEnabled = false;

        string activeCanvases = string.Empty;

        // We could save performance by skipping the loop on (ready == true)
        foreach (var menu in menus)
        {
            if (menu.Canvas.gameObject.activeInHierarchy)
            {
                activeCanvases += menu.Canvas.name;
                isAMenuEnabled = true;
            }
        }
        Debug.Log($"The following canvases are active: {(activeCanvases == string.Empty ? "none" : activeCanvases[..^2])}");

        userInterfaceCamera.gameObject.SetActive(isAMenuEnabled);
        OnMenuChange(currentMenuType, previousMenuType, isAMenuEnabled);
    }

    /// <summary>
    ///     Loads a menu appropraite to the current game action.
    /// </summary>
    void UpdateMenusToGameAction(GameManager.GameAction action)
    {
        MenuTypes menuToLoad = action switch
        {
            GameManager.GameAction.EnterMainMenu => MenuTypes.MainMenu,
            GameManager.GameAction.PauseGame => MenuTypes.Pause,
            GameManager.GameAction.BeatGame => MenuTypes.GameEndScreen,
            GameManager.GameAction.CompleteLevel => MenuTypes.BeatLevel,
            _ => MenuTypes.Empty,
        };

        Debug.Log($"Menu Manager: Handled Game Action {action} and loaded menu of type: {menuToLoad}");

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
    void LoadMenu(MenuTypes menuType, bool addToHistory = true, bool addToQueue = true)
    {
        // In the future I would like the game to smoothly switch between screen transitions
        if (ScreenTransitions.Instance.IsTransitioning)
        {
            Debug.LogWarning("Can not change menus during screen transition.");
            if (addToQueue)
                nextInQueue = menuType;
            return;
        }

        if (MenuTypeToMenu.TryGetValue(menuType, out Menu menu))
            LoadMenu(menu, addToHistory);
        else if (menuType == MenuTypes.Previous)
            LoadPreviousMenu();
        // Unload all menus
        else if (menuType == MenuTypes.Empty)
            foreach (var current in menus)
                current.SetReady(false);
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


        if (menu.canSeePaper)
            userInterfaceCamera.cullingMask = allSeeingCullingMask;
        else
            userInterfaceCamera.cullingMask = uIOnlyCullingMask;

        if (menu.Canvas != null && !emptyMenu.DisableOnReady.Contains(menu.Canvas.gameObject))
        {
            foreach (GameObject menuObject in menu.EnableOnReady)
                emptyMenu.DisableOnReady.Add(menuObject);

            emptyMenu.DisableOnReady.Add(menu.Canvas.gameObject);
        }
    }

    void OnMenuChange(MenuTypes newMenuType, MenuTypes previousMenuType, bool isAMenuEnabled)
        => MenuChangeEventHandler?.Invoke(this, new(newMenuType, previousMenuType, isAMenuEnabled));

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