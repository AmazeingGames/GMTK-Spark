using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Level Data serves as a central point for other objects to access important data, which changes from scene to scene.
/// </summary>
public class LevelData : MonoBehaviour
{
    /// <summary> The parent object of all the puzzle pieces in the level. </summary>
    [field: SerializeField] public Transform PuzzleParent { get; private set; }

    /// <summary> Serves as a temporary parent for grabbed paper objects, so that rotation properly functions. </summary>
    [field: SerializeField] public Transform PaperParent { get; private set ; }
    public SpriteRenderer PaperParentSpriteRenderer { get; private set; }

    /// <summary> Leniency refers to how close a puzzle piece needs to be to its origin to snap into place. </summary>
    [field: SerializeField] public float PositionalLeniency { get; private set; } = .45f;
    [field: SerializeField] public float RotationalLeniency { get; private set; } = .1f;

    /// <summary> Notifies listeners when this object is being loaded in, as part of loading/unloading levels. /// </summary>
    public static event Action<LoadLevelDataEventArgs> LoadLevelData;

    private void Start() => 
        PaperParentSpriteRenderer = PaperParent.gameObject.GetComponent<SpriteRenderer>();

    /// <summary> Tells listeners to save the new level data as its being loaded in. </summary>
    private void OnEnable() =>
        LoadLevelData?.Invoke(new(this, isLoadingIn: true));

    /// <summary> Tells listeners to forget data as its being unloaded. </summary>
    private void OnDisable() =>
        LoadLevelData?.Invoke(new(this, isLoadingIn: false));

    public class LoadLevelDataEventArgs : EventArgs
    {
        /// <summary> 
        ///     True: This level data is part of the level being loaded in
        ///     False: Data is being unloaded.
        /// </summary>
        public readonly bool isLoadingIn;

        public readonly LevelData levelData;
        public LoadLevelDataEventArgs(LevelData levelData, bool isLoadingIn)
        {
            this.levelData = levelData;
            this.isLoadingIn = isLoadingIn;
        }
    }
}
