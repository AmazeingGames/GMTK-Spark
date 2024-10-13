using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

// Contains all code for loading screens and screen transitions
public class ScreenTransitions : Singleton<ScreenTransitions>
{
    [field: Header("Transition Data")]
    [SerializeField] TransitionData transitionInData;
    [SerializeField] TransitionData transitionOutData;
    [field: SerializeField] float distance;

    [SerializeField] TransitionTypes transitionType;

    public enum TransitionTypes { AlwaysMove, OnlyWhenNecessary, Stacked }

    [Serializable]
    class TransitionData
    {
        [field: SerializeField] public AnimationCurve Curve { get; private set; }
        [field: SerializeField] public float Speed { get; private set; }
    }
    public enum OthogonalDirection { Up,  Down, Left, Right }

    public static event EventHandler<ScreenTransitionsEventArgs> ScreenTransitionEventHandler;

    public bool IsTransitioning { get; private set; } = false;
    public class ScreenTransitionsEventArgs : EventArgs { }

    Transform lastElements;
    bool lastIsReadying;
    OthogonalDirection lastDirection;
    bool neededToMoveOutOfFrame;
    bool wasNested;
    // Update is called once per frame
    void Update()
    {
# if DEBUG
        if (Input.GetKeyDown(KeyCode.Space) && lastElements != null)
            StartTransition(lastElements, lastIsReadying, lastDirection, neededToMoveOutOfFrame, wasNested);
#endif
    }

    OthogonalDirection OppositeDirection(OthogonalDirection direction)
    {
        return direction switch
        {
            OthogonalDirection.Up => OthogonalDirection.Down,
            OthogonalDirection.Down => OthogonalDirection.Up,
            OthogonalDirection.Left => OthogonalDirection.Right,
            OthogonalDirection.Right => OthogonalDirection.Left,
            _ => throw new NotImplementedException(),
        };
    }

    public Coroutine StartTransition(Transform elements, bool isReadying, OthogonalDirection slideInDirection, bool needsToMoveOutOfFrame, bool wasNested)
    {
        lastElements = elements;
        lastIsReadying = isReadying;
        lastDirection = slideInDirection;
        neededToMoveOutOfFrame = needsToMoveOutOfFrame;
        this.wasNested = wasNested;

        if (elements == null)
        {
            Debug.Log("Could not transition due to null elements");
            return null;
        }
        var position = elements.localPosition;

        var transitionData = isReadying ? transitionInData : transitionOutData;

        var slideDirection = slideInDirection;
        if (isReadying)
        {
            elements.localPosition = slideInDirection switch
            {
                OthogonalDirection.Up => new(position.x, -distance),
                OthogonalDirection.Down => new(position.x, distance),
                OthogonalDirection.Left => new(distance, position.y),
                OthogonalDirection.Right => new(-distance, position.y),
                _ => throw new NotImplementedException($"Othogonal Direction {slideInDirection} not implemented")
            };
        }
        else
            slideDirection = OppositeDirection(slideDirection);

        return StartCoroutine(SlideElements(elements, transitionData, isReadying, slideDirection, needsToMoveOutOfFrame, wasNested));
    }

    // I can either move the game elements or I can move the game camera
    IEnumerator SlideElements(Transform elements, TransitionData transitionData, bool movingInFrame, OthogonalDirection slideDirection, bool mustMoveOutOfFrame, bool wasNested)
    {
        var position = elements.localPosition;
        Vector2 startingPosition = elements.transform.localPosition;
        Vector2 goalPosition;

        if (movingInFrame)
            goalPosition = Vector2.zero;
        else    
            goalPosition = slideDirection switch
            {
                OthogonalDirection.Up => new(position.x, position.y + distance),
                OthogonalDirection.Down => new(position.x, position.y - distance),
                OthogonalDirection.Left => new(position.x - distance, position.y),
                OthogonalDirection.Right => new(position.x + distance, position.y),
                _ => throw new NotImplementedException($"Othogonal Direction {slideDirection} not implemented")
            };

        float current = 0;

        if (movingInFrame && wasNested && transitionType == TransitionTypes.Stacked)
        {
            Debug.Log("Set current to 1");
            current = 1;
        }

        while (current < 1)
        {
            IsTransitioning = true;
            current = Mathf.MoveTowards(current, 1, transitionData.Speed * Time.deltaTime);

            switch (transitionType)
            {
                case TransitionTypes.AlwaysMove:
                    LerpElements();
                break;

                case TransitionTypes.Stacked:
                    if (movingInFrame || mustMoveOutOfFrame || (!movingInFrame && !wasNested))
                        LerpElements();
                break;

                case TransitionTypes.OnlyWhenNecessary:
                    if (movingInFrame || mustMoveOutOfFrame)
                        LerpElements();
                break;
            }

            void LerpElements()
                => elements.localPosition = Vector3.Lerp(startingPosition, goalPosition, transitionData.Curve.Evaluate(current));

            yield return null;
        }
        elements.localPosition = goalPosition;
        IsTransitioning = false;
    }
}
