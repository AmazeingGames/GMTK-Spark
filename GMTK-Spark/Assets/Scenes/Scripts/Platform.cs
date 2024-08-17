using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Channel;
using static Switch;
using TMPro;

public class Platform : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] Signal signal;

    [Header("Components")]
    [SerializeField] GameObject platform;
    [SerializeField] TextMeshPro text;

    private void OnEnable()
    {
        RaiseSignal += OnActivateSwitch;
    }

    private void OnDisable()
    {
        RaiseSignal -= OnActivateSwitch;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        text.text = signal.ToString()[0].ToString();
    }

    void OnActivateSwitch(Signal signalChannel, Command command)
    {
        Debug.Log($"SignalChannel : {signalChannel} | Command : {command}");
        if (signalChannel != signal)
            return;

        switch (command)
        {
            case Command.Null:
                throw new System.Exception("Signal Response should not be null");

            case Command.Appear:
                platform.SetActive(true);
            break;

            case Command.Disappear:
                platform.SetActive(false);
            break;
        }
    }
}
