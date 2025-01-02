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
    List<Menu> menuHistory = new();
    List<Menu> nestedMenuHistory = new();
    int currentHistoryIndex = -1;

    Dictionary<MenuTypes, Menu> MenuTypeToMenu;
    Dictionary<Menu, MenuTypes> MenuToMenuType;
    readonly List<Menu> menus = new();


    [Serializable]
    class Menu
    {
        [field: SerializeField] public Canvas Canvas { get; private set; } = new();
        [field: SerializeField] public List<GameObject> ObjectsToEnableOnReady { get; private set; } = new();
        [field: SerializeField] public List<Menu> MenusToDisableOnReady { get; private set; } = new();
        [field: SerializeField] public ScreenTransitions.OthogonalDirection SlideDirection { get; private set; }
        [field: SerializeField] public bool canSeePaper { get; private set; }
        public Transform CanvasElements { get; private set; }
        public MenuTypes menuType { get; private set; }

        bool isReady = false;

        public static event Action<Menu, bool> SetCanvasAction;

        /// <summary>
        ///    Plays a transition animation before enabling/disabling the canvas.
        /// </summary>
        /// <param name="setActive"></param>
        public void SetCanvas(bool setActive, bool needsToMoveOutOfFrame = false, bool wasNested = false)
        {              
            if (setActive == isReady)
            {
                Debug.Log($"Trying to {(setActive ? "enable" : "disable")} canvas, when canvas is already {(isReady ? "enabled" : "disabled")}");
                return;
            }

            isReady = setActive;
            if (CanvasElements != null)
            {
                Debug.Log($"{Canvas.name} starting transition to being set : {setActive} | direction : {SlideDirection} | needsToMoveOutOfFrame : {needsToMoveOutOfFrame} | wasNested : {wasNested}");
                ScreenTransitions.Instance.StartTransition(CanvasElements, setActive, SlideDirection, needsToMoveOutOfFrame, wasNested);
            }

            ScreenTransitions.Instance.StartCoroutine(SetObjectsAndCanvas(setActive));

            if (setActive)
            {
                string disabledMenus = string.Empty;
                foreach (Menu menu in MenusToDisableOnReady)
                {
                    disabledMenus += menu.Canvas.name + ", ";
                    menu.SetCanvas(false);
                }
                Debug.Log($"Disabled the following menus on ready: {(disabledMenus == string.Empty ? "none" : disabledMenus[..^2])}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ready"></param>
        /// <returns></returns>
        IEnumerator SetObjectsAndCanvas(bool ready)
        {
            // Instantly enables the canvas
            if (ready)
            {
                if (Canvas != null)
                {
                    Canvas.gameObject.SetActive(ready);
                    Debug.Log($"Invoked set canvas {ready}");
                    SetCanvasAction?.Invoke(this, ready);
                }
                foreach (GameObject obj in ObjectsToEnableOnReady)
                    obj.SetActive(ready);
            }
               
            while (ScreenTransitions.Instance.IsTransitioning)
                yield return null;

            // Waits until the canvas moves out of frame 
            if (!ready)
            {
                if (Canvas != null)
                {
                    Canvas.gameObject.SetActive(ready);
                    Debug.Log($"Invoked set canvas {ready}");
                    SetCanvasAction?.Invoke(this, ready);
                    Debug.Log($"Disabled canvas: {Canvas.name}");
                }
                foreach (GameObject obj in ObjectsToEnableOnReady)
                    obj.SetActive(ready);
            }
        }

       public void Init(MenuTypes menuType)
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
                    CanvasElements = child;
            }

            this.menuType = menuType;
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
                emptyMenu.MenusToDisableOnReady.Add(menu);
        }

        // Initializes each menu
        // Makes sure only the main menu is open on start
        foreach (var menu in menus)
        {
            if (!MenuToMenuType.TryGetValue(menu, out var menuType))
                menuType = MenuTypes.None;
            menu.Init(menuType);

            if (menu.Canvas != null && menu.menuType != MenuTypes.MainMenu)
                menu.Canvas.gameObject.SetActive(false);
        }

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
        Menu.SetCanvasAction += HandleSetCanvas;
    }

    void OnDisable()
    {
        GameStateChangeEventHandler -= HandleGameStateChange;
        UIButton.UIInteractEventHandler -= HandleUIButtonInteract;
        GameManager.GameActionEventHandler -= HandleGameAction;
        Menu.SetCanvasAction -= HandleSetCanvas;
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
    /// <param name="setActive"> The SetActive() property the canvas was set to. </param>
    void HandleSetCanvas(Menu menu, bool setActive)
    {
        bool isAMenuEnabled = false;

        string activeCanvases = string.Empty;

        // We could save performance by skipping the loop on (ready == true)
        foreach (var m in menus)
        {
            if (m.Canvas.gameObject.activeInHierarchy)
            {
                activeCanvases += m.Canvas.name;
                isAMenuEnabled = true;
            }
        }
        Debug.Log($"The following canvases are currently active: {(activeCanvases == string.Empty ? "no active canvases" : activeCanvases[..^2])}");

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
        {
            foreach (var current in menus)
                current.SetCanvas(false, needsToMoveOutOfFrame: true);
            nestedMenuHistory.Clear();
        }
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
        // We also need to ensure that the menu we just loaded is at the end of the list
        bool transitioningToMenuUnderStack = false;
        if (nestedMenuHistory.Contains(menu))
        {
            transitioningToMenuUnderStack = true;
            nestedMenuHistory.RemoveAt(nestedMenuHistory.Count() - 1);
        }
        else
            nestedMenuHistory.Add(menu);

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

        previousMenu?.SetCanvas(false, false, !transitioningToMenuUnderStack);
        menu.SetCanvas(true, false, transitioningToMenuUnderStack);


        if (menu.canSeePaper)
            userInterfaceCamera.cullingMask = allSeeingCullingMask;
        else
            userInterfaceCamera.cullingMask = uIOnlyCullingMask;
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