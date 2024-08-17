using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Singleton<Player>
{
    [Header("Properties")]
    [SerializeField] float movementSpeed;

    [Header("Components")]
    [SerializeField] SpriteRenderer spriteRenderer;

    Robot robotToMove;
    public enum MovementType { FollowMouse, MoveRobot }

    public MovementType movementType;
    float horizontalInput;

    // Update is called once per frame
    void Update()
    {
        switch (movementType)
        {
            case MovementType.FollowMouse:
                spriteRenderer.enabled = true;
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                transform.position = mousePosition;
            break;

            case MovementType.MoveRobot:
                horizontalInput = Input.GetAxisRaw("Horizontal");
                robotToMove.Rigidbody.velocity = new Vector2(horizontalInput * movementSpeed, robotToMove.Rigidbody.velocity.y);
                break;
        }
    }

    public bool TryEnterRobot(Robot robot)
    {
        if (robotToMove != null)
            return false;

        robotToMove = robot;
        spriteRenderer.enabled = false;
        movementType = MovementType.MoveRobot;

        return true;
    }

    public bool TryExitRobot(Robot robot)
    {
        if (robot != robotToMove)
            return false;

        robotToMove = null;
        movementType = MovementType.FollowMouse;

        return true;
    }
}