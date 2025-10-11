using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Wallrunning : MonoBehaviour
{
    public PlayerMotor PM;
    public Transform tiltHolder;
    public GameObject lastWall;

    public bool hasWallJumped;
    public bool isTouchingWall;

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
        isTouchingWall = false;    
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Ground"))
        {
            PM.isGrounded = true;
            PM.canJump = true;
            hasWallJumped = false;
            lastWall = null;
        }
        
        if (hit.gameObject.CompareTag("Wall"))
        {
            isTouchingWall = true;
            Gmultiplier = 0.2f;
            // Check if we touched a *different* wall
            if (!PM.isGrounded && (!hasWallJumped || hit.gameObject != lastWall))
            {
                PM.canJump = true;
                hasWallJumped = false; // reset so next wall jump is allowed
            }
        }

        lastWall = hit.gameObject;
        isTouchingWall = false;
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
