using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelData : MonoBehaviour
{
    [field: SerializeField] public Transform PuzzleParent { get; private set; }
    [field: SerializeField] public Transform DragParent { get; private set ; }
    [field: SerializeField] public SpriteRenderer DragParentSpriteRenderer { get; private set ; }
    [field: SerializeField] public float PositionalLeniency { get; private set; } = .45f;
    [field: SerializeField] public float RotationalLeniency { get; private set; } = .1f;

    public static EventHandler<LoadLevelDataEventArgs> LoadLevelDataEventHandler;

    public class LoadLevelDataEventArgs
    {
        public readonly bool isLoadingIn;
        public readonly LevelData levelData;
        public LoadLevelDataEventArgs(LevelData levelData, bool isLoadingIn)
        {
            this.levelData = levelData;
            this.isLoadingIn = isLoadingIn;
        }
    }

    private void OnEnable()
        => OnLoadLevelData(true);

    private void OnDisable()
        => OnLoadLevelData(false);

    void OnLoadLevelData(bool loadState)
        => LoadLevelDataEventHandler?.Invoke(this, new (this, loadState));

}
