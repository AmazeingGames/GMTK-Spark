using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    KeyCode pauseKey = KeyCode.Escape;

    public enum GameState { None, InMenu, Running, Paused, Loading }
    public enum GameAction { None, EnterMainMenu, StartLevel, PauseGame, ResumeGame, RestartLevel, LoadNextLevel, CompleteLevel, BeatGame, }

    public GameState CurrentState { get; private set; }
    public GameAction LastGameAction { get; private set; }

    readonly List<Paper> paperList = new();

    public LevelData LevelData { get; private set; }

    public static event EventHandler<GameStateChangeEventArgs> GameStateChangeEventHandler;
    public static event EventHandler<GameActionEventArgs> GameActionEventHandler;

    public class GameActionEventArgs : EventArgs
    {
        public readonly GameManager gameManager;
        public readonly GameAction gameAction;
        public readonly int levelToLoad;

        public GameActionEventArgs(GameManager gameManager, GameAction gameAction, int levelToLoad)
        {
            this.gameManager = gameManager;
            this.gameAction = gameAction;
            this.levelToLoad = levelToLoad;
        }
    }

    public class GameStateChangeEventArgs : EventArgs
    {
        public readonly GameManager gameManager;
        public readonly GameState newState;
        public readonly GameState previousState;
        public readonly int levelToLoad;

        public GameStateChangeEventArgs(GameManager gameManager, GameState newState, GameState previousState, int levelToLoad)
        {
            this.gameManager = gameManager;
            this.newState = newState;
            this.previousState = previousState;
            this.levelToLoad = levelToLoad;
        }
    }

    void OnEnable()
    {
        MovePaper.PaperActionEventHandler += HandlePaperAction;
        ScenesManager.BeatLastLevelEventHandler += HandleBeatLastLevel;
        LevelData.LoadLevelDataEventHandler += HandleLoadLevelData;
        UIButton.UIInteractEventHandler += HandleUIInteract;
        CheatsManager.CheatEventHandler += HandleCheat;
    }

    void OnDisable()
    {
        MovePaper.PaperActionEventHandler -= HandlePaperAction;
        ScenesManager.BeatLastLevelEventHandler -= HandleBeatLastLevel;
        LevelData.LoadLevelDataEventHandler -= HandleLoadLevelData;
        UIButton.UIInteractEventHandler -= HandleUIInteract;
        CheatsManager.CheatEventHandler -= HandleCheat;
    }

    // Start is called before the first frame update
    void Start()
    {
        bool foundTestLevel = false;

#if DEBUG
        pauseKey = KeyCode.P;

        int levelToLoad = -1;
        string levelString;
        List<string> levelsToUnload = new();

        // Checks if there's any levels already loaded
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);

            if (scene.name[..5] != "Level")
                continue;

            levelString = scene.name[(scene.name.LastIndexOf('_') + 1)..];
            if (!int.TryParse(levelString, out int levelNumber))
                continue;

            levelsToUnload.Add(scene.name);
            levelToLoad = levelNumber;
        }

        // If there are, we load the first level and unload the rest
        if (ScenesManager.IsLevelInBuildPath(levelToLoad))
        {
            foundTestLevel = true;
            Debug.Log($"TestLevel: {levelToLoad}");

            for (int i = 0; i < levelsToUnload.Count; i++)
                SceneManager.UnloadSceneAsync(levelsToUnload[i]);

            PerformGameAction(GameAction.StartLevel, levelToLoad);
        }
#endif
        // Otherewise, runs the game as normal
        if (!foundTestLevel)
            PerformGameAction(GameAction.EnterMainMenu);
    }

    private void Update()
    {
        // In the future, I would like the game to acknowledge this, and be able to smoothly transition between the 2 quickly
        if (Input.GetKeyDown(pauseKey) && !ScreenTransitions.Instance.IsTransitioning)
        {
            switch (CurrentState)
            {
                case GameState.Running:
                    if (LastGameAction == GameAction.CompleteLevel)
                        return;
                    PerformGameAction(GameAction.PauseGame);
                break;

                case GameState.Paused:
                    PerformGameAction(GameAction.ResumeGame);
                break;
            }
        }
    }

    bool hasBeatLevel;

    /// <summary>
    ///     Checks if we beat the level when we snap a piece into place.
    /// </summary>
    void HandlePaperAction(object sender, MovePaper.PaperActionEventArgs e)
    {
        if (e.actionType != MovePaper.PaperActionEventArgs.PaperActionType.Snap)
            return;
        
        Invoke(nameof(CheckVictory), .1f);
    }

    /// <summary>
    ///     Checks if all papers are in the proper position.
    /// </summary>
    void CheckVictory()
    {
        foreach (var paper in paperList)
        {
            if (!paper.IsInPlace)
            {
                Debug.Log($"Paper {paper.name} is not in place");
                return;
            }
        }

        PerformGameAction(GameAction.CompleteLevel);
    }

    /// <summary>
    ///     Performs a game action given from a UI button.
    /// </summary>
    void HandleUIInteract(object sender, UIButton.UIInteractEventArgs e)
    {
        if (e.buttonEvent != UIButton.UIEventTypes.GameAction || e.buttonInteraction != UIButton.UIInteractionTypes.Click)
            return;

        PerformGameAction(e.actionToPerform, e.levelToLoad);
    }

    /// <summary>
    ///     Updates the game to end when we beat the last level.
    /// </summary>
    void HandleBeatLastLevel(object sender, EventArgs e)
        => PerformGameAction(GameAction.BeatGame);

    /// <summary>
    ///     Performs a game action given from the cheat menu.
    /// </summary>
    void HandleCheat(object sender, CheatsManager.CheatEventArgs e)
    {
        if (e.gameAction != GameAction.None)
            PerformGameAction(e.gameAction);
    }

    /// <summary>
    ///     Saves level data when we finish loading a new level
    /// </summary>
    void HandleLoadLevelData(object sender, LevelData.LoadLevelDataEventArgs e)
    {
        if (LevelData == e.levelData && !e.isLoadingIn)
        {
            Debug.Log("set level data null");
            LevelData = null;
            return;
        }
        
        if (LevelData != e.levelData && e.isLoadingIn)
        {
            Debug.Log("Set new level data");
            LevelData = e.levelData;
            paperList.Clear();

            for (int i = 0; i < LevelData.PuzzleParent.transform.childCount; i++)
                paperList.Add(LevelData.PuzzleParent.transform.GetChild(i).GetComponent<Paper>());
        }
        else
            Debug.Log("did nothing to level data");
    }

    /// <summary>
    ///     Informs listerners of a game action and updates the game state accordingly.
    /// </summary>
    /// <param name="action"> The game action to perform. </param>
    /// <param name="levelToLoad"> If we should load a level, otherwise leave at -1. </param>
    void PerformGameAction(GameAction action, int levelToLoad = -1)
    {
        if (action == GameAction.None)
        {
            Debug.LogWarning("Cannont run comand 'none'.");
            return;
        }

        LastGameAction = action;
        OnGameAction(action, levelToLoad);

        // Updates the game state to fit the action
        switch (action)
        {
            case GameAction.EnterMainMenu:
                UpdateGameState(GameState.InMenu);
            break;

            case GameAction.StartLevel:
            case GameAction.ResumeGame:
            case GameAction.RestartLevel:
            case GameAction.LoadNextLevel:
                UpdateGameState(GameState.Running);
            break;

            case GameAction.PauseGame:
                UpdateGameState(GameState.Paused);
            break;
        }
    }

    void OnGameAction(GameAction action, int levelToLoad)
        => GameActionEventHandler?.Invoke(this, new(this, action, levelToLoad));

    /// <summary>
    ///     Informs listeners on how to align with the current state of the game.
    /// </summary>
    /// <param name="newState"> The state of the game to update to. </param>
    void UpdateGameState(GameState newState, int levelToLoad = -1)
    {
        if (newState == GameState.None)
        {
            Debug.LogWarning("Cannont update game state to 'none'.");
            return;
        }
        else if (newState == CurrentState)
        {
            Debug.LogWarning($"Cannont update game state to its own state ({newState}).");
            return;
        }

        var previousState = CurrentState;
        CurrentState = newState;

        GameStateChangeEventHandler?.Invoke(this, new(this, newState, previousState, levelToLoad));
    }
}