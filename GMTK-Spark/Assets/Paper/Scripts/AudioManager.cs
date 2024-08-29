using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MovePaper.PaperActionEventArgs;
using static MovePaper;
using static GameManager;
using static UIButton;


public class AudioManager : MonoBehaviour
{
    [Header("Paper Sounds")]
    [SerializeField] AudioSource dropPaper;
    [SerializeField] AudioSource shuffle;
    [SerializeField] AudioSource grabPaper;
    [SerializeField] AudioSource snap;

    [Header("UI")]
    [SerializeField] AudioSource buttonEnter;
    [SerializeField] AudioSource buttonClick;
    [SerializeField] AudioSource buttonUp;
    [SerializeField] AudioSource buttonExit;

    [Header("Game State")]
    [SerializeField] AudioSource win;

    [Header("Audio Properties")]
    [SerializeField] bool shortPickup;
    [SerializeField] bool snap1;

    public Dictionary<PaperActionEventArgs.PaperActionType, AudioSource> ActionsToAudio;
    public Dictionary<UIButton.ButtonInteractType, AudioSource> UIInteractToAudio;
    public Dictionary<GameManager.GameState, AudioSource> GameStateToAudio;

    private void OnEnable()
    {
        MovePaper.PaperAction += HandlePaperAction;
        GameManager.GameStateChangeEventHandler += HandleGameStateChange;
        UIButton.UIInteractEventHandler += HandleUIInteract;
    }
    
    private void OnDisable()
    {
        MovePaper.PaperAction -= HandlePaperAction;
        GameManager.GameStateChangeEventHandler -= HandleGameStateChange;
        UIButton.UIInteractEventHandler -= HandleUIInteract;
    }

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

        UIInteractToAudio = new()
        {
            { ButtonInteractType.Enter, buttonEnter },
            { ButtonInteractType.Click, buttonClick },
            { ButtonInteractType.Up,    buttonUp    },
            { ButtonInteractType.Exit,  buttonExit  },
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

    /// <summary>
    ///     Plays audio for corresponding UI interactions
    /// </summary>
    void HandleUIInteract(object sender, UIButton.UIInteractEventArgs e)
    {
        if (UIInteractToAudio.TryGetValue(e.buttonInteraction, out var audio) && audio != null)
            audio.Play();
    }
}
