using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MovePaper.PaperActionEventArgs;

public class AudioManager : Singleton<AudioManager>
{
    [Header("Paper")]
    [SerializeField] AudioSource dropPaper;
    [SerializeField] AudioSource shuffle;
    [SerializeField] AudioSource pickUp;
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

    private void OnEnable()
    {
        MovePaper.PaperAction += HandlePaperAction;
    }

    void PlaySound(PaperSounds paperSounds)
    {
        switch (paperSounds)
        {
            case PaperSounds.Drop:
                dropPaper.Play();
            break;

            case PaperSounds.Shuffle:
                shuffle.Play();
            break;

            case PaperSounds.Grab:
                pickUp.Play();
            break;

            case PaperSounds.Snap:
                snap.Play();
            break;
        }
    }

    void PlaySound(UISounds uiSounds)
    {

    }

    void PlaySound(GameState gameSound)
    {

    }

    void HandlePaperAction(object sender, MovePaper.PaperActionEventArgs e)
    {
        switch (e.ActionType)
        {
            case PaperActionType.Grab:
                PlaySound(PaperSounds.Grab);
            break;

            case PaperActionType.Drop:
                PlaySound(PaperSounds.Drop);
            break;

            case PaperActionType.Snap:
                PlaySound(PaperSounds.Snap);
            break;
        }
    }
}
