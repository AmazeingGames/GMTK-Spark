using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

// Contains all code for loading screens and screen transitions
public class ScreenTransitions : Singleton<ScreenTransitions>
{
    [field: SerializeField] public AnimationCurve BounceCurve { get; private set; }
    [field: SerializeField] public float Speed { get; private set; }
    [SerializeField] float distanceToMove;
    public enum OthogonalDirection { Up,  Down, Left, Right }

    public static event EventHandler<ScreenTransitionsEventArgs> ScreenTransitionEventHandler;

    public bool IsTransitioning { get; private set; } = false;
    public class ScreenTransitionsEventArgs : EventArgs
    {
        public ScreenTransitionsEventArgs() 
        {
            
        }
    }

    Transform lastElements;
    bool lastIsReadying;
    OthogonalDirection lastDirection;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
# if DEBUG
        if (Input.GetKeyDown(KeyCode.Space) && lastElements != null)
            StartTransition(lastElements, lastIsReadying, lastDirection);
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

    public Coroutine StartTransition(Transform elements, bool isReadying, OthogonalDirection slideInDirection)
    {
        lastElements = elements;
        lastIsReadying = isReadying;
        lastDirection = slideInDirection;
        if (elements == null)
        {
            Debug.Log("Could not transition due to null elements");
            return null;
        }
        var position = elements.localPosition;

        var slideDirection = slideInDirection;
        if (isReadying)
        {
            elements.localPosition = slideInDirection switch
            {
                OthogonalDirection.Up => new(position.x, position.y - distanceToMove),
                OthogonalDirection.Down => new(position.x, position.y + distanceToMove),
                OthogonalDirection.Left => new(position.x + distanceToMove, position.y),
                OthogonalDirection.Right => new(position.x - distanceToMove, position.y),
                _ => throw new NotImplementedException($"Othogonal Direction {slideInDirection} not implemented")
            };
        }
        else
            slideDirection = OppositeDirection(slideDirection);

        return StartCoroutine(SlideElements(elements, BounceCurve, Speed, isReadying, slideDirection));
    }

    // I can either move the game elements or I can use the game camera
    IEnumerator SlideElements(Transform elements, AnimationCurve curve, float speed, bool isReadying, OthogonalDirection slideDirection)
    {
        // Slide UI elements using lerp and an animation curve

        var position = elements.localPosition;
        Vector2 startingPosition = elements.transform.localPosition;
        Vector2 goalPosition;

        if (isReadying)
            goalPosition = Vector2.zero;
        else    
            goalPosition = slideDirection switch
            {
                OthogonalDirection.Up => new(position.x, position.y + distanceToMove),
                OthogonalDirection.Down => new(position.x, position.y - distanceToMove),
                OthogonalDirection.Left => new(position.x - distanceToMove, position.y),
                OthogonalDirection.Right => new(position.x + distanceToMove, position.y),
                _ => throw new NotImplementedException($"Othogonal Direction {slideDirection} not implemented")
            };
       
        float current = 0;

        while (current < 1)
        {
            IsTransitioning = true;
            current = Mathf.MoveTowards(current, 1, speed * Time.deltaTime);

            elements.localPosition = Vector3.Lerp(startingPosition, goalPosition, curve.Evaluate(current));
            yield return null;
        }
        IsTransitioning = false;
    }
}
