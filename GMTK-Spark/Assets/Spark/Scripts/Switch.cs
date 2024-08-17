using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Channel;
using TMPro;
using UnityEditor;

//[ExecuteInEditMode]
public class Switch : MonoBehaviour
{
    public enum Command { Null, Appear, Disappear }
    //public enum SwitchState { On, Off }

    [Header("Properties")]
    [SerializeField] bool startingState;
    [SerializeField] Signal signal;
    [SerializeField] Color onColor;
    [SerializeField] Color offColor;

    [Header("Components")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] TextMeshPro text;

    public static event Action<Signal, Command> RaiseSignal;
    
    bool switchState;

    // Start is called before the first frame update
    void Start()
    {
        switchState = startingState;
        ActivateSwitch(false);
    }

    private void OnValidate()
    {
        text.text = signal.ToString()[0].ToString();
        switchState = startingState;
        ActivateSwitch(false);
    }

    private void OnMouseOver()
    { 
        if (Player.Instance.movementType == Player.MovementType.FollowMouse && Input.GetMouseButtonDown(0))
            ActivateSwitch();
    }

    void ActivateSwitch(bool changeState = true)
    {
        Debug.Log("Switch activated");
        
        if (signal == Signal.Null)
            throw new Exception("Output signal should not be null");

        if (changeState)
            switchState = !switchState;

        Command command = switchState ? Command.Appear : Command.Disappear;

        spriteRenderer.color = switchState ? onColor : offColor;

        RaiseSignal?.Invoke(signal, command);
    }
}
