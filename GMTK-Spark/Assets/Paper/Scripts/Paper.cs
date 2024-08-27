using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Paper : MonoBehaviour
{
    [field: SerializeField] public SpriteRenderer SpriteRenderer { get; private set; }
    [SerializeField] PolygonCollider2D polygonCollider;

    bool isHolding;

    public bool IsInPlace { get; private set; } = false;

    public static event EventHandler<PaperInteractionEventArgs> PaperInteraction;

    public class PaperInteractionEventArgs : EventArgs
    {
        public enum InteractionType { Click, Release }

        public readonly InteractionType interaction;

        public PaperInteractionEventArgs(InteractionType interaction)
            => this.interaction = interaction;
    }

    public void OnPaperInteraction(PaperInteractionEventArgs.InteractionType interaction)
        => PaperInteraction(this, new (interaction));

    private void OnEnable()
        => MovePaper.PaperAction += HandlePaperAction;

    private void OnDisable()
        => MovePaper.PaperAction -= HandlePaperAction;

    private void OnMouseDown()
    {
        if (isHolding)
            return;

        OnPaperInteraction(PaperInteractionEventArgs.InteractionType.Click);
    }

    private void OnMouseUp()
    {
        if (!isHolding)
            return;

        OnPaperInteraction(PaperInteractionEventArgs.InteractionType.Release);
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
            case MovePaper.PaperActionEventArgs.PaperActionType.Grab:
                isHolding = true;
                IsInPlace = false;
            break;

            case MovePaper.PaperActionEventArgs.PaperActionType.Drop:
                isHolding = false;
            break;

            case MovePaper.PaperActionEventArgs.PaperActionType.StartSnap:
                IsInPlace = false;
                isHolding = false;
            break;

            case MovePaper.PaperActionEventArgs.PaperActionType.Snap:
                IsInPlace = true;
                isHolding = false;
            break;
        }
    }
}
