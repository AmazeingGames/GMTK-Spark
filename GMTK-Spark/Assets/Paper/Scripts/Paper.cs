using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using static UnityEditor.Timeline.TimelinePlaybackControls;

public class Paper : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] PolygonCollider2D polygonCollider;
    [field: SerializeField] public PaperType Type { get; private set; }

    public enum PaperType { Null, Sunset, Portrait  };
    Vector2 screenBounds;
    float objectWidth;
    float objectHeight;
    bool isHolding;
    private void OnMouseDown()
    {
        if (isHolding)
            return;

        isHolding = MovePaper.Instance.TryGrabPaper(this);
    }

    private void Start()
    {
        if (polygonCollider == null)
            polygonCollider = GetComponent<PolygonCollider2D>();

        screenBounds = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z));
        //objectWidth = transform.GetComponent<SpriteRenderer>().bounds.extents.x; //extents = size of width / 2
        //objectHeight = transform.GetComponent<SpriteRenderer>().bounds.extents.y; //extents = size of height / 2    

        objectWidth = polygonCollider.bounds.extents.x;
        objectHeight = polygonCollider.bounds.extents.y;
    }

    private void OnMouseUp()
    {
        if (!isHolding)
            return;

        isHolding = !MovePaper.Instance.TryDropPaper(this);
    }

    private void Update()
    {

        if (!isHolding)
            return;

        ClampToScreenOrthographic();
    }

    void ClampToScreenPerspective()
    {
        Vector3 viewPos = transform.position;
        viewPos.x = Mathf.Clamp(viewPos.x, screenBounds.x + objectWidth, screenBounds.x * -1 - objectWidth);
        viewPos.y = Mathf.Clamp(viewPos.y, screenBounds.y + objectHeight, screenBounds.y * -1 - objectHeight);
        transform.position = viewPos;
    }

    void ClampToScreenOrthographic()
    {
        Vector3 viewPos = transform.position;
        viewPos.x = Mathf.Clamp(viewPos.x, screenBounds.x * -1 + objectWidth, screenBounds.x - objectWidth);
        viewPos.y = Mathf.Clamp(viewPos.y, screenBounds.y * -1 + objectHeight, screenBounds.y - objectHeight);
        transform.position = viewPos;
    }
}
