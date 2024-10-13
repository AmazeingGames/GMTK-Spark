using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
public static class InteractionMath
{
    /// <summary>
    ///     Fires 8 raycasts around the click point to see if it's close to the edge.
    /// </summary>
    /// <param name="polygon"></param>
    /// <param name="mousePosition"></param>
    /// <param name="leniency"></param>
    /// <param name="objectLayer"></param>
    /// <returns> True if any raycasts around the click point don't hit the object. </returns>
    /// <exception cref="NotImplementedException"></exception>
    public static bool IsCloseToEdge(PolygonCollider2D polygon, float leniency, LayerMask objectLayer)
    {
        for (int i = 0; i < 3; i++)
        {
            float yLeniency = i switch
            {
                0 => -leniency,
                1 => 0,
                2 => leniency,
                _ => throw new NotImplementedException()
            };
            for (int n = 0; n < 3; n++)
            {
                float xLeniency;
                if (n == 0)
                    xLeniency = -leniency;
                else if (n == 2)
                    xLeniency = leniency;
                else
                    continue;

                var hits = Physics2D.RaycastAll((Vector2)GetMousePosition() + new Vector2(xLeniency, yLeniency), Vector3.forward, Mathf.Infinity, objectLayer);
                bool hitPolygon = false;

                foreach (var hit in hits)
                {
                    if (hit.collider == polygon)
                        hitPolygon = true;
                }

                if (!hitPolygon)
                    return true;
            }
        }
        Debug.Log("Is close to edge : False");
        return false;
    }

    /// <summary>
    ///     Lerps the polygon's transform position so the centroid lines up with the mouse position.
    ///     Lerps at a fixed speed, rather than fixed time value.
    /// </summary>
    /// <param name="polygon"></param>
    /// <param name="speed"> How fast to move the polygon. The higher the faster. </param>
    /// <param name="curve"> The animation curve used to fade speed smoothly. </param>
    public static IEnumerator LerpColliderToCenter(PolygonCollider2D polygon, float speed, AnimationCurve curve, bool stopOnMouseUp)
    {
        var mousePosition = (Vector2)GetMousePosition();

        var centroidPosition = GetCentroid(polygon);

        var amountToMove = GetAmountToMoveBetweenPoints(centroidPosition, GetMousePosition());
        var startPosition = (Vector2)polygon.transform.position;
        var goalPosition = startPosition + amountToMove;

        float distance = Vector3.Distance(startPosition, goalPosition);
        float remainingDistance = distance;
        while (GetTotalDistanceBetweenPoints(centroidPosition, mousePosition) > .1f)
        {
            if (Input.GetMouseButtonUp(0) && stopOnMouseUp)
                break;

            mousePosition = (Vector2)GetMousePosition();
            centroidPosition = GetCentroid(polygon);

            Debug.Log($"Distance between centroid ({centroidPosition.x}, {centroidPosition.y}) and mouse position ({mousePosition.x}, {mousePosition.y}): {GetTotalDistanceBetweenPoints(centroidPosition, mousePosition)}");

            amountToMove = GetAmountToMoveBetweenPoints(centroidPosition, mousePosition);
            goalPosition = (Vector2)polygon.transform.position + amountToMove;

            // current = Mathf.MoveTowards(current, 1, speed * Time.deltaTime);
            // polygon.transform.position = Vector3.Lerp(startPosition, goalPosition, curve.Evaluate(current));

            polygon.transform.position = Vector3.Lerp(startPosition, goalPosition, curve.Evaluate(1 - (remainingDistance / distance)));
            remainingDistance -= speed * Time.deltaTime;

            Debug.Log((1 - (remainingDistance / distance)));
            yield return null;
        }
        Debug.Log("Finished Lerp!");
    }

    /// <summary>
    ///     Gets the position of the mouse.
    /// </summary>
    /// <returns> The mouse position translated to a world point with an adjusted z value. </returns>
    public static Vector3 GetMousePosition()
    {
        var mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = -10;
        return mousePosition;
    }
    
    /// <summary>
    ///     Returns the total distance between two points.
    /// </summary>
    /// <param name="point1"> Point to find the distance from. </param>
    /// <param name="point2"> Point to find the distance to. </param>
    /// <returns> The total distance from point1 to point2. </returns>
    public static double GetTotalDistanceBetweenPoints(Vector2 point1, Vector2 point2)
        => Math.Sqrt(Math.Pow((point2.x - point1.x), 2) + Math.Pow((point2.y - point1.y), 2));

    /// <summary>
    ///     Returns the distance required to move from one point to another point.
    /// </summary>
    /// <param name="from"> The point we're moving from. </param>
    /// <param name="to"> The point we're moving to. </param>
    /// <returns> Vector of the distance required to move from one point to another point. </returns>
    public static Vector2 GetAmountToMoveBetweenPoints(Vector2 from, Vector2 to)
    {
        Vector2 amountToMove = new()
        {
            x = from.x - to.x,
            y = from.y - to.y
        };

        if (from.x > to.x)
            amountToMove.x = Mathf.Abs(amountToMove.x) * -1;
        else
            amountToMove.x = Mathf.Abs(amountToMove.x);

        if (from.y > to.y)
            amountToMove.y = Mathf.Abs(amountToMove.y) * -1;
        else
            amountToMove.y = Mathf.Abs(amountToMove.y);

        return amountToMove;
    }

    /// <summary>
    ///     Computes the centroid of a polygon while adjusting for world space. 
    ///     Does not work for a complex polygon.
    /// </summary>
    /// <param name="polygon"> Polygon collider to find the centroid of. </param>
    /// <returns> Centroid point, Vector2.zero, if something is wrong. </returns>
    public static Vector2 GetCentroid(PolygonCollider2D polygon)
    {
        float accumulatedArea = 0.0f;
        float centerX = 0.0f;
        float centerY = 0.0f;

        List<Vector2> worldSpacePoints = new();

        foreach (var point in polygon.points)
            worldSpacePoints.Add(polygon.transform.TransformPoint(point));

        for (int i = 0, j = worldSpacePoints.Count - 1; i < worldSpacePoints.Count; j = i++)
        {
            float temp = worldSpacePoints[i].x * worldSpacePoints[j].y - worldSpacePoints[j].x * worldSpacePoints[i].y;
            accumulatedArea += temp;
            centerX += (worldSpacePoints[i].x + worldSpacePoints[j].x) * temp;
            centerY += (worldSpacePoints[i].y + worldSpacePoints[j].y) * temp;
        }

        if (Math.Abs(accumulatedArea) < 1E-7f)
        {
            Debug.LogWarning("Avoided division by zero");
            return Vector2.zero;  // Avoid division by zero
        }

        accumulatedArea *= 3f;
        return new Vector2(centerX / accumulatedArea, centerY / accumulatedArea);
    }
}
