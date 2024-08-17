using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePaper : Singleton<MovePaper>
{
    [SerializeField] Transform dragParent;
    [SerializeField] Transform sunsetParent;
    [SerializeField] Transform portraitParent;
    [SerializeField] bool worldPositionStays;
    [SerializeField] float rotationSpeed;
    [SerializeField] Space space;
    Paper holdingPaper;

    // Update is called once per frame
    void Update()
    {
        dragParent.transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.mouseScrollDelta.y != 0)
        {
            dragParent.transform.Rotate(Input.mouseScrollDelta.y * rotationSpeed * Time.deltaTime * Vector3.forward, space);
        }
    }

    public bool TryGrabPaper(Paper paper)
    {
        if (holdingPaper != null)
            return false;

        holdingPaper = paper;
        holdingPaper.transform.SetParent(dragParent, worldPositionStays);

        return true;
    }

    public bool TryDropPaper(Paper paper)
    {
        if (holdingPaper != paper)
            return false;

        Transform parent = holdingPaper.Type switch
        {
            Paper.PaperType.Portrait => portraitParent,
            Paper.PaperType.Sunset => sunsetParent,
            _ => null
        };

        holdingPaper.transform.SetParent(parent, worldPositionStays);
        holdingPaper = null;

        return true;
    }
}
