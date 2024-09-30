using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///     Responsible for loading all scenes and contains all the functionality for scene loading.
/// </summary>
public class ScenesManager : Singleton<ScenesManager>
{
    [SerializeField] string levelConvention = "Level_";
    public class SceneLoadEventArgs : EventArgs
    {
        public enum Progress { Start, Loading, Finish }

        public readonly AsyncOperation asyncOperation;

        public readonly Progress progress;

        public readonly bool isLevel;
        public readonly int levelNumber;

        public SceneLoadEventArgs(AsyncOperation asyncOperation, Progress progress)
        {
            this.asyncOperation = asyncOperation;
            this.progress = progress;
        }
    }

    public int CurrentLevel { get; private set; }

    string lastLoadedLevel = null;

    public static event EventHandler<SceneLoadEventArgs> SceneLoadEventHandler;
    public static event EventHandler BeatLastLevelEventHandler;

    readonly List<(AsyncOperation, int)> preloadedLevels = new();

    private void OnEnable()
    {
        GameManager.GameActionEventHandler += HandleGameAction;
        GameManager.GameStateChangeEventHandler += HandleGameStateChange;
    }
    private void OnDisable()
    {
        GameManager.GameActionEventHandler -= HandleGameAction;
        GameManager.GameStateChangeEventHandler -= HandleGameStateChange;
    }

    private void Start()
    {
        string menu = "Menus";
        if (!IsSceneLoaded(menu))
            LoadScene(menu);
    }

    void HandleGameAction(object sender, GameManager.GameActionEventArgs e)
    {
        switch (e.gameAction)
        {
            case GameManager.GameAction.EnterMainMenu:
                UnloadLevel(CurrentLevel);
            break;

            case GameManager.GameAction.StartLevel:
                if (e.levelToLoad == -1)
                    throw new ArgumentException("Level to load should not be -1. ");
                LoadLevel(e.levelToLoad);
            break;

            case GameManager.GameAction.RestartLevel:
                LoadLevel(CurrentLevel);
            break;

            case GameManager.GameAction.LoadNextLevel:
                if (!LoadLevel(CurrentLevel + 1))
                    Invoke(nameof(OnBeatLastLevel), .01f);
            break;

            case GameManager.GameAction.CompleteLevel:
                PreloadLevel(CurrentLevel + 1);
            break;
        }
    }

    /// <summary>
    ///     Handles scene and level loading for various game updates.
    /// </summary>
    /// <exception cref="ArgumentException"> Exception on invalid level number when loading a level. </exception>
    void HandleGameStateChange(object sender, GameManager.GameStateChangeEventArgs e)
    {
        
    }

    /// <summary>
    ///     Called when we know we're going to need a scene/level, and loads it disabled in advance.
    ///     The next time we load a level, we'll first check to see if it's preloaded before attempting to load it.
    /// </summary>
    /// <param name="level"></param>
    void PreloadLevel(int level)
    {
        if (!IsLevelInBuildPath(level))
        {
            Debug.Log("No level exists to ready");
            return;
        }

        var scene = SceneManager.LoadSceneAsync($"{levelConvention}{level}", LoadSceneMode.Additive);
        scene.allowSceneActivation = false;
        preloadedLevels.Add((scene, level));
    }

    /// <summary>
    ///     Asyrnchously readies or loads a level and unloads the previous level.
    /// </summary>
    /// <param name="level"> The number of the level to unload. </param>
    /// <returns> True if level is found. </returns>
    bool LoadLevel(int level)
    {
        UnloadScene(lastLoadedLevel);

        CurrentLevel = level;
        lastLoadedLevel = $"{levelConvention}{CurrentLevel}";

        // Loading Screen

        // If the level is preloaded, only readies it
        var levelToReady = preloadedLevels.Find(i => i.Item2 == level);
        if (levelToReady.Item1 != null)
        {
            Debug.Log("Found and allowed scene actication");
            levelToReady.Item1.allowSceneActivation = true;
            preloadedLevels.Remove(levelToReady);
            return true;
        }
        
        // Otherwise loads it
        Debug.Log("Couldn't find readied scene");
        return LoadScene($"{levelConvention}{CurrentLevel}");
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
        
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        return true;
    }

    /*
    /// <summary>
    ///     Called when we begin asynchronously loading a level.
    /// </summary>
    /// <param name="levelLoadAsyncOperation"> The level loading operation. </param>
    void OnStartSceneLoad(AsyncOperation levelLoadAsyncOperation)
    {
        SceneLoadEventArgs eventArgs = new(levelLoadAsyncOperation, SceneLoadEventArgs.Progress.Start);
        SceneLoadEventHandler?.Invoke(this, eventArgs);
        
        StartCoroutine(SceneLoading(levelLoadAsyncOperation));

        Debug.Log("Start Level load!");
    }

    
    void OnFinishSceneLoad(AsyncOperation levelLoadAsyncOperation)
    {
        SceneLoadEventArgs eventArgs = new(levelLoadAsyncOperation, SceneLoadEventArgs.Progress.Finish);
        SceneLoadEventHandler?.Invoke(this, eventArgs);

        Debug.Log("Finish Level load!");
    }

    IEnumerator SceneLoading(AsyncOperation levelLoadOperation)
    {
        while (!levelLoadOperation.isDone)
            yield return null;

        OnFinishSceneLoad(levelLoadOperation);
    }

    void OnCompleteLevelLoad(AsyncOperation levelLoadOperation)
    {

    }*/

    void OnBeatLastLevel()
        => BeatLastLevelEventHandler?.Invoke(this, new());

    /// <summary>
    ///     Checks if a level is in the build path and can be loaded in.
    /// </summary>
    /// <param name="levelnumber"> The nummber of the level to check. </param>
    /// <returns> True if a scene is successfully found. </returns>
    public static bool IsLevelInBuildPath(int levelnumber)
    {
        if (Instance == null)
            return false;

        return SceneUtility.GetBuildIndexByScenePath($"{Instance.levelConvention}{levelnumber}") != -1;
    }

    static bool IsLevelLoaded(int levelnumber)
        => IsSceneLoaded($"{Instance.levelConvention}{levelnumber}");

    /// <summary>
    ///     Checks if there is a matching scene name currently loaded in.
    /// </summary>
    /// <returns> True if the scene is currently loaded in the project. </returns>
    static bool IsSceneLoaded(string sceneName)
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
    bool UnloadLevel(int levelnumber)
        => UnloadScene($"{levelConvention}{levelnumber}");

    /// <summary>
    ///     Asynchronously unloads a scene.
    /// </summary>
    /// <param name="sceneName"> The name of the scene to unload. </param>
    /// <returns> True if the scene starts to unload. </returns>
    bool UnloadScene(string sceneName)
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
