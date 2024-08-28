using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MovePaper.PaperActionEventArgs;
using static GameManager;

public class AudioManager : MonoBehaviour
{
    [Header("Paper Sounds")]
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

    public Dictionary<PaperActionType, AudioSource> ActionsToAudio;
    public Dictionary<GameState, AudioSource> GameStateToAudio;

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

        GameStateToAudio = new()
        {
            { GameState.StartLevel,     shuffle },
            { GameState.RestartLevel,   null    },
            { GameState.BeatLevel,      null    },
            { GameState.RunGame,        null    },
            { GameState.BeatGame,       null    },
        };
    }

    /// <summary>
    ///     Plays audio for corresponding paper actions
    /// </summary>
    void HandlePaperAction(object sender, MovePaper.PaperActionEventArgs e)
    {
        Debug.Log($"Handled paper action {e.actionType}");
        if (ActionsToAudio.TryGetValue(e.actionType, out var audio) && audio != null)
            audio.Play();
    }

    /// <summary>
    ///     Plays audio for corresponding game state actions
    /// </summary>
    void HandleGameStateChange(object sender, GameManager.GameStateChangeEventArgs e)
    {
        Debug.Log($"Handled game action {e.newState}");
        if (GameStateToAudio.TryGetValue(e.newState, out var audio) && audio != null)
            audio.Play();
    }
}
