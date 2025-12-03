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
    public Rigidbody rb;
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
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        SprintLinesP.SetActive(false);
        canJump = true;
        rb.freezeRotation = true;
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

        // Let Wallrunning script handle Gmultiplier - don't override it here
        if (!wallrunning.isTouchingWall && !isGrounded)
            wallrunning.Gmultiplier = 1f;

        // Handle crouch interpolation (using CapsuleCollider)
        if (LerpCrouch)
        {
            CrouchTimer += Time.deltaTime;
            float p = CrouchTimer / 1f;
            p *= p;
            CapsuleCollider col = GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.height = Mathf.Lerp(col.height, isCrouching ? 1f : 2f, p);
            }
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
            rb.AddForce(slideDirection * SlideForce, ForceMode.Force);
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

        float sprintSpeed = speed * (isSprinting ? SprintMultiplier : 1f);
        currentSpeed = sprintSpeed * (isCrouching ? CrouchMultiplier : 1f);
    }

    public void ProcessMove(Vector2 input)
    {
        // Only wallrun if touching wall and NOT grounded
        if (wallrunning.isTouchingWall && !isGrounded && wallrunning.wallNormal != Vector3.zero)
        {
            Vector3 moveDir = transform.TransformDirection(new Vector3(0, 0, input.y));
            Vector3 alongWall = Vector3.ProjectOnPlane(moveDir, wallrunning.wallNormal).normalized;
            Vector3 velocity = alongWall * currentSpeed;
            velocity.y = rb.velocity.y;
            velocity.y += gravity * wallrunning.Gmultiplier * Time.deltaTime;
            rb.velocity = velocity;
        }
        else
        {
            // Use normal movement if grounded or not wallrunning
            Vector3 moveDirection = transform.TransformDirection(new Vector3(input.x, 0, input.y));
            Vector3 velocity = moveDirection * currentSpeed;
            velocity.y = rb.velocity.y;
            velocity.y += gravity * wallrunning.Gmultiplier * Time.deltaTime;
            rb.velocity = velocity;
        }
    }

    public void Jump()
    {
        if (!canJump) return;
        rb.velocity = new Vector3(rb.velocity.x, Mathf.Sqrt(jumpHeight * -2f * gravity), rb.velocity.z);
        canJump = false;
        if (!isGrounded && wallrunning.isTouchingWall)
        {
            wallrunning.wallJumpUsed = true;
            wallrunning.hasWallJumped = true;
        }
    }

    public void Crouch()
    {
        isCrouching = !isCrouching;
        CrouchTimer = 0f;
        LerpCrouch = true;
        // Adjust player scale or collider height here if needed
    }

    public void Sprint()
    {
        speed *= SprintMultiplier;
    }

    public void DoFov(float endvalue)
    {
        MainCam.DOFieldOfView(endvalue, 0.25f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            canJump = true;
            wallrunning.hasWallJumped = false;
        }
    }
    
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            canJump = true;
        }
    }
    
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}