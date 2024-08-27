using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static MovePaper.PaperActionEventArgs;
using static TMPro.Examples.ObjectSpin;

public class MovePaper : Singleton<MovePaper>
{
    [SerializeField] Transform dragParent;
    [SerializeField] SpriteRenderer dragParentSpriteRenderer;

    [Header("Rotation Properties")]
    [SerializeField] float rotationSpeed;
    [SerializeField] Space space;

    [Header("Snap Properties")]
    [SerializeField] bool worldPositionStays;
    [SerializeField] float positionalLeniency;
    [SerializeField] float rotationalLeniency;
    [SerializeField] AnimationCurve lerpCurve;
    [SerializeField] float lerpSpeed;
    [SerializeField] bool fixedSpeed;

    int order = 0;

    Transform rememberParent;

    public static event EventHandler<PaperActionEventArgs> PaperAction;

    protected virtual void OnPaperAction(Paper paper, PaperActionType paperAction)
        => PaperAction?.Invoke(this, new(paper, paperAction));

    public class PaperActionEventArgs : EventArgs
    {
        public enum PaperActionType { Grab, Drop, StartSnap, Snap, Shuffle }

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
        Paper.PaperInteraction += HandlePaperInteraction;
        paperValues.OnEnable();
    }

    void OnDisable()
    {
        Paper.PaperInteraction -= HandlePaperInteraction;
        paperValues.OnDisable();
    }

    /// <summary>
    ///     <para>
    ///     Called on mouse interaction on paper scraps. <br/>
    ///     Grabs the paper on mouse down, and drops on mouse up.
    ///     </para>
    /// </summary>
    void HandlePaperInteraction(object sender, Paper.PaperInteractionEventArgs e)
    {
        if (sender is not Paper)
            return;

        Paper paper = sender as Paper;

        Func<Paper, bool> paperAction = e.interaction switch
        {
            Paper.PaperInteractionEventArgs.InteractionType.Click => TryGrabPaper,
            Paper.PaperInteractionEventArgs.InteractionType.Release => TryDropPaper,
            _ => null
        };
        paperAction(paper);
    }

    void Update()
    {
        // Moves & Rotates Paper
        dragParent.transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Clamper.CalculateBounds(dragParentSpriteRenderer, out float width, out float height, out Vector2 screenBounds);
        Clamper.ClampToScreenOrthographic(dragParent, width, height, screenBounds);
        if (Input.mouseScrollDelta.y != 0)
            dragParent.transform.Rotate(Input.mouseScrollDelta.y * rotationSpeed * Time.deltaTime * Vector3.forward, space);

        // Debug
        if (paperValues.HoldingPaper == null)
            return;
        Quaternion rotation = paperValues.HoldingPaper.transform.rotation;
        Debug.Log($"Rotation is : (z){rotation.z} | (w){rotation.w}");
    }

    bool isXClose;
    bool isYClose;
    bool isZRotationClosePlus;
    bool isZRotationCloseMinus;
    bool isWRotationClosePlus;
    bool isWRotationCloseMinus;

    /// <summary>
    ///     Resets the paper's parent and informs listeners of any state changes.
    /// </summary>
    /// <param name="sender"> The paper calling this method. </param>
    /// <returns> True if the paper is successfully dropped. </returns>
    public bool TryDropPaper(Paper paper)
    {
        if (paperValues.HoldingPaper != paper)
            return false;

        paper.transform.SetParent(rememberParent, worldPositionStays);

        PaperActionType paperActionType = CheckPosition(paper) ? PaperActionType.StartSnap : PaperActionType.Drop;
        OnPaperAction(paper, paperActionType);
        return true;
    }

    /// <summary>
    ///     <para>
    ///     Should only be called when we drop a paper. <br/>
    ///     Checks if the dropped paper is close to 0, 0 and 0, 1.
    ///     </para>
    /// </summary>
    /// <returns> True if we start snapping the paper </returns>
    bool CheckPosition(Paper paper)
    {
        // Check Position
        var position = paper.transform.localPosition;
        
        isXClose = (position.x >= 0 && position.x <= positionalLeniency) || (position.x <= 0 && position.x > -positionalLeniency);
        isYClose = (position.y > 0 && position.y <= positionalLeniency) || (position.y <= 0 && position.y > -positionalLeniency);

        // Check Rotation
        Quaternion rotation = paper.transform.rotation;

        isZRotationClosePlus = rotation.z > 0 && rotation.z <= rotationalLeniency;
        isZRotationCloseMinus = rotation.z <= 0 && rotation.z > -rotationalLeniency;

        bool isCloseZ = isZRotationClosePlus || isZRotationCloseMinus;

        isWRotationClosePlus = rotation.w > 1 && rotation.w <= rotationalLeniency;
        isWRotationCloseMinus = rotation.w <= 1 && rotation.w > -rotationalLeniency;

        bool isCloseW = isWRotationCloseMinus || isWRotationClosePlus;

        // Snap Position
        if (isXClose && isYClose && isCloseZ && isCloseW)
        {
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
            paper.transform.SetPositionAndRotation(Vector3.Lerp(startingPosition, Vector3.zero, lerpCurve.Evaluate(time)), Quaternion.Slerp(startingRotation, Quaternion.Euler(0, 0, 0), lerpCurve.Evaluate(time)));
            time += Time.deltaTime * lerpSpeed;
            
            yield return null;
        }

        OnPaperAction(paper, PaperActionType.Snap);
    }

    /// <summary>
    ///     Sets the paper's parent to the mouse and informs listeners of any state changes.
    /// </summary>
    /// <param name="caller"></param>
    /// <returns></returns>
    public bool TryGrabPaper(Paper paper)
    {
        if (paperValues.HoldingPaper != null)
            return false;

        rememberParent = paper.transform.parent;
        paper.transform.SetParent(dragParent, worldPositionStays);

        paper.SpriteRenderer.sortingOrder = order++;

        OnPaperAction(paper, PaperActionType.Grab);
        return true;
    }

    [Serializable]
    class PaperVariables
    {
        public Paper HoldingPaper { get; private set; }
        public Paper DroppedPaper {get; private set; }
        public Paper LerpPaper {get; private set; }


        public void OnEnable()
            => PaperAction += HandlePaperAction;

        public void OnDisable()
            => PaperAction -= HandlePaperAction;

        void HandlePaperAction(object sender, PaperActionEventArgs e)
        {
            HoldingPaper = null;
            DroppedPaper = null;
            LerpPaper = null;

            switch (e.actionType)
            {
                case PaperActionType.Grab:
                    HoldingPaper = e.paper;
                break;

                case PaperActionType.Drop:
                    DroppedPaper = e.paper;
                break;

                case PaperActionType.StartSnap:
                    LerpPaper = e.paper;
                break;

                case PaperActionType.Snap:
                case PaperActionType.Shuffle:
                break;
            }
        }
    }

    
}
