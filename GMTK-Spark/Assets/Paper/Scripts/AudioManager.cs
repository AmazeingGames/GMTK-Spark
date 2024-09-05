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
    [SerializeField] bool stopMusicByMute = false;

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
            { GameState.EnterMainMenu,  mainMenuMusic },
            { GameState.StartLevel,     gameplayMusic },
            { GameState.ResumeGame,     gameplayMusic },
            { GameState.RestartLevel,   gameplayMusic },
            { GameState.PauseGame,      pauseMenuMusic }
        };

    }

    /// <summary>
    ///     Plays audio for corresponding paper actions
    /// </summary>
    void HandlePaperAction(object sender, MovePaper.PaperActionEventArgs e)
    {
        if (ActionsToSFX.TryGetValue(e.actionType, out var sfx) && sfx != null)
            sfx.Play();
        Debug.Log($"AudioManager: Handled paper action {e.actionType} {(sfx == null ? "" : $"and played sfx : {sfx}")}");
    }

    /// <summary>
    ///     Plays sfx for corresponding game state actions
    /// </summary>
    void HandleGameStateChange(object sender, GameManager.GameStateChangeEventArgs e)
    {
        if (GameStateToSFX.TryGetValue(e.newState, out var sfx) && sfx != null)
            sfx.Play();
        if (GameStateToMusic.TryGetValue(e.newState, out var music) && music != null)
        {
            if (currentMusic != null)
            {
                if (stopMusicByMute)
                    currentMusic.mute = true;
                else
                    currentMusic.Stop();

            }
            currentMusic = music;
            currentMusic.mute = false;

            if (!stopMusicByMute)
                currentMusic.Play();
            else if (!currentMusic.isPlaying)
                currentMusic.Play();
        }

        Debug.Log($"AudioManager: Handled game action {e.newState} {(sfx == null ? "" : $"and played sfx : {sfx}")} {(music == null ? "" : $", as well as changed music track : {music}")}");

    }

    /// <summary>
    ///     Plays sfx for corresponding UI interactions
    /// </summary>
    void HandleUIInteract(object sender, UIButton.UIInteractEventArgs e)
    {
        if (UIInteractToSFX.TryGetValue(e.buttonInteraction, out var sfx) && sfx != null)
            sfx.Play();
        Debug.Log($"AudioManager: Handled UI interaction {e.buttonInteraction} {(sfx == null ? "" : $"and played sfx : {sfx}")}");

    }
}
