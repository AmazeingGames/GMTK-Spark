using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelData : MonoBehaviour
{
    [field: SerializeField] public Transform PuzzleParent { get; private set; }
    [field: SerializeField] public Transform DragParent { get; private set ; }
    [field: SerializeField] public SpriteRenderer DragParentSpriteRenderer { get; private set ; }

    public static EventHandler<LevelLoadEventArgs> LevelLoadEventHandler;

    public class LevelLoadEventArgs
    {
        public enum LoadType { Loaded, Unloaded }
        public readonly LoadType loadState;
        public readonly LevelData levelData;

        public LevelLoadEventArgs(LevelData levelData, LoadType loadState)
        {
            this.levelData = levelData;
            this.loadState = loadState;
        }
    }

    private void OnEnable()
        => OnLevelLoad(LevelLoadEventArgs.LoadType.Loaded);

    private void OnDisable()
        => OnLevelLoad(LevelLoadEventArgs.LoadType.Unloaded);

    void OnLevelLoad(LevelLoadEventArgs.LoadType loadState)
        => LevelLoadEventHandler?.Invoke(this, new (this, loadState));

}
