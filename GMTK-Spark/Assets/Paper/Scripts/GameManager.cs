using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public enum GameState { RunGame, StartLevel, LoseLevel, RestartLevel, BeatLevel, BeatGame }

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
        LevelData.LevelLoadEventHandler += HandleLevelLoad;
    }

    void OnDisable()
    {
        MovePaper.PaperAction -= HandlePaperAction;
        ScenesManager.BeatLastLevelEventHandler -= HandleBeatLastLevel;
        LevelData.LevelLoadEventHandler -= HandleLevelLoad;
    }

    // Start is called before the first frame update
    void Start()
    {
        bool foundTestLevel = false;

#if DEBUG
        // Starts the game by unloading and reloading the level already in the scene
        int levelToLoad = -1;
        string levelString;
        string levelToUnload = string.Empty;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);

            if (scene.name[..5] != "Level")
                continue;

            levelString = scene.name[(scene.name.LastIndexOf('_') + 1)..];

            if (int.TryParse(levelString, out int levelNumber))
            {
                levelToUnload = scene.name;
                levelToLoad = levelNumber;
            }
        }

        if (ScenesManager.DoesLevelExist(levelToLoad))
        {
            foundTestLevel = true;
            Debug.Log($"TestLevel: {levelToLoad}");

            if (!string.IsNullOrEmpty(levelToUnload))
                SceneManager.UnloadSceneAsync(levelToUnload);

            UpdateGameState(GameState.StartLevel, levelToLoad);
        }
#endif
        if (!foundTestLevel)
            UpdateGameState(GameState.RunGame);
    }

    void HandlePaperAction(object sender, MovePaper.PaperActionEventArgs e)
    {
        if (e.actionType != MovePaper.PaperActionEventArgs.PaperActionType.Snap)
            return;

        foreach (var paper in paperList)
        {
            if (!paper.IsInPlace)
                return;
        }

        UpdateGameState(GameState.BeatLevel);
    }

    void HandleReachGoal(object sender, EventArgs e)
        => UpdateGameState(GameState.BeatLevel);

    void HandleLoseLevel(object sender, EventArgs e)
        => UpdateGameState(GameState.RestartLevel);

    void HandleBeatLastLevel(object sender, EventArgs e)
        => UpdateGameState(GameState.RunGame);

    void HandleLevelLoad(object sender, LevelData.LevelLoadEventArgs e)
    {
        switch (e.loadState)
        {
            case LevelData.LevelLoadEventArgs.LoadType.Loaded:
                LevelData = e.levelData;

                for (int i = 0; i < LevelData.PuzzleParent.transform.childCount; i++)
                    paperList.Add(LevelData.PuzzleParent.transform.GetChild(i).GetComponent<Paper>());
            break;

            case LevelData.LevelLoadEventArgs.LoadType.Unloaded:
                LevelData = null;
            break;
        }
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