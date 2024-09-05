using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    KeyCode pauseKey = KeyCode.Escape;

    public enum GameState { None, EnterMainMenu, StartLevel, LoseLevel, RestartLevel, BeatLevel, BeatGame, PauseGame, ResumeGame }

    public GameState CurrentState { get; private set; }

    readonly List<Paper> paperList = new();
    
    public static event EventHandler<GameStateChangeEventArgs> GameStateChangeEventHandler;

    public LevelData LevelData { get; private set; }

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
        MovePaper.PaperAction += HandlePaperAction;
        ScenesManager.BeatLastLevelEventHandler += HandleBeatLastLevel;
        LevelData.LoadLevelDataEventHandler += HandleLoadLevelData;
        UIButton.UIInteractEventHandler += HandleUIInteract;
    }

    void OnDisable()
    {
        MovePaper.PaperAction -= HandlePaperAction;
        ScenesManager.BeatLastLevelEventHandler -= HandleBeatLastLevel;
        LevelData.LoadLevelDataEventHandler -= HandleLoadLevelData;
        UIButton.UIInteractEventHandler -= HandleUIInteract;
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

        // Loads the first found level and unloads the rest
        if (ScenesManager.IsLevelInBuildPath(levelToLoad))
        {
            foundTestLevel = true;
            Debug.Log($"TestLevel: {levelToLoad}");

            for (int i = 0; i < levelsToUnload.Count; i++)
                SceneManager.UnloadSceneAsync(levelsToUnload[i]);

            UpdateGameState(GameState.StartLevel, levelToLoad);
        }
#endif
        // Otherewise, runs the game as normal
        if (!foundTestLevel)
            UpdateGameState(GameState.EnterMainMenu);
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            switch (CurrentState)
            {
                case GameState.StartLevel:
                case GameState.RestartLevel:
                case GameState.ResumeGame:
                    UpdateGameState(GameState.PauseGame);
                break;

                case GameState.PauseGame:
                    UpdateGameState(GameState.ResumeGame);
                break;
            }
        }
    }

    /// <summary>
    ///     Reacts to paper being moved, dragged, picked up, and dropped into place
    ///     Delay to make sure other objects can update before responding
    /// </summary>
    void HandlePaperAction(object sender, MovePaper.PaperActionEventArgs e)
    {
        if (e.actionType != MovePaper.PaperActionEventArgs.PaperActionType.Snap)
            return;
        
        StartCoroutine(CheckVictory());
    }

    IEnumerator CheckVictory()
    {
        yield return new WaitForSeconds(.1f);

        foreach (var paper in paperList)
        {
            if (!paper.IsInPlace)
                yield break;
        }
        UpdateGameState(GameState.BeatLevel);
    }

    void HandleUIInteract(object sender, UIButton.UIInteractEventArgs e)
    {
        if (e.buttonEvent != UIButton.UIEventTypes.GameState)
            return;

        if (e.buttonInteraction != UIButton.UIInteractionTypes.Click)
            return;

        UpdateGameState(e.newGameState, e.levelToLoad);
    }

    void HandleReachGoal(object sender, EventArgs e)
        => UpdateGameState(GameState.BeatLevel);

    void HandleLoseLevel(object sender, EventArgs e)
        => UpdateGameState(GameState.RestartLevel);

    void HandleBeatLastLevel(object sender, EventArgs e)
        => UpdateGameState(GameState.EnterMainMenu);

    void HandleLoadLevelData(object sender, LevelData.LoadLevelDataEventArgs e)
    {
        if (!e.isLoadingIn)
        {
            LevelData = null;
            return;
        }

        LevelData = e.levelData;

        for (int i = 0; i < LevelData.PuzzleParent.transform.childCount; i++)
            paperList.Add(LevelData.PuzzleParent.transform.GetChild(i).GetComponent<Paper>());
    }


    /// <summary>
    ///     Informs listeners on how to align with the current state of the game.
    /// </summary>
    /// <param name="newState"> The state of the game to update to. </param>
    public void UpdateGameState(GameState newState, int levelToLoad = -1)
    {
        var previousState = CurrentState;
        CurrentState = newState;

        GameStateChangeEventHandler?.Invoke(this, new(this, newState, previousState, levelToLoad));
    }
}