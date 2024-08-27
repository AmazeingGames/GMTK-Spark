using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MovePaper.PaperActionEventArgs;

public class AudioManager : Singleton<AudioManager>
{
    [Header("Paper")]
    [SerializeField] AudioSource dropPaper;
    [SerializeField] AudioSource shuffle;
    [SerializeField] AudioSource grabPaper;
    [SerializeField] AudioSource snap;

    [Header("UI")]
    [SerializeField] AudioSource select;
    [SerializeField] AudioSource back;
    [SerializeField] AudioSource start;
    [SerializeField] AudioSource move;

    [Header("Game State")]
    [SerializeField] AudioSource win;

    [Header("Audio Properties")]
    [SerializeField] bool shortPickup;
    [SerializeField] bool snap1;

    enum PaperSounds { Drop, Shuffle, Grab, Snap }
    enum UISounds { Select, Back, Start, Move }
    enum GameState { Win, Correct }

    public Dictionary<PaperActionType, AudioSource> ActionsToAudio;
    private void OnEnable()
        => MovePaper.PaperAction += HandlePaperAction;

    private void OnDisable()
        => MovePaper.PaperAction -= HandlePaperAction;

    private void Start()
    {
        ActionsToAudio = new()
        {
            { PaperActionType.Grab,     grabPaper   },
            { PaperActionType.Drop,     dropPaper   },
            { PaperActionType.Snap,     snap        },
            { PaperActionType.Shuffle,  shuffle     },
        };
    }

    /// <summary>
    ///     
    /// </summary>
    void HandlePaperAction(object sender, MovePaper.PaperActionEventArgs e)
    {
        Debug.Log($"Handled paper action {e.actionType}");
        if (ActionsToAudio.TryGetValue(e.actionType, out var audio))
            audio.Play();
    }

    public void PlayWinSound()
        => win.Play();
}
