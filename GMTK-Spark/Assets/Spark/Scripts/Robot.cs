using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Robot : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] Color onColor;
    [SerializeField] Color offColor;

    [field: Header("Components")]   
    [field: SerializeField] public Rigidbody2D Rigidbody { get; private set; }
    [SerializeField] SpriteRenderer spriteRenderer;

    bool isActivated;


    // Update is called once per frame
    void Update()
    {
        if (!isActivated)
            return;

        if (Input.GetMouseButtonUp(1))
        {
            isActivated = !Player.Instance.TryExitRobot(this);
            spriteRenderer.color = isActivated ? onColor : offColor;
        }
    }

    private void OnMouseOver()
    {
        if (isActivated)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            isActivated = Player.Instance.TryEnterRobot(this);
            spriteRenderer.color = isActivated ? onColor : offColor;
        }


    }
}
