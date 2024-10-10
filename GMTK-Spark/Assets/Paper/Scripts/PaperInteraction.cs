using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PaperInteraction : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] bool disableCenters;
    [SerializeField] float edgeLeniency;
    [SerializeField] float speed;
    [SerializeField] AnimationCurve lerpCurve;

    [Header("Static Properties")]
    [SerializeField] LayerMask paperLayer;
    [SerializeField] Transform colliderCenter;
    [SerializeField] Transform transformCenter;
    [SerializeField] Transform polygonCenter;
    [SerializeField] Transform boundingBoxTopLeft;
    [SerializeField] Transform boundingBoxTopRight;
    [SerializeField] Transform boundingBoxBottomRight;
    [SerializeField] Transform boundingBoxBottomLeft;


    private void OnEnable()
        => MovePaper.PaperActionEventHandler += HandlePaperAction;

    private void OnDisable()
        => MovePaper.PaperActionEventHandler -= HandlePaperAction;

    Paper holdingPaper;
    bool IsHoldingPaper => holdingPaper != null; 

    void HandlePaperAction(object sender, MovePaper.PaperActionEventArgs e)
    {
        switch (e.actionType)
        { 
            case MovePaper.PaperActionEventArgs.PaperActionType.Grab:
                holdingPaper = e.paper;
                if (InteractionMath.IsCloseToEdge(holdingPaper.PolygonCollider2D, edgeLeniency, paperLayer))
                    StartCoroutine(InteractionMath.LerpColliderToCenter(holdingPaper.PolygonCollider2D, speed, lerpCurve));
            break;
            
            case MovePaper.PaperActionEventArgs.PaperActionType.StartSnap:
            case MovePaper.PaperActionEventArgs.PaperActionType.Snap:
            case MovePaper.PaperActionEventArgs.PaperActionType.Drop:
                holdingPaper = null;
            break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (disableCenters && !IsHoldingPaper)
        {
            this.colliderCenter.gameObject.SetActive(false);
            this.transformCenter.gameObject.SetActive(false);
            this.polygonCenter.gameObject.SetActive(false);
        }

        if (!IsHoldingPaper)
            return;

        Vector2 mousePosition = InteractionMath.GetMousePosition();

        this.polygonCenter.gameObject.SetActive(true);
        this.colliderCenter.gameObject.SetActive(true);
        this.transformCenter.gameObject.SetActive(true);

        //Vector2 polygonCenter = ((Vector2)mousePosition - colliderCenter) + (Vector2)mousePosition;
        Vector2 transformCenter = holdingPaper.transform.position;
        Vector2 colliderCenter = holdingPaper.PolygonCollider2D.bounds.center;
        Vector2 polygonCenter;

        var boundingBoxMax = holdingPaper.PolygonCollider2D.bounds.max;
        var boundingBoxMin = holdingPaper.PolygonCollider2D.bounds.min;

        polygonCenter = InteractionMath.GetCentroid(holdingPaper.PolygonCollider2D);

        this.polygonCenter.transform.position = polygonCenter;
        this.colliderCenter.transform.position = colliderCenter;
        this.transformCenter.transform.position = transformCenter;

        boundingBoxBottomLeft.transform.position = boundingBoxMin;
        boundingBoxTopRight.transform.position = boundingBoxMax;

        boundingBoxBottomRight.transform.position = new(boundingBoxMax.x, boundingBoxMin.y);
        boundingBoxTopLeft.transform.position = new(boundingBoxMin.x, boundingBoxMax.y);

        if (Input.GetKeyDown(KeyCode.Space))
            holdingPaper.transform.position = colliderCenter;

        //Debug.Log($"Polygon center is : {transformCenter} | Mouse Position is : {mousePosition} | Amount to move from center to mouse: ({InteractionMath.GetAmountToMoveBetweenPoints(transformCenter, mousePosition).x}, {InteractionMath.GetAmountToMoveBetweenPoints(transformCenter, mousePosition).y})");
    }
}