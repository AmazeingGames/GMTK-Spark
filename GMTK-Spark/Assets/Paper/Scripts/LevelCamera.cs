using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Potentially I could just remove the audio listener component
// If any audio problems come up I can make a single universal audio listener
public class LevelCamera : MonoBehaviour
{
    [SerializeField] Camera levelCamera;
    [SerializeField] AudioListener levelAudioListener;

    private void OnEnable()
    {
        MenuManager.MenuChangeEventHandler += HandleMenuChange;
    }

    private void OnDisable()
    {
        MenuManager.MenuChangeEventHandler -= HandleMenuChange;
    }

    void HandleMenuChange(object sender, MenuManager.MenuChangeEventArgs e)
    {
        levelCamera.enabled = !e.isAMenuEnabled;
        levelAudioListener.enabled = !e.isAMenuEnabled;
    }
}
