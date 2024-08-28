using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static MovePaper.PaperActionEventArgs;
using static TMPro.Examples.ObjectSpin;
using static Paper.PaperInteractionEventArgs;

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
        switch (e.interaction)
        {
            // Grab Paper
            case InteractionType.Click:
                if (paperValues.HoldingPaper != null)
                    break;

                // Sets the paper's parent to the mouse and informs listeners of any state changes.
                rememberParent = e.paper.transform.parent;
                e.paper.transform.SetParent(dragParent, worldPositionStays);
                e.paper.SpriteRenderer.sortingOrder = order++;
                OnPaperAction(e.paper, PaperActionType.Grab);
            break;

            // Drop Paper
            case InteractionType.Release:
                if (paperValues.HoldingPaper != e.paper)
                    break;

                // Resets the paper's parent and informs listeners of any state changes.
                e.paper.transform.SetParent(rememberParent, worldPositionStays);
                PaperActionType paperActionType = CheckPosition(e.paper) ? PaperActionType.StartSnap : PaperActionType.Drop;
                OnPaperAction(e.paper, paperActionType);
            break;
        }
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
    bool isZRotationBetweenZeroAndPositiveLeniency;
    bool isZRotationBetweenZeroAndNegativeLeniency;
    bool isWRotationBetweenOneAndPositiveLeniency;
    bool isWRotationBetweenOneAndNegativeLeniency;
    
    bool isFlippedWRotationBetweenNegativeOneAndNegativeLeniency; // Close to 1
    bool isFlippedWRotationBetweenNegativeOneAndPositiveLeniency; // Close to 1

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
        
        isXClose = (position.x >= 0 && position.x <= positionalLeniency) || (position.x <= 0 && position.x > -positionalLeniency); // Close to 0
        isYClose = (position.y > 0 && position.y <= positionalLeniency) || (position.y <= 0 && position.y > -positionalLeniency); // Close to 0

        // Check Rotation
        Quaternion rotation = paper.transform.rotation;

        isZRotationBetweenZeroAndPositiveLeniency = rotation.z > 0 && rotation.z <= rotationalLeniency;     // Close to 0
        isZRotationBetweenZeroAndNegativeLeniency = rotation.z <= 0 && rotation.z > -rotationalLeniency;    // Close to 0

        bool isCloseZ = isZRotationBetweenZeroAndPositiveLeniency || isZRotationBetweenZeroAndNegativeLeniency;

        isWRotationBetweenOneAndPositiveLeniency = rotation.w > 1 && rotation.w <= (1 + rotationalLeniency);    // Close to 1 (>1 && <1.1) leniency of .1
        isWRotationBetweenOneAndNegativeLeniency = rotation.w <= 1 && rotation.w > -(1 - rotationalLeniency);   // Close to 1 (<1 && >-.9) leniency of .1

        isFlippedWRotationBetweenNegativeOneAndNegativeLeniency = rotation.w > -1 && rotation.w <= -(1 - rotationalLeniency);  // Close to -1 (>-1 && <-.9) leniency of .1
        isFlippedWRotationBetweenNegativeOneAndPositiveLeniency = rotation.w <= -1 && rotation.w > -(1 - rotationalLeniency);  // Close to -1 (<-1 && >-.9) leniency of .1

        bool isCloseW = (isWRotationBetweenOneAndNegativeLeniency || isWRotationBetweenOneAndPositiveLeniency) || (isFlippedWRotationBetweenNegativeOneAndNegativeLeniency || isFlippedWRotationBetweenNegativeOneAndPositiveLeniency);

        // Start snap Position
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
