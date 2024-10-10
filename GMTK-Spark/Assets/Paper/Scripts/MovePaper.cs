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
    List<Paper> listOfPapersMouseOver = new();

    public static event EventHandler<PaperActionEventArgs> PaperActionEventHandler;
    public static event EventHandler<GetMatchingPaperEventArgs> GetMatchingPaperEventHandler;

    Transform rememberParent;
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
        LevelData.LoadLevelDataEventHandler += HandleLoadLevelData;
        CheatsManager.CheatEventHandler += HandleCheat;
        paperValues.OnEnable();
    }

    void OnDisable()
    {
        LevelData.LoadLevelDataEventHandler -= HandleLoadLevelData;
        CheatsManager.CheatEventHandler -= HandleCheat;
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

    /// <summary>
    ///     <para>
    ///     Called on mouse interaction on paper scraps. <br/>
    ///     Grabs the paper on mouse down, and drops on mouse up.
    ///     </para>
    /// </summary>
    void PaperInteraction(InteractionType interactionType, Paper paper)
    {
        Transform dragParent = GameManager.Instance.LevelData.DragParent;
        switch (interactionType)
        {
            // Grab Paper
            case InteractionType.Click:
                Debug.Log("handle click on paper");
                if (paperValues.HoldingPaper != null)
                    return;

                for (int i = 0; i < GameManager.Instance.LevelData.DragParent.childCount; i++)
                {
                    GameManager.Instance.LevelData.DragParent.GetChild(i).SetParent(rememberParent);
                    Debug.LogWarning("Set parent to remember parent. This should normally not happen. ");
                }

                // Sets the paper's parent to the mouse and informs listeners of any state changes.

                rememberParent = paper.transform.parent;
                paper.transform.SetParent(dragParent, worldPositionStays);
                paper.SpriteRenderer.sortingOrder = order++;
                OnPaperAction(paper, PaperActionType.Grab);
            break;

            // Drop Paper
            case InteractionType.Release:
                if (paperValues.HoldingPaper != paper || paperValues.HoldingPaper == null)
                    return;

                // Resets the paper's parent and informs listeners of any state changes.
                paper.transform.SetParent(rememberParent, worldPositionStays);
                PaperActionType paperActionType = CheckPosition(paper) ? PaperActionType.StartSnap : PaperActionType.Drop;
                OnPaperAction(paper, paperActionType);
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
        GameManager.Instance.LevelData.DragParent.transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Clamper.CalculateBounds(GameManager.Instance.LevelData.DragParentSpriteRenderer, out float width, out float height, out Vector2 screenBounds);
        Clamper.ClampToScreenOrthographic(GameManager.Instance.LevelData.DragParent, width, height, screenBounds);
        if (Input.mouseScrollDelta.y != 0)
            GameManager.Instance.LevelData.DragParent.transform.Rotate(Input.mouseScrollDelta.y * rotationSpeed * Time.deltaTime * Vector3.forward, space);
    }

    /// <summary>
    ///     <para>
    ///     Should only be called when we drop a paper. <br/>
    ///     Checks if we should snap the paper
    ///     </para>
    /// </summary>
    /// <returns> True if we start snapping the paper </returns>
    bool CheckPosition(Paper paper)
    {
        // Checks Position
        var position = paper.transform.localPosition;
        
        isXClose = (position.x >= 0 && position.x <= levelData.PositionalLeniency) || (position.x <= 0 && position.x > -levelData.PositionalLeniency); // Close to 0
        isYClose = (position.y > 0 && position.y <= levelData.PositionalLeniency) || (position.y <= 0 && position.y > -levelData.PositionalLeniency); // Close to 0

        // Check Rotation
        Quaternion rotation = paper.transform.rotation;

        isZRotationBetweenZeroAndPositiveLeniency = rotation.z > 0 && rotation.z <= levelData.RotationalLeniency;     // Close to 0
        isZRotationBetweenZeroAndNegativeLeniency = rotation.z <= 0 && rotation.z > -levelData.RotationalLeniency;    // Close to 0

        bool isCloseZ = isZRotationBetweenZeroAndPositiveLeniency || isZRotationBetweenZeroAndNegativeLeniency;

        isWRotationBetweenOneAndPositiveLeniency = rotation.w > 1 && rotation.w <= (1 + levelData.RotationalLeniency);    // Close to 1 (>1 && <1.1) leniency of .1
        isWRotationBetweenOneAndNegativeLeniency = rotation.w <= 1 && rotation.w > -(1 - levelData.RotationalLeniency);   // Close to 1 (<1 && >-.9) leniency of .1

        isFlippedWRotationBetweenNegativeOneAndNegativeLeniency = rotation.w > -1 && rotation.w <= -(1 - levelData.RotationalLeniency);  // Close to -1 (>-1 && <-.9) leniency of .1
        isFlippedWRotationBetweenNegativeOneAndPositiveLeniency = rotation.w <= -1 && rotation.w > -(1 - levelData.RotationalLeniency);  // Close to -1 (<-1 && >-.9) leniency of .1

        bool isCloseW = (isWRotationBetweenOneAndNegativeLeniency || isWRotationBetweenOneAndPositiveLeniency) || (isFlippedWRotationBetweenNegativeOneAndNegativeLeniency || isFlippedWRotationBetweenNegativeOneAndPositiveLeniency);

        bool startSnap = isXClose && isYClose && isCloseZ && isCloseW;
#if DEBUG
        if (autoSnap)
            startSnap = true;
#endif
        if (startSnap)
        {
            OnPaperAction(paper, PaperActionType.StartSnap);
            StartCoroutine(LerpSnap(paper));
            return true;
        }
        return false;
    }

    /// <summary>
    ///     Moves the last held paper to the correct position over time.
    /// </summary>
    IEnumerator LerpSnap(Paper paper)
    {
        float time = 0;
        paper.transform.GetPositionAndRotation(out Vector3 startingPosition, out Quaternion startingRotation);
        
        while (time < 1)
        {
            if (paperValues.HoldingPaper == paper)
                yield break;

            if (paper.transform.parent == GameManager.Instance.LevelData.DragParent)
                paper.transform.SetParent(rememberParent);

            paper.transform.SetPositionAndRotation(Vector3.Lerp(startingPosition, Vector3.zero, lerpCurve.Evaluate(time)), Quaternion.Slerp(startingRotation, Quaternion.Euler(0, 0, 0), lerpCurve.Evaluate(time)));
            time += Time.deltaTime * lerpSpeed;
            
            yield return null;
        }

        paper.SpriteRenderer.sortingOrder = 0;
        OnPaperAction(paper, PaperActionType.Snap);
    }

    [Serializable]
    class PaperVariables
    {
        public Paper HoldingPaper { get; private set; }
        public Paper DroppedPaper {get; private set; }

        public void OnEnable()
            => PaperActionEventHandler += HandlePaperAction;

        public void OnDisable()
            => PaperActionEventHandler -= HandlePaperAction;

        void HandlePaperAction(object sender, PaperActionEventArgs e)
        {
            HoldingPaper = null;
            DroppedPaper = null;

            switch (e.actionType)
            {
                case PaperActionType.Grab:
                    HoldingPaper = e.paper;
                break;

                case PaperActionType.Drop:
                    DroppedPaper = e.paper;
                break;
            }
        }
    }


}
