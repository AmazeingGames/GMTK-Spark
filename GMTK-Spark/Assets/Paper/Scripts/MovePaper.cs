using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static MovePaper.PaperActionEventArgs;

public class MovePaper : MonoBehaviour
{
    [Header("Rotation Properties")]
    [SerializeField] float rotationSpeed;
    [SerializeField] Space space;

    [Header("Snap Properties")]
    [SerializeField] bool worldPositionStays;
    [SerializeField] AnimationCurve lerpCurve;
    [SerializeField] float lerpSpeed;
    [SerializeField] bool fixedSpeed;

    [Header("Paper Propertiers")]
    [SerializeField] LayerMask paperLayer;

    [Header("Cheats")]
    [SerializeField] bool autoSnap;

    Vector3 mousePosition;
    Paper mouseOverPaper;

    RaycastHit2D[] hits;
    readonly List<Paper> listOfPapersMouseOver = new();

    public static event EventHandler<PaperActionEventArgs> PaperActionEventHandler;
    public static event EventHandler<GetMatchingPaperEventArgs> GetMatchingPaperEventHandler;

    Transform levelHolder;
    int order = 0;

    LevelData levelData;

    bool isXClose;
    bool isYClose;
    bool isZRotationBetweenZeroAndPositiveLeniency;
    bool isZRotationBetweenZeroAndNegativeLeniency;
    bool isWRotationBetweenOneAndPositiveLeniency;
    bool isWRotationBetweenOneAndNegativeLeniency;

    bool isFlippedWRotationBetweenNegativeOneAndNegativeLeniency; // Close to 1
    bool isFlippedWRotationBetweenNegativeOneAndPositiveLeniency; // Close to 1

    GetMatchingPaperEventArgs OnGetPaper(GameObject paperGameObject)
    {
        var paperEventArgs = new GetMatchingPaperEventArgs(paperGameObject);
        GetMatchingPaperEventHandler?.Invoke(this, paperEventArgs);
        return paperEventArgs;
    }
        void OnPaperAction(Paper paper, PaperActionType paperAction)
            => PaperActionEventHandler?.Invoke(this, new(paper, paperAction));

    public class GetMatchingPaperEventArgs : EventArgs
    {
        public readonly GameObject paperGameObject;
        public Paper MatchingPaper { get; private set; }

        public GetMatchingPaperEventArgs(GameObject paperGameObject)
            => this.paperGameObject = paperGameObject;

        public void WriteResults(Paper paper)
            => MatchingPaper = paper;
    }

    public class PaperActionEventArgs : EventArgs
    {
        public enum PaperActionType { Grab, Drop, StartSnap, Snap }

        public readonly PaperActionType actionType;
        public readonly Paper paper;

        public PaperActionEventArgs(Paper paper, PaperActionType actionType)
        {
            this.paper = paper;
            this.actionType = actionType;
        }
    }

    PaperVariables paperValues;

    private void Awake()
        => paperValues = new PaperVariables();

    void OnEnable()
    {
        LevelData.LoadLevelData += HandleLoadLevelData;
        CheatsManager.CheatEventHandler += HandleCheat;
        GameManager.GameActionEventHandler += HandleGameAction;
        paperValues.OnEnable();
    }

    void OnDisable()
    {
        LevelData.LoadLevelData -= HandleLoadLevelData;
        CheatsManager.CheatEventHandler -= HandleCheat;
        GameManager.GameActionEventHandler -= HandleGameAction;
        paperValues.OnDisable();
    }

    void HandleCheat(object sender, CheatsManager.CheatEventArgs e)
    {
        switch (e.cheatCommand)
        {
            case CheatsManager.CheatCommands.AutoSnap:
                autoSnap = true;
            break;
        }
    }

    // WHY DOES MOVE PAPER HAVE ITS OWN REFERENCE TO LEVEL DATA IF IT JUST USES GAMEMANAGER'S?!
    void HandleLoadLevelData(object sender, LevelData.LoadLevelDataEventArgs e)
    {
        if (levelData == e.levelData && !e.isLoadingIn)
            levelData = null;
        else if (levelData != e.levelData && e.isLoadingIn)
            levelData = e.levelData;
    }

    void HandleGameAction(object sender, GameManager.GameActionEventArgs e)
    {
        switch (e.gameAction)
        {
            case GameManager.GameAction.StartLevel:
            case GameManager.GameAction.RestartLevel:
            case GameManager.GameAction.LoadNextLevel:
                autoSnap = false;
            break;

            case GameManager.GameAction.CompleteLevel:
                autoSnap = true;
            break;
        }
    }

    /// <summary> 
    ///     Handles mouse clicks and mouse releases with paper scraps.
    ///     Grabs paper on mouse down; drops paper on mouse up. 
    /// </summary>
    void PaperInteraction(InteractionType interactionType, Paper paperInteraction)
    {
        Transform dragParent = GameManager.Instance.LevelData.MousePosition;
        switch (interactionType)
        {
            // Grab Paper
            case InteractionType.Click:
                Debug.Log("handle click on paper");
                if (paperValues.HoldingPaper != null)
                    return;

                if (!paperInteraction.CanBeGrabbed)
                    return;

                for (int i = 0; i < GameManager.Instance.LevelData.MousePosition.childCount; i++)
                {
                    Debug.LogWarning("Set parent to remember parent. This should normally not happen. ");
                    GameManager.Instance.LevelData.MousePosition.GetChild(i).SetParent(levelHolder);
                }

                // Sets the paper's parent to the mouse and informs listeners of any state changes.
                levelHolder = paperInteraction.transform.parent;
                paperInteraction.transform.SetParent(dragParent, worldPositionStays);
                paperInteraction.SpriteRenderer.sortingOrder = order++;

                // The way this is set up with events is the stupidest thing I've ever seen
                OnPaperAction(paperInteraction, PaperActionType.Grab);
            break;

            // Drop Paper
            case InteractionType.Release:
                if (paperValues.HoldingPaper != paperInteraction || paperValues.HoldingPaper == null)
                    return;

                // Resets the paper's parent and informs listeners of any state changes.
                paperInteraction.transform.SetParent(levelHolder, worldPositionStays);
                PaperActionType paperActionType = CheckPosition(paperInteraction) ? PaperActionType.StartSnap : PaperActionType.Drop;
                OnPaperAction(paperInteraction, paperActionType);
            break;
        }
    }

    enum InteractionType { Click, Release }
    void Update()
    {
        // WHY DOES MOVE PAPER HAVE ITS OWN REFERENCE TO LEVEL DATA IF IT JUST USE GAMEMANAGER'S ANYWAYS?!
        if (GameManager.Instance.LevelData == null)
            return;

        // Checks what papers the mouse is over every frame
        mousePosition = InteractionMath.GetMousePosition();
        hits = Physics2D.RaycastAll(mousePosition, transform.TransformDirection(Vector3.forward), Mathf.Infinity, paperLayer);

        listOfPapersMouseOver.Clear();
        mouseOverPaper = null;

        if (hits.Length > 0)
        {
            Debug.DrawRay(mousePosition, transform.TransformDirection(Vector3.forward) * 1000, Color.yellow);
            
            // Gets the 'Paper' component from each gameobject hit with the raycast
            foreach (var hit in hits)
            {
                var paperGameObject = hit.collider.gameObject;
                Paper hitPaper = OnGetPaper(paperGameObject).MatchingPaper;
                listOfPapersMouseOver.Add(hitPaper);

                if (hitPaper == null)
                    throw new NullReferenceException("Hit paper is null. This means no paper handled the GetPaper event hander, even though we hit a gameobject with the paper layermask.");
            }

            // Grabs the paper with the highest sorting layer
            mouseOverPaper = listOfPapersMouseOver
                .OrderBy(p => p.SpriteRenderer.sortingOrder)
                .LastOrDefault();
        }
        else
            Debug.DrawRay(mousePosition, transform.TransformDirection(Vector3.forward) * 1000, Color.white);
        
        if (mouseOverPaper != null && Input.GetMouseButtonDown(0))
            PaperInteraction(InteractionType.Click, mouseOverPaper.GetComponent<Paper>());

        else if (Input.GetMouseButtonUp(0))
            PaperInteraction(InteractionType.Release, paperValues.HoldingPaper);

        // Moves & Rotates Parent
        GameManager.Instance.LevelData.MousePosition.transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Clamper.CalculateBounds(GameManager.Instance.LevelData.PaperParentSpriteRenderer, out float width, out float height, out Vector2 screenBounds);
        Clamper.ClampToScreenOrthographic(GameManager.Instance.LevelData.MousePosition, width, height, screenBounds);
        if (Input.mouseScrollDelta.y != 0)
            GameManager.Instance.LevelData.MousePosition.transform.Rotate(Input.mouseScrollDelta.y * rotationSpeed * Time.deltaTime * Vector3.forward, space);
    }

    /// <summary> Checks to snap a paper into place, on drop only. </summary>
    /// <returns> True if we start snapping the paper </returns>
    bool CheckPosition(Paper droppedPaper)
    {
        // Checks Position
        var position = droppedPaper.transform.localPosition;
        
        isXClose = (position.x >= 0 && position.x <= levelData.PositionalLeniency) || (position.x <= 0 && position.x > -levelData.PositionalLeniency); // Close to 0
        isYClose = (position.y > 0 && position.y <= levelData.PositionalLeniency) || (position.y <= 0 && position.y > -levelData.PositionalLeniency); // Close to 0

        // Check Rotation
        Quaternion rotation = droppedPaper.transform.rotation;

        isZRotationBetweenZeroAndPositiveLeniency = rotation.z > 0 && rotation.z <= levelData.RotationalLeniency;     // Close to 0
        isZRotationBetweenZeroAndNegativeLeniency = rotation.z <= 0 && rotation.z > -levelData.RotationalLeniency;    // Close to 0

        bool isCloseZ = isZRotationBetweenZeroAndPositiveLeniency || isZRotationBetweenZeroAndNegativeLeniency;

        isWRotationBetweenOneAndPositiveLeniency = rotation.w > 1 && rotation.w <= (1 + levelData.RotationalLeniency);    // Close to 1 (>1 && <1.1) leniency of .1
        isWRotationBetweenOneAndNegativeLeniency = rotation.w <= 1 && rotation.w > -(1 - levelData.RotationalLeniency);   // Close to 1 (<1 && >-.9) leniency of .1

        isFlippedWRotationBetweenNegativeOneAndNegativeLeniency = rotation.w > -1 && rotation.w <= -(1 - levelData.RotationalLeniency);  // Close to -1 (>-1 && <-.9) leniency of .1
        isFlippedWRotationBetweenNegativeOneAndPositiveLeniency = rotation.w <= -1 && rotation.w > -(1 - levelData.RotationalLeniency);  // Close to -1 (<-1 && >-.9) leniency of .1

        bool isCloseW = (isWRotationBetweenOneAndNegativeLeniency || isWRotationBetweenOneAndPositiveLeniency) || (isFlippedWRotationBetweenNegativeOneAndNegativeLeniency || isFlippedWRotationBetweenNegativeOneAndPositiveLeniency);

        bool startSnap = isXClose && isYClose && isCloseZ && isCloseW;

        if (autoSnap)
            startSnap = true;

        if (startSnap)
        {
            OnPaperAction(droppedPaper, PaperActionType.StartSnap);
            Debug.Log($"started paper snap on {droppedPaper.name}");
            StartCoroutine(LerpSnap(droppedPaper));
            return true;
        }
        return false;
    }

    /// <summary>
    ///     Moves the last held paper to the correct position over time.
    /// </summary>
    IEnumerator LerpSnap(Paper lerpPaper)
    {
        float time = 0;
        lerpPaper.transform.GetPositionAndRotation(out Vector3 startingPosition, out Quaternion startingRotation);
        
        while (time < 1)
        {
            if (paperValues.HoldingPaper == lerpPaper)
            {
                Debug.LogWarning("Holding the paper we're trying to lerp");
                yield break;
            }

            if (lerpPaper.transform.parent == GameManager.Instance.LevelData.MousePosition)
                lerpPaper.transform.SetParent(levelHolder);

            var newPosition = Vector3.Lerp(startingPosition, Vector3.zero, lerpCurve.Evaluate(time));
            var newRotation = Quaternion.Slerp(startingRotation, Quaternion.Euler(0, 0, 0), lerpCurve.Evaluate(time));

            lerpPaper.transform.SetPositionAndRotation(newPosition, newRotation);
            time += Time.deltaTime * lerpSpeed;
            
            yield return null;
        }

        lerpPaper.SpriteRenderer.sortingOrder = 0;
        lerpPaper.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 0, 0));
        OnPaperAction(lerpPaper, PaperActionType.Snap);
    }

    // This 100% needs to be changed
    [Serializable]
    class PaperVariables
    {
        public Paper HoldingPaper { get; private set; }
        public Paper DroppedPaper {get; private set; }

        public void OnEnable()
            => PaperActionEventHandler += HandlePaperAction;

        public void OnDisable()
            => PaperActionEventHandler -= HandlePaperAction;

        // If we can only interact with one paper at a time, then we should really only have a single paper value
        void HandlePaperAction(object sender, PaperActionEventArgs e)
        {
            DroppedPaper = null;
            switch (e.actionType)
            {
                case PaperActionType.Grab:
                    Debug.Log($"Set holding paper to {e.paper}");
                    HoldingPaper = e.paper;
                break;

                case PaperActionType.Drop:
                    Debug.Log($"Set dropped paper to {e.paper}");
                    DroppedPaper = e.paper;
                    HoldingPaper = null;
                break;

                case PaperActionType.StartSnap:
                    HoldingPaper = null;
                    break;
            }
        }
    }


}
