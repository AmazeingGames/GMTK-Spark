using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using static GameManager;
using static MovePaper;
using static MovePaper.PaperActionEventArgs;
using static ScreenTransitions;
using static UIButton;


public class AudioManager : MonoBehaviour
{

    [Header("Paper Sounds")]
    [SerializeField] AudioSource shuffle;
    [SerializeField] List<AudioSource> dropPaper;
    [SerializeField] List<AudioSource> grabPaper;
    [SerializeField] List<AudioSource> snap;

    [Header("UI Buttons")]
    [SerializeField] AudioSource buttonEnter;
    [SerializeField] AudioSource buttonClick;
    [SerializeField] AudioSource buttonUp;
    [SerializeField] AudioSource buttonExit;

    [Header("UI Menus")]
    [SerializeField] AudioSource openDiary;

    [Header("Game Actions")]
    [SerializeField] AudioSource startLevel;
    [SerializeField] AudioSource beatLevel;

    [Header("Music")]
    [SerializeField] AudioSource gameplayMusic;
    [SerializeField] AudioSource mainMenuMusic;
    [SerializeField] AudioSource pauseMenuMusic;

    [Header("Music Fade")]
    [SerializeField] bool stopMusicByMute = false;
    [SerializeField] float maxMusicVolume;
    [SerializeField] public AnimationCurve fadeCurve;
    [SerializeField] public float fadeSpeed;

    AudioSource currentMusic;

    [Header("Audio Properties")]
    [SerializeField] bool shortPickup;
    [SerializeField] bool snap1;

    [Header("Debug")]
    [SerializeField] bool logAudio;

    readonly Dictionary<List<AudioSource>, int> sfxsToLastIndex = new();
    public Dictionary<PaperActionEventArgs.PaperActionType, List<AudioSource>> ActionsToSFX;
    public Dictionary<UIButton.UIInteractionTypes, AudioSource> UIInteractToSFX;
    public Dictionary<GameManager.GameAction, AudioSource> GameActionToSFX;
    public Dictionary<GameManager.GameState, AudioSource> GameStateToMusic;
    public Dictionary<MenuManager.MenuTypes, AudioSource> OpenMenuToSFX;

    private void OnEnable()
    {
        MovePaper.PaperActionEventHandler += HandlePaperAction;
        GameManager.GameStateChangeEventHandler += HandleGameStateChange;
        UIButton.UIInteractEventHandler += HandleUIInteract;
        GameManager.GameActionEventHandler += HandleGameAction;
        MenuManager.MenuChangeEventHandler += HandleMenuChange;
    }
    
    private void OnDisable()
    {
        MovePaper.PaperActionEventHandler -= HandlePaperAction;
        GameManager.GameStateChangeEventHandler -= HandleGameStateChange;
        UIButton.UIInteractEventHandler -= HandleUIInteract;
        GameManager.GameActionEventHandler -= HandleGameAction;
        MenuManager.MenuChangeEventHandler -= HandleMenuChange;
    }

    private void Start()
    {
        ActionsToSFX = new()
        {
            { PaperActionType.Grab,     grabPaper   },
            { PaperActionType.Drop,     dropPaper   },
            { PaperActionType.Snap,     snap        },
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
            { GameAction.StartLevel,    shuffle      },
            { GameAction.LoadNextLevel, shuffle     },
            { GameAction.RestartLevel,  null         },
            { GameAction.CompleteLevel, beatLevel    },
            { GameAction.EnterMainMenu, null         },
            { GameAction.BeatGame,      null         },
        };

        GameStateToMusic = new()
        {
            { GameState.InMenu,  mainMenuMusic },
            { GameState.Running, gameplayMusic },
            { GameState.Paused,  pauseMenuMusic }
        };

        OpenMenuToSFX = new()
        {
            { MenuManager.MenuTypes.Diary, openDiary },
        };

    }

    /// <summary>
    ///     Given a list of audio sources, randomly selects one item in the list to play.
    ///         Never repeats the same item in the list twice.
    /// </summary>
    /// <param name="sfx"> The list to choose from. </param>
    void PlayRandom(List<AudioSource> sfxList)
    {
        int lastIndex = -1;
        
        if (sfxsToLastIndex.ContainsKey(sfxList))
            lastIndex = sfxsToLastIndex[sfxList];
        else
            sfxsToLastIndex.Add(sfxList, lastIndex);

        int random;
        do
            random = UnityEngine.Random.Range(0, sfxList.Count);
         while (lastIndex != random && sfxList.Count > 1);

        sfxList[random].Play();
        sfxsToLastIndex[sfxList] = random;
    }

    /// <summary> Plays audio for corresponding paper actions (grab, drop, snap, etc.). </summary>
    void HandlePaperAction(object sender, MovePaper.PaperActionEventArgs e)
    {
        if (ActionsToSFX.TryGetValue(e.actionType, out var sfx) && sfx != null)
            PlayRandom(sfx);

        if (logAudio)
            Debug.Log($"AudioManager: Handled paper action {e.actionType} {(sfx == null ? "" : $"and played sfx : {sfx}")}");
    }

    /// <summary> Plays audio for corresponding UI menu changes (currently none). </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void HandleMenuChange(object sender, MenuChangeEventArgs e)
    {
        if (OpenMenuToSFX.TryGetValue(e.newMenuType, out var sfx) && sfx != null)
            sfx.Play();

        if (logAudio)
            Debug.Log($"AudioManager: Handled game action {e.newMenuType}{(sfx == null ? "" : $"and played sfx : {sfx}")}");
    }

    /// <summary> Swaps music tracks based on the new game state (pause, in-game, etc.). </summary>
    void HandleGameStateChange(object sender, GameManager.GameStateChangeEventArgs e)
    {
        // Swaps and fades in and out the current tracks
        if (GameStateToMusic.TryGetValue(e.newState, out var newMusic) && newMusic != null)
        {
            StartCoroutine(LerpTrackVolume(currentMusic, mute: true));

            if (!stopMusicByMute)
                currentMusic.Play();

            currentMusic = newMusic;
            StartCoroutine(LerpTrackVolume(newMusic, mute: false));
        }

        if (logAudio)
            Debug.Log($"AudioManager: Handled game state change {(newMusic == null ? "" : $"and changed music track to : {newMusic}")}");
    }

    IEnumerator LerpTrackVolume(AudioSource track, bool mute) => LerpTrackVolume(track, fadeCurve, fadeSpeed, mute);
    IEnumerator LerpTrackVolume(AudioSource track, AnimationCurve curve, float speed, bool mute)
    {
        // Slide UI elements using lerp and an animation curve

        if (track == null)
        {
            Debug.LogWarning("Music track is null.");
            yield break;
        }

        float startingVolume = track.volume;
        float targetVolume = mute ? 0 : maxMusicVolume;

        float current = 0;

        while (current < 1)
        {
            current = Mathf.MoveTowards(current, 1, speed * Time.deltaTime);

            track.volume = Mathf.Lerp(startingVolume, targetVolume, curve.Evaluate(current));
            yield return null;
        }
    }

    /// <summary> Plays the corresponding SFX to a game action </summary>
    void HandleGameAction(object sender, GameManager.GameActionEventArgs e)
    {
        // I could potentially change this to a class containing a predicate and a sfx, where each sound can have its own condition of when to play, given certain circumstances, which could potentially be a bit less clunky of a solution
        switch (e.gameAction)
        {
            case GameAction.StartLevel:
            case GameAction.RestartLevel:
            case GameAction.LoadNextLevel:
                foreach (var audioSource in snap)
                    StartCoroutine(LerpTrackVolume(audioSource, mute: false));
            break;

            case GameAction.CompleteLevel:
                foreach (var audioSource in snap)
                    StartCoroutine(LerpTrackVolume(audioSource, mute: true));
            break;
        }

        if (GameActionToSFX.TryGetValue(e.gameAction, out var sfx) && sfx != null)
            sfx.Play();

        if (logAudio)
            Debug.Log($"AudioManager: Handled game action {e.gameAction}{(sfx == null ? "" : $"and played sfx : {sfx}")}");
    }

    /// <summary> Plays sfx for corresponding UI interactions. </summary>
    void HandleUIInteract(object sender, UIButton.UIInteractEventArgs e)
    {
        if (UIInteractToSFX.TryGetValue(e.buttonInteraction, out var sfx) && sfx != null)
            sfx.Play();

        if (logAudio)
            Debug.Log($"AudioManager: Handled UI interaction {e.buttonInteraction} {(sfx == null ? "" : $"and played sfx : {sfx}")}");

        // throw new Exception("Logger: Create logger script and check if logger.loggingObject.Contains(gameObject)");
    }
}

