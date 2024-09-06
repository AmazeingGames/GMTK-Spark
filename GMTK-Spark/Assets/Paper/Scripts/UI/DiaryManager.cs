using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DiaryManager : MonoBehaviour
{
    [Header("Diary Entries")]
    [SerializeField] TextMeshProUGUI page1;
    [SerializeField] TextMeshProUGUI page2;
    [SerializeField] TextMeshProUGUI page3;
    [SerializeField] TextMeshProUGUI page4;
    [SerializeField] TextMeshProUGUI page5;

    int lastLoadedLevel = -1;
    List<TextMeshProUGUI> DiaryTexts;

    private void OnEnable()
    {
        GameManager.GameStateChangeEventHandler += HandleGameStateChange;
        MenuManager.MenuChangeEventHandler += HandleMenuChange;
    }
    private void OnDisable()
    {
        GameManager.GameStateChangeEventHandler -= HandleGameStateChange;
        MenuManager.MenuChangeEventHandler -= HandleMenuChange;
    }
    private void Start()
    {
        DiaryTexts = new()
        {
            { page1 },
            { page2 },
            { page3 },
            { page4 },
            { page5 },
        };
    }

    void HandleGameStateChange(object sender, GameManager.GameStateChangeEventArgs e)
    {
        if (e.levelToLoad != -1)
            lastLoadedLevel = e.levelToLoad;
    }

    void HandleMenuChange(object sender, MenuManager.MenuChangeEventArgs e)
    {
        if (e.newMenuType == MenuManager.MenuTypes.Diary)
        {
            foreach(var text in DiaryTexts) 
                text.gameObject.SetActive(false);
            DiaryTexts[lastLoadedLevel - 1].gameObject.SetActive(true);
        }
    }
}
