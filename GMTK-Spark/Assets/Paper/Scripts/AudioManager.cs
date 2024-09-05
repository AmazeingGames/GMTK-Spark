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

    [Header("Game States")]
    [SerializeField] AudioSource win;

    [Header("Music")]
    [SerializeField] AudioSource gameplayMusic;
    [SerializeField] AudioSource mainMenuMusic;
    [SerializeField] AudioSource pauseMenuMusic;

    AudioSource currentMusic;

    [Header("Audio Properties")]
    [SerializeField] bool shortPickup;
    [SerializeField] bool snap1;

    public Dictionary<PaperActionEventArgs.PaperActionType, AudioSource> ActionsToSFX;
    public Dictionary<UIButton.UIInteractionTypes, AudioSource> UIInteractToSFX;
    public Dictionary<GameManager.GameState, AudioSource> GameStateToSFX;
    public Dictionary<GameManager.GameState, AudioSource> GameStateToMusic;

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
        ActionsToSFX = new()
        {
            { PaperActionType.Grab,     grabPaper   },
            { PaperActionType.Drop,     dropPaper   },
            { PaperActionType.Snap,     snap        },
            { PaperActionType.Shuffle,  shuffle     },
        };

        UIInteractToSFX = new()
        {
            { UIInteractionTypes.Enter, buttonEnter },
            { UIInteractionTypes.Click, buttonClick },
            { UIInteractionTypes.Up,    buttonUp    },
            { UIInteractionTypes.Exit,  buttonExit  },
        };

        GameStateToSFX = new()
        {
            { GameState.StartLevel,     shuffle },
            { GameState.RestartLevel,   null    },
            { GameState.BeatLevel,      win    },
            { GameState.EnterMainMenu,  null    },
            { GameState.BeatGame,       null    },
        };

        GameStateToMusic = new()
        {
            { GameState.EnterMainMenu, mainMenuMusic },
            { GameState.StartLevel, gameplayMusic },
            { GameState.PauseGame, pauseMenuMusic }
        };

    }

    /// <summary>
    ///     Plays audio for corresponding paper actions
    /// </summary>
    void HandlePaperAction(object sender, MovePaper.PaperActionEventArgs e)
    {
        if (ActionsToSFX.TryGetValue(e.actionType, out var audio) && audio != null)
            audio.Play();
        Debug.Log($"AudioManager: Handled paper action {e.actionType} {(audio == null ? "" : $"and played audio : {audio}")}");
    }

    /// <summary>
    ///     Plays audio for corresponding game state actions
    /// </summary>
    void HandleGameStateChange(object sender, GameManager.GameStateChangeEventArgs e)
    {
        if (GameStateToSFX.TryGetValue(e.newState, out var audio) && audio != null)
            audio.Play();

        switch (e.newState)
        {
            case GameState.None:
                break;
            case GameState.EnterMainMenu:

                break;
            case GameState.StartLevel:
                break;
            case GameState.LoseLevel:
                break;
            case GameState.RestartLevel:
                break;
            case GameState.BeatLevel:
                break;
            case GameState.BeatGame:
                break;
        }

        Debug.Log($"AudioManager: Handled game action {e.newState} {(audio == null ? "" : $"and played audio : {audio}")}");
    }

    /// <summary>
    ///     Plays audio for corresponding UI interactions
    /// </summary>
    void HandleUIInteract(object sender, UIButton.UIInteractEventArgs e)
    {
        if (UIInteractToSFX.TryGetValue(e.buttonInteraction, out var audio) && audio != null)
            audio.Play();
        Debug.Log($"AudioManager: Handled UI interaction {e.buttonInteraction} {(audio == null ? "" : $"and played audio : {audio}")}");

    }
}
