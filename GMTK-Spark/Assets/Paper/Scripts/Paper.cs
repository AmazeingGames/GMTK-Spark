using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Paper : MonoBehaviour
{
    [field: SerializeField] public SpriteRenderer SpriteRenderer { get; private set; }
    [SerializeField] PolygonCollider2D polygonCollider;

    public enum PaperType { Null, Sunset, Portrait  };
    bool isHolding;

    public bool IsInPlace { get; private set; } = false;

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

        if (SpriteRenderer == null)
            SpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnMouseUp()
    {
        if (!isHolding)
            return;

        isHolding = !MovePaper.Instance.TryDropPaper(this);
    }

    public void SetIsInPlace(bool inPlace)
        => IsInPlace = inPlace;
}
