using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static MovePaper.PaperActionEventArgs.PaperActionType;
public class Paper : MonoBehaviour
{
    [Header("Paper")]
    public SpriteRenderer SpriteRenderer { get; private set; }
    public PolygonCollider2D PolygonCollider2D {get; private set; }

    public bool IsInPlace { get; private set; }
    public bool CanBeGrabbed { get; private set; } = true;
    private void OnEnable()
    {
        SpriteRenderer = GetComponent<SpriteRenderer>();
        PolygonCollider2D = GetComponent<PolygonCollider2D>();
        MovePaper.GetMatchingPaperEventHandler += HandleGetMatchingPaper;
        MovePaper.PaperActionEventHandler += HandlePaperAction;

        CanBeGrabbed = true;
    }
    private void OnDisable()
    {
        MovePaper.GetMatchingPaperEventHandler -= HandleGetMatchingPaper;
        MovePaper.PaperActionEventHandler -= HandlePaperAction;
    }

    // Returns a reference to the Paper class if the given game object is a match
    void HandleGetMatchingPaper(object sender, MovePaper.GetMatchingPaperEventArgs e)
    {
        if (e.paperGameObject != gameObject)
            return;

        e.WriteResults(this);
    }

    /// <summary>
    ///     Updates properties to reflect any actions performed on this paper.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void HandlePaperAction(object sender, MovePaper.PaperActionEventArgs e)
    {
        if (e.paper != this)
            return;

        CanBeGrabbed = e.actionType != MovePaper.PaperActionEventArgs.PaperActionType.StartSnap;
        IsInPlace = e.actionType == MovePaper.PaperActionEventArgs.PaperActionType.Snap;

        switch (e.actionType)
        {
            case MovePaper.PaperActionEventArgs.PaperActionType.Drop:
            case MovePaper.PaperActionEventArgs.PaperActionType.StartSnap:
            case MovePaper.PaperActionEventArgs.PaperActionType.Grab:
                IsInPlace = false;
            break;

            case MovePaper.PaperActionEventArgs.PaperActionType.Snap:
                IsInPlace = true;
            break;
        }
    }
}
