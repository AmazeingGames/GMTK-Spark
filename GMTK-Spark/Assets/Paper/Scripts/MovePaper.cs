using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static MovePaper.PaperActionEventArgs;

public class MovePaper : Singleton<MovePaper>
{
    [SerializeField] Transform dragParent;
    [SerializeField] bool worldPositionStays;
    [SerializeField] float rotationSpeed;
    [SerializeField] Space space;

    [Header("Snap Properties")]
    [SerializeField] float positionalLeniency;
    [SerializeField] Vector2 rotationalLeniency;
    [SerializeField] AnimationCurve lerpCurve;
    [SerializeField] float lerpSpeed;
    [SerializeField] bool fixedSpeed;

    int order = 0;

    Paper lerpPaper;
    Paper holdingPaper;
    Transform rememberParent;
    SpriteRenderer dragParentSpriteRenderer;

    public static event EventHandler<PaperActionEventArgs> PaperAction;

    protected virtual void OnPaperAction(PaperActionEventArgs e)
    {
        PaperAction?.Invoke(this, e);
    }

    public class PaperActionEventArgs : EventArgs
    {
        public PaperActionEventArgs(PaperActionType actionType)
        {
            ActionType = actionType;
        }

        public enum PaperActionType { Grab, Drop, Snap, DropSnap }

        public PaperActionType ActionType { get; private set; }
    }

    private void Start()
    {
        dragParentSpriteRenderer = dragParent.GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        dragParent.transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.mouseScrollDelta.y != 0)
        {
            dragParent.transform.Rotate(Input.mouseScrollDelta.y * rotationSpeed * Time.deltaTime * Vector3.forward, space);
        }

        Clamp.CalculateBounds(dragParentSpriteRenderer, out float width, out float height, out Vector2 screenBounds);
        Clamp.ClampToScreenOrthographic(dragParent, width, height, screenBounds);

        SnapPaper();
        DebugPaper();
    }

    void DebugPaper()
    {
        if (holdingPaper == null)
            return;

        Quaternion rotation = holdingPaper.transform.rotation;

        Debug.Log($"Rotation is : (z){rotation.z} | (w){rotation.w}");
    }

    bool SnapPaper()
    {
        if (holdingPaper == null)
            return false;

        var position = holdingPaper.transform.localPosition;

        bool isXClose = ((position.x >= 0 && position.x <= positionalLeniency) || (position.x <= 0 && position.x > -positionalLeniency));
        bool isYClose = ((position.y > 0 && position.y <= positionalLeniency) || (position.y <= 0 && position.y > -positionalLeniency));

        Quaternion rotation = holdingPaper.transform.rotation;

        bool isZRotationClose = ((rotation.z > 0 && rotation.z <= positionalLeniency) || (rotation.z <= 0 && rotation.z > -positionalLeniency));
        bool isWRotationClose = ((rotation.w > 1 && rotation.w <= positionalLeniency) || (rotation.w <= 1 && rotation.w > -positionalLeniency));

        if (isXClose && isYClose && isZRotationClose && isWRotationClose)
        {
            StartCoroutine(LerpSnap());
            return true;
        }
        return false;
    }

    // Turn this into a list with a Queue
    IEnumerator LerpSnap()
    {
        if (lerpPaper == null)
            yield break;

        // Instead I could rotate the parent until the rotation matches up
        float time = 0;
        lerpPaper.transform.GetPositionAndRotation(out Vector3 startingPosition, out Quaternion startingRotation);
        
        while (time < 1)
        {
            if (lerpPaper == null)
                yield break;

            lerpPaper.transform.rotation = Quaternion.Slerp(startingRotation, Quaternion.Euler(0, 0, 0), lerpCurve.Evaluate(time));
            lerpPaper.transform.position = Vector3.Lerp(startingPosition, Vector3.zero, lerpCurve.Evaluate(time));

            time += Time.deltaTime * lerpSpeed;
            
            yield return null;
        }
        PaperAction?.Invoke(this, new(PaperActionType.Snap));
        lerpPaper = null;
    }

    public bool TryGrabPaper(Paper paper)
    {
        if (holdingPaper != null)
            return false;

        
        holdingPaper = paper;
        rememberParent = holdingPaper.transform.parent;
        holdingPaper.transform.SetParent(dragParent, worldPositionStays);
        holdingPaper.SpriteRenderer.sortingOrder = order++;

        PaperAction?.Invoke(this, new(PaperActionType.Grab));
        return true;
    }

    public bool TryDropPaper(Paper paper)
    {
        if (holdingPaper != paper)
            return false;

        holdingPaper.transform.SetParent(rememberParent, worldPositionStays);
        lerpPaper = holdingPaper;
        bool snapPaper = SnapPaper();
        holdingPaper = null;

        
        PaperActionType paperActionType = snapPaper ? PaperActionType.DropSnap : PaperActionType.Drop;
        PaperAction?.Invoke(this, new(paperActionType));
        return true;
    }
}
