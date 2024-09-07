using System.Collections.Generic;
using UnityEngine;
using static GameManager;
using static MovePaper;
using static MovePaper.PaperActionEventArgs;
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
    public Dictionary<GameManager.GameAction, AudioSource> GameActionToSFX;
    public Dictionary<GameManager.GameState, AudioSource> GameStateToMusic;

    private void OnEnable()
    {
        MovePaper.PaperActionEventHandler += HandlePaperAction;
        GameManager.GameStateChangeEventHandler += HandleGameStateChange;
        UIButton.UIInteractEventHandler += HandleUIInteract;
    }
    
    private void OnDisable()
    {
        MovePaper.PaperActionEventHandler -= HandlePaperAction;
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

        GameActionToSFX = new()
        {
            { GameAction.StartLevel,       shuffle },
            { GameAction.RestartLevel,     null    },
            { GameAction.CompleteLevel,    win     },
            { GameAction.EnterMainMenu,    null    },
            { GameAction.BeatGame,         null    },
        };

        GameStateToMusic = new()
        {
            { GameState.InMenu,   mainMenuMusic },
            { GameState.Running,   gameplayMusic },
            { GameState.Paused,   pauseMenuMusic }
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
    ///     Plays sfx for corresponding game state change.
    /// </summary>
    void HandleGameStateChange(object sender, GameManager.GameStateChangeEventArgs e)
    {
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

        Debug.Log($"AudioManager: Handled game state change {(music == null ? "" : $"and changed music track to : {music}")}");
    }

    /// <summary>
    ///     Plays a SFX corresponding to a game action
    /// </summary>
    void HandleGameAction(object sender, GameManager.GameActionEventArgs e)
    {
        if (GameActionToSFX.TryGetValue(e.gameAction, out var sfx) && sfx != null)
            sfx.Play();

        Debug.Log($"AudioManager: Handled game action {e.gameAction}{(sfx == null ? "" : $"and played sfx : {sfx}")}");
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
