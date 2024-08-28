using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] GameObject puzzleParent;

    readonly List<Paper> paperList = new();

    private void OnEnable()
        => MovePaper.PaperAction += HandlePaperAction;

    private void OnDisable()
        => MovePaper.PaperAction -= HandlePaperAction;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < puzzleParent.transform.childCount; i++)
            paperList.Add(puzzleParent.transform.GetChild(i).GetComponent<Paper>());
    }

    void HandlePaperAction(object sender, MovePaper.PaperActionEventArgs e)
    {
        if (e.actionType != MovePaper.PaperActionEventArgs.PaperActionType.Snap)
            return;

        foreach (var paper in paperList)
        {
            if (!paper.IsInPlace)
                return;
        }

        Win();
    }

    void Win()
        => AudioManager.Instance.PlayWinSound();
}
