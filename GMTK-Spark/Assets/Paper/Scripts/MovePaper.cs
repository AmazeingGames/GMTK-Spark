using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePaper : Singleton<MovePaper>
{
    [SerializeField] Transform dragParent;
    [SerializeField] bool worldPositionStays;
    [SerializeField] float rotationSpeed;
    [SerializeField] Space space;

    [Header("Snap Properties")]
    [SerializeField] float positionalLeniency;
    [SerializeField] Vector2 rotationalLeniency;

    int order = 0;

    Paper holdingPaper;
    Transform rememberParent;
    SpriteRenderer dragParentSpriteRenderer;

    private void Start()
    {
        dragParentSpriteRenderer = dragParent.GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        dragParent.transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.mouseScrollDelta.y != 0)
        {
            dragParent.transform.Rotate(Input.mouseScrollDelta.y * rotationSpeed * Time.deltaTime * Vector3.forward, space);
        }

        Clamp.CalculateBounds(dragParentSpriteRenderer, out float width, out float height, out Vector2 screenBounds);
        Clamp.ClampToScreenOrthographic(dragParent, width, height, screenBounds);

        SnapPaper();
        DebugPaper();
    }

    void DebugPaper()
    {
        if (holdingPaper == null)
            return;

        Quaternion rotation = holdingPaper.transform.rotation;

        Debug.Log($"Rotation is : (z){rotation.z} | (w){rotation.w}");
    }

    void SnapPaper()
    {
        if (holdingPaper == null)
            return;

        var position = holdingPaper.transform.localPosition;

        bool isXClose = ((position.x >= 0 && position.x <= positionalLeniency) || (position.x <= 0 && position.x > -positionalLeniency));
        bool isYClose = ((position.y > 0 && position.y <= positionalLeniency) || (position.y <= 0 && position.y > -positionalLeniency));

        Quaternion rotation = holdingPaper.transform.rotation;

        bool isZRotationClose = ((rotation.z > 0 && rotation.z <= positionalLeniency) || (rotation.z <= 0 && rotation.z > -positionalLeniency));
        bool isWRotationClose = ((rotation.w > 1 && rotation.w <= positionalLeniency) || (rotation.w <= 1 && rotation.w > -positionalLeniency));

        
        if (isXClose && isYClose && isZRotationClose && isWRotationClose)
            holdingPaper.transform.SetPositionAndRotation(Vector2.zero, new Quaternion(rotation.x, rotation.y, 0, 1));
    }

    public bool TryGrabPaper(Paper paper)
    {
        if (holdingPaper != null)
            return false;

        holdingPaper = paper;
        rememberParent = holdingPaper.transform.parent;
        holdingPaper.transform.SetParent(dragParent, worldPositionStays);
        holdingPaper.SpriteRenderer.sortingOrder = order++;

        return true;
    }

    public bool TryDropPaper(Paper paper)
    {
        if (holdingPaper != paper)
            return false;

        holdingPaper.transform.SetParent(rememberParent, worldPositionStays);
        SnapPaper();
        holdingPaper = null;
        return true;
    }
}
