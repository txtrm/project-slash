using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Wallrunning : MonoBehaviour
{
    public PlayerMotor PM;
    public Transform tiltHolder;
    public GameObject lastWall;
    public Vector3 wallNormal;

    public bool hasWallJumped;
    public bool isTouchingWall;
    public bool wallJumpUsed = false; // Track if jump was already used on current wall

    public float Gmultiplier = 1f;

    void Start()
    {
        lastWall = null;
    }

    void Update()
    {
        if (!isTouchingWall)
            Gmultiplier = 1f;
    }

    void LateUpdate()
    {
        if (!isTouchingWall)
            Gmultiplier = 1f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            PM.isGrounded = true;
            PM.canJump = true;
            hasWallJumped = false;
            lastWall = null;
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            isTouchingWall = true;
            Gmultiplier = 0.03f; // Reduce gravity for slower slide
            lastWall = collision.gameObject;
            wallNormal = collision.contacts[0].normal;
            // Reset wallJumpUsed when touching a new wall
            wallJumpUsed = false;
            if (!PM.isGrounded && (!hasWallJumped || collision.gameObject != lastWall))
            {
                PM.canJump = true;
                hasWallJumped = false;
            }
        }
    }
    
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            isTouchingWall = true;
            Gmultiplier = 0.03f;
            lastWall = collision.gameObject;
            wallNormal = collision.contacts[0].normal;
            // Allow jump only if not already used on this wall
            if (!PM.isGrounded && !wallJumpUsed)
            {
                PM.canJump = true;
            }
        }
    }
    
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            isTouchingWall = false;
            Gmultiplier = 1f;
            wallNormal = Vector3.zero;
            wallJumpUsed = false; // Reset so next wall allows jumping again
        }
    }
    
    public void DoTilt(float zTilt)
    {
        if (tiltHolder == null) return;

        tiltHolder.DOKill();

        // Only rotate around Z
        Vector3 current = tiltHolder.localEulerAngles;
        tiltHolder.DOLocalRotate(new Vector3(current.x, current.y, zTilt), 0.25f)
            .SetEase(Ease.InOutSine);
    }
}
