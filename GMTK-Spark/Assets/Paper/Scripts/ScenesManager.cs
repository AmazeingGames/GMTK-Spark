using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenesManager : Singleton<ScenesManager>
{
    [SerializeField] string levelConvention = "Level_";
    public class LevelLoadStartEventArgs : EventArgs
    {
        public readonly AsyncOperation asyncOperation;

        public LevelLoadStartEventArgs(AsyncOperation _asyncOperation)
            => asyncOperation = _asyncOperation;
    }

    public int CurrentLevel { get; private set; }

    string lastLoadedLevel = null;

    public static event EventHandler<LevelLoadStartEventArgs> StartLevelLoadEventHandler;
    public static event EventHandler BeatLastLevelEventHandler;

    private void OnEnable()
        => GameManager.GameStateChangeEventHandler += HandleGameStateChange;

    private void OnDisable()
        => GameManager.GameStateChangeEventHandler -= HandleGameStateChange;

    /// <summary>
    ///     Handles scene and level loading for various game updates.
    /// </summary>
    /// <exception cref="ArgumentException"> Exception on invalid level number when loading a level. </exception>
    public void HandleGameStateChange(object sender, GameManager.GameStateChangeEventArgs e)
    {
        switch (e.newState)
        {
            case GameManager.GameState.EnterMainMenu:
                string menu = "Menus";
                if (!IsSceneLoaded(menu))
                    LoadScene(menu);
                
                UnloadLevel(CurrentLevel);
            break;

            case GameManager.GameState.StartLevel:

                if (e.levelToLoad == -1)
                    throw new ArgumentException("Level to load should not be -1. ");
                LoadLevel(e.levelToLoad);
            break;

            case GameManager.GameState.LoseLevel:
            case GameManager.GameState.RestartLevel:
                LoadLevel(CurrentLevel);
            break;

            case GameManager.GameState.BeatLevel:
                if (!LoadLevel(CurrentLevel + 1))
                    OnBeatLastLevel();
            break;
        }
    }

    /// <summary>
    ///     Asyrnchously loads a level and unloads the previous level.
    /// </summary>
    /// <param name="level"> The number of the level to unload. </param>
    /// <returns> True if level is found. </returns>
    public bool LoadLevel(int level)
    {
        UnloadScene(lastLoadedLevel);

        lastLoadedLevel = $"{levelConvention}{level}";
        CurrentLevel = level;

        return LoadScene($"{levelConvention}{level}");
    }

    /// <summary>
    ///     Asynchronously loads a scene.
    /// </summary>
    /// <param name="sceneName"> The name of the scene to load. </param>
    /// <returns> True if the scene starts loading. </returns>
    bool LoadScene(string sceneName)
    {
        if (SceneUtility.GetBuildIndexByScenePath(sceneName) == -1)
            return false;

        AsyncOperation levelLoadAsyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        OnStartLevelLoad(levelLoadAsyncOperation);
        return true;
    }

    /// <summary>
    ///     Called when we begin asynchronously loading a level.
    /// </summary>
    /// <param name="levelLoadAsyncOperation"> The level loading operation. </param>
    public void OnStartLevelLoad(AsyncOperation levelLoadAsyncOperation)
    {
        LevelLoadStartEventArgs eventArgs = new(levelLoadAsyncOperation);
        StartLevelLoadEventHandler?.Invoke(this, eventArgs);
    }

    public void OnBeatLastLevel()
        => BeatLastLevelEventHandler?.Invoke(this, new());

    /// <summary>
    ///     Checks if a level is in the build path.
    /// </summary>
    /// <param name="levelnumber"> The nummber of the level to check. </param>
    /// <returns> True if a scene is successfully found. </returns>
    public static bool IsLevelInBuildPath(int levelnumber)
    {
        if (Instance == null)
            return false;

        return SceneUtility.GetBuildIndexByScenePath($"{Instance.levelConvention}{levelnumber}") != -1;
    }

    public static bool IsLevelLoaded(int levelnumber)
        => IsSceneLoaded($"{Instance.levelConvention}{levelnumber}");

    public static bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            
            if (scene.name != sceneName)
                continue;
            return true;
        }
        return false;
    }

    /// <summary>
    ///     Asynchronously unloads a level.
    /// </summary>
    /// <param name="levelnumber"> The number of the level to unload </param>
    /// <returns> True if the level starts to unload. </returns>
    public bool UnloadLevel(int levelnumber)
        => UnloadScene($"{levelConvention}{levelnumber}");

    /// <summary>
    ///     Asynchronously unloads a scene.
    /// </summary>
    /// <param name="sceneName"> The name of the scene to unload. </param>
    /// <returns> True if the scene starts to unload. </returns>
    public bool UnloadScene(string sceneName)
    {
        if (SceneUtility.GetBuildIndexByScenePath(sceneName) == -1)
            return false;
        if (lastLoadedLevel == null)
            return false;
        if (!IsSceneLoaded(sceneName))
            return false;

        SceneManager.UnloadSceneAsync(sceneName);
        return true;
    }
}
