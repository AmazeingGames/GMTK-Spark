using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using static GameManager;
using static MovePaper;
using static MovePaper.PaperActionEventArgs;
using static ScreenTransitions;
using static UIButton;
using System.Data;
using UnityEditor;
using UnityEngine.Timeline;
using UnityEngine.UIElements;


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
    [SerializeField] bool debugLog;

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
            { GameAction.StartLevel,       shuffle      },
            { GameAction.LoadNextLevel,     shuffle     },
            { GameAction.RestartLevel,     null         },
            { GameAction.CompleteLevel,    beatLevel    },
            { GameAction.EnterMainMenu,    null         },
            { GameAction.BeatGame,         null         },
        };

        GameStateToMusic = new()
        {
            { GameState.InMenu,   mainMenuMusic },
            { GameState.Running,   gameplayMusic },
            { GameState.Paused,   pauseMenuMusic }
        };

        OpenMenuToSFX = new()
        {
            { MenuManager.MenuTypes.Diary, openDiary },
        };

    }

    /// <summary>
    ///     Given a list of random audio sources, randomly selects one item in the list to play.
    ///     Never repeats the same item in the list twice.
    /// </summary>
    /// <param name="sfx"> The list containing the sfx we would like to play. </param>
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

    /// <summary>
    ///     Plays audio for corresponding paper actions
    /// </summary>
    void HandlePaperAction(object sender, MovePaper.PaperActionEventArgs e)
    {
        if (ActionsToSFX.TryGetValue(e.actionType, out var sfx) && sfx != null)
            PlayRandom(sfx);

        if (debugLog)
            Debug.Log($"AudioManager: Handled paper action {e.actionType} {(sfx == null ? "" : $"and played sfx : {sfx}")}");
    }

    void HandleMenuChange(object sender, MenuManager.MenuChangeEventArgs e)
    {
        if (OpenMenuToSFX.TryGetValue(e.newMenuType, out var sfx) && sfx != null)
            sfx.Play();

        if (debugLog)
            Debug.Log($"AudioManager: Handled game action {e.newMenuType}{(sfx == null ? "" : $"and played sfx : {sfx}")}");
    }

    /// <summary>
    ///     Plays sfx for corresponding game state change.
    /// </summary>
    void HandleGameStateChange(object sender, GameManager.GameStateChangeEventArgs e)
    {
        if (GameStateToMusic.TryGetValue(e.newState, out var music) && music != null)
        {
            if (currentMusic != null)
                StartCoroutine(LerpTrackVolume(currentMusic, fadeCurve, fadeSpeed, true, true));
            if (!stopMusicByMute)
                currentMusic.Play();
            currentMusic = music;
            StartCoroutine(LerpTrackVolume(currentMusic, fadeCurve, fadeSpeed, false, true));
        }

        if (debugLog)
            Debug.Log($"AudioManager: Handled game state change {(music == null ? "" : $"and changed music track to : {music}")}");
    }

    bool isFading = false;
    IEnumerator LerpTrackVolume(AudioSource track, AnimationCurve curve, float speed, bool isMuting, bool waitUntilFade)
    {
        // Slide UI elements using lerp and an animation curve

        float startingVolume = track.volume;
        float targetVolume = isMuting ? 0 : maxMusicVolume;

        float current = 0;

        while (current < 1)
        {
            isFading = true;
            current = Mathf.MoveTowards(current, 1, speed * Time.deltaTime);

            track.volume = Mathf.Lerp(startingVolume, targetVolume, curve.Evaluate(current));
            yield return null;
        }
        isFading = false;
    }

    /// <summary>
    ///     Plays a SFX corresponding to a game action
    /// </summary>
    void HandleGameAction(object sender, GameManager.GameActionEventArgs e)
    {
        if (GameActionToSFX.TryGetValue(e.gameAction, out var sfx) && sfx != null)
            sfx.Play();

        if (debugLog)
            Debug.Log($"AudioManager: Handled game action {e.gameAction}{(sfx == null ? "" : $"and played sfx : {sfx}")}");
    }

    /// <summary>
    ///     Plays sfx for corresponding UI interactions
    /// </summary>
    void HandleUIInteract(object sender, UIButton.UIInteractEventArgs e)
    {
        if (UIInteractToSFX.TryGetValue(e.buttonInteraction, out var sfx) && sfx != null)
            sfx.Play();

        if (debugLog)
            Debug.Log($"AudioManager: Handled UI interaction {e.buttonInteraction} {(sfx == null ? "" : $"and played sfx : {sfx}")}");

        // throw new Exception("Logger: Create logger script and check if logger.loggingObject.Contains(gameObject)");
    }
}

