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
    // Track the normal of the wall we last used for a wall-jump
    public Vector3 lastWallNormal = Vector3.zero;
    // Angle (degrees) threshold to consider a contact as the "same" wall
    public float wallJumpResetAngle = 20f;
    // Track the exact GameObject we last used for a wall-jump. If the player
    // contacts the same GameObject again while still airborne, ignore it.
    public GameObject lastUsedWall = null;

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
            Vector3 contactNormal = collision.contacts[0].normal;

            // If we've recently used a wall-jump and we're still airborne, ignore
            // re-contact with the same wall GameObject.
            if (wallJumpUsed && lastUsedWall != null && !PM.isGrounded && collision.gameObject == lastUsedWall)
                return;

            // If we've recently used a wall-jump and this contact has a very
            // similar normal to the last jumped wall, treat it as the same wall
            // and ignore it (prevent reattaching and jumping again).
            if (wallJumpUsed && lastWallNormal != Vector3.zero &&
                Vector3.Angle(contactNormal, lastWallNormal) < wallJumpResetAngle)
            {
                // ignore this contact as it's effectively the same wall
                return;
            }

            isTouchingWall = true;
            Gmultiplier = 0.03f; // Reduce gravity for slower slide
            lastWall = collision.gameObject;
            wallNormal = contactNormal;
            // Only reset wallJumpUsed if this contact is a different wall than
            // the one we used for a wall-jump. This prevents re-enabling jumps
            // immediately when touching the same wall.
            if (lastUsedWall == null || collision.gameObject != lastUsedWall)
            {
                wallJumpUsed = false;
                lastWallNormal = contactNormal;
            }

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
            Vector3 contactNormal = collision.contacts[0].normal;

            // If we've recently used a wall-jump and we're still airborne, ignore
            // re-contact with the same wall GameObject.
            if (wallJumpUsed && lastUsedWall != null && !PM.isGrounded && collision.gameObject == lastUsedWall)
                return;

            // ignore same-wall contact if we already wall-jumped from it
            if (wallJumpUsed && lastWallNormal != Vector3.zero &&
                Vector3.Angle(contactNormal, lastWallNormal) < wallJumpResetAngle)
            {
                return;
            }

            isTouchingWall = true;
            Gmultiplier = 0.03f;
            lastWall = collision.gameObject;
            wallNormal = contactNormal;
            // Only reset wallJumpUsed if this is a different wall than lastUsedWall
            if (lastUsedWall == null || collision.gameObject != lastUsedWall)
            {
                lastWallNormal = contactNormal;
                wallJumpUsed = false;
            }
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

    // Reset wall-jump tracking state (used when landing)
    public void ResetWallJumpState()
    {
        wallJumpUsed = false;
        lastWallNormal = Vector3.zero;
        lastUsedWall = null;
    }
}
