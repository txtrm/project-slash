using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using DG.Tweening;

public class PlayerMotor : MonoBehaviour
{
    [SerializeField] LayerMask layerMask;

    public ViewBobbing viewBobbing;
    public Wallrunning wallrunning;

    public Camera MainCam;
    public GameObject SprintLinesP;
    public ParticleSystem SprintLines;
    public CharacterController controller;
    public Vector3 slideDirection;

    private Vector3 playerVelocity;

    public float speed = 9f;
    public float currentSpeed;
    public float gravity = -9.8f;
    public float jumpHeight = 2f;
    public float SprintMultiplier = 1.2f;
    public float CrouchMultiplier = 0.5f;
    public float CrouchTimer = 0f;
    public float SlideForce = 10f;

    public bool isSprinting;
    public bool isGrounded;
    public bool canJump;
    public bool isCrouching;
    public bool LerpCrouch;

    private bool isTilted;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        SprintLinesP.SetActive(false);
        canJump = true;
    }

    void Update()
    {
        #region Raycasts

        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.left), out RaycastHit hitinfoL, 1f, layerMask) && !isGrounded && !isTilted)
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left) * 1f, Color.red);
            wallrunning.DoTilt(-15f);
            isTilted = true;
        }
        else
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left) * 1f, Color.green);
        }
        
        if (!Physics.Raycast(transform.position, transform.TransformDirection(Vector3.left), 1f, layerMask) &&
            !Physics.Raycast(transform.position, transform.TransformDirection(Vector3.right), 1f, layerMask) &&
            isTilted)
        {
            wallrunning.DoTilt(0f);
            isTilted = false;
        }

        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.right), out RaycastHit hitinfoR, 1f, layerMask) && !isGrounded && !isTilted)
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * 1f, Color.red);
            wallrunning.DoTilt(15f);
            isTilted = true;
        }
        else
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * 1f, Color.green);
        }

        #endregion         
        #region Visual

        if (canJump && !isGrounded)
            wallrunning.Gmultiplier = 0.2f;
        else
            wallrunning.Gmultiplier = 1f;

        // Handle crouch interpolation
        if (LerpCrouch)
        {
            CrouchTimer += Time.deltaTime;
            float p = CrouchTimer / 1f;
            p *= p;
            controller.height = Mathf.Lerp(controller.height, isCrouching ? 1f : 2f, p);

            if (p > 1f)
            {
                LerpCrouch = false;
                CrouchTimer = 0f;
            }
        }

        // Sprint visuals
        if (isSprinting && !isCrouching)
        {
            SprintLinesP.SetActive(true);
            if (!SprintLines.isPlaying) SprintLines.Play();
        }
        else if (SprintLines.isPlaying && !isSprinting)
        {
            SprintLines.Stop();
        }

        // Bobbing
        if (isGrounded)
            viewBobbing.Amount = isSprinting ? 0.08f : (isCrouching ? 0.03f : 0.05f);
        else
            viewBobbing.Amount = 0f;

        #endregion

        slideDirection = transform.forward;

        if (isSprinting && isCrouching)
        {
            SlideForce -= Time.deltaTime * 8f;
            controller.Move(slideDirection * SlideForce * Time.deltaTime);
            if (SlideForce <= 0f)
                SlideForce = 0f;
        }
        else
        {
            SlideForce = 15f;
        }

        if (isSprinting)
            DoFov(75f);
        else
            DoFov(60f);

        // Speed logic
        float sprintSpeed = speed * (isSprinting ? SprintMultiplier : 1f);
        currentSpeed = sprintSpeed * (isCrouching ? CrouchMultiplier : 1f);
    }

    public void ProcessMove(Vector2 input)
    {
        Vector3 moveDirection = new Vector3(input.x, 0, input.y);
        controller.Move(transform.TransformDirection(moveDirection) * currentSpeed * Time.deltaTime);

        // Apply gravity
        playerVelocity.y += gravity * Time.deltaTime * wallrunning.Gmultiplier;
        controller.Move(playerVelocity * Time.deltaTime);

        // Reset vertical velocity when grounded
        if (controller.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
            isGrounded = true;
            canJump = true;
            wallrunning.hasWallJumped = false; // reset wall jump ability when grounded
        }
        else
        {
            isGrounded = false;
        }
    }

    public void Jump()
    {
        if (!canJump) return;

        playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        canJump = false;

        // If this jump was from a wall, remember which one
        if (!isGrounded && wallrunning.lastWall != null)
        {
            wallrunning.hasWallJumped = true;
        }
    }

    public void Crouch()
    {
        isCrouching = !isCrouching;
        CrouchTimer = 0f;
        LerpCrouch = true;
    }

    public void Sprint()
    {
        speed *= SprintMultiplier;
    }

    public void DoFov(float endvalue)
    {
        MainCam.DOFieldOfView(endvalue, 0.25f);
    }
}
