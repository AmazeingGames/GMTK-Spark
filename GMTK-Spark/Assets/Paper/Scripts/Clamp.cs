using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Clamp
{
    public static void CalculateBounds(PolygonCollider2D collider, out float objectWidth, out float objectHeight, out Vector2 screenBounds)
    {
        screenBounds = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z));

        objectWidth = collider.bounds.extents.x;
        objectHeight = collider.bounds.extents.y;
    }

    public static void CalculateBounds(SpriteRenderer spriteRenderer, out float objectWidth, out float objectHeight, out Vector2 screenBounds)
    {
        screenBounds = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z));

        objectWidth = spriteRenderer.bounds.extents.x; //extents = size of width / 2
        objectHeight = spriteRenderer.bounds.extents.y; //extents = size of height / 2    
    }

    public static void ClampToScreenPerspective(Transform transform, float width, float height, Vector2 screenBounds)
    {
        Vector3 viewPos = transform.position;
        viewPos.x = Mathf.Clamp(viewPos.x, screenBounds.x + width, screenBounds.x * -1 - width);
        viewPos.y = Mathf.Clamp(viewPos.y, screenBounds.y + height, screenBounds.y * -1 - height);
        transform.position = viewPos;
    }

    public static void ClampToScreenOrthographic(Transform transform, float width, float height, Vector2 screenBounds)
    {
        Vector3 viewPos = transform.position;
        viewPos.x = Mathf.Clamp(viewPos.x, screenBounds.x * -1 + width, screenBounds.x - width);
        viewPos.y = Mathf.Clamp(viewPos.y, screenBounds.y * -1 + height, screenBounds.y - height);
        transform.position = viewPos;
    }
}
