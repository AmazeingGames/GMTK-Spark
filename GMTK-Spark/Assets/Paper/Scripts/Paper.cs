using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Paper : MonoBehaviour
{
    [field: SerializeField] public SpriteRenderer SpriteRenderer { get; private set; }
    [field: SerializeField] public PolygonCollider2D PolygonCollider2D { get; private set; }

    public bool IsInPlace { get; private set; }
    private void OnEnable()
    {
        MovePaper.GetMatchingPaperEventHandler += HandleGetMatchingPaper;
        MovePaper.PaperActionEventHandler += HandlePaperAction;
    }
    private void OnDisable()
    {
        MovePaper.GetMatchingPaperEventHandler -= HandleGetMatchingPaper;
        MovePaper.PaperActionEventHandler -= HandlePaperAction;
    }

    private void Start()
    {
        PolygonCollider2D = GetComponent<PolygonCollider2D>();
    }

    // Returns a reference to the Paper class if the given game object is a match
    void HandleGetMatchingPaper(object sender, MovePaper.GetMatchingPaperEventArgs e)
    {
        if (e.paperGameObject != gameObject)
            return;

        e.WriteResults(this);
    }

    /// <summary>
    ///     Updates properties to reflect the actions performed on this paper
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void HandlePaperAction(object sender, MovePaper.PaperActionEventArgs e)
    {
        if (e.paper != this)
            return;

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
