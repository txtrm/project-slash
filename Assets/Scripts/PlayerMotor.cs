using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using DG.Tweening;
using System.Runtime.CompilerServices;
using UnityEditor.Experimental.GraphView;

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
    [Tooltip("Horizontal speed applied when jumping off a wall (push away from wall)")]
    public float wallJumpHorizontalSpeed = 6f;
    [Tooltip("Multiplier for vertical jump speed when wall-jumping")]
    public float wallJumpUpMultiplier = 1f;
    public float SprintMultiplier = 1.2f;
    public float CrouchMultiplier = 0.5f;
    public float CrouchTimer = 0f;

    // Sliding (Rigidbody-based)
    public bool sliding = false;
    public float slideDuration = 2f;
    public float slideImpulse = 6f; // initial impulse when sliding starts
    public float slideControlForce = 4f; // player input influence while sliding
    public float slideDrag = 0.2f; // drag while sliding (lower -> longer slide)
    private float originalDrag;
    private float slideStartTime = 0f;
    private CapsuleCollider capsule;
    private float originalCapsuleHeight = 2f;
    // Movement lock after jump so ProcessMove doesn't overwrite jump velocity
    public float movementLockDuration = 0.12f;
    private float movementLockTimer = 0f;

    public bool isSprinting;
    public bool isGrounded;
    public bool canJump;
    public bool isCrouching;
    public bool LerpCrouch;

    private bool isCleared;
    private bool isTilted;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        if (capsule != null) originalCapsuleHeight = capsule.height;
        originalDrag = rb.drag;
        Cursor.lockState = CursorLockMode.Locked;
        SprintLinesP.SetActive(false);
        canJump = true;
        rb.freezeRotation = true;
    }

    void Update()
    {
        // Update movement lock timer
        if (movementLockTimer > 0f)
            movementLockTimer -= Time.deltaTime;
        #region Raycasts

        float rayDist = 1f;
        Vector3 leftDir = transform.TransformDirection(Vector3.left);
        Vector3 rightDir = transform.TransformDirection(Vector3.right);

        bool leftHit = Physics.Raycast(transform.position, leftDir, out RaycastHit hitinfoL, rayDist, layerMask);
        bool rightHit = Physics.Raycast(transform.position, rightDir, out RaycastHit hitinfoR, rayDist, layerMask);

        // Draw rays once using the actual hit results
        Debug.DrawRay(transform.position, leftDir * rayDist, leftHit ? Color.red : Color.green);
        Debug.DrawRay(transform.position, rightDir * rayDist, rightHit ? Color.red : Color.green);

        // Tilt only when airborne; use stored hit results
        if (!isGrounded)
        {
            if (leftHit && !isTilted)
            {
                wallrunning.DoTilt(-15f);
                isTilted = true;
            }
            else if (rightHit && !isTilted)
            {
                wallrunning.DoTilt(15f);
                isTilted = true;
            }
            else if (!leftHit && !rightHit && isTilted)
            {
                wallrunning.DoTilt(0f);
                isTilted = false;
            }
        }
        else if (isTilted)
        {
            wallrunning.DoTilt(0f);
            isTilted = false;
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
            if (!isCleared)
            {
                SprintLines.Clear();
                isCleared = true;
            }
            SprintLinesP.SetActive(true);
            if (!SprintLines.isPlaying)
            {
                SprintLines.Play();
            }
                
        }
        else if (SprintLines.isPlaying && !isSprinting)
        {
            SprintLines.Stop();
            isCleared = false;
        }

        // Bobbing
        if (isGrounded)
            viewBobbing.Amount = isSprinting ? 0.08f : (isCrouching ? 0.03f : 0.05f);
        else
            viewBobbing.Amount = 0f;

        #endregion

        // Sliding lifecycle
        if (sliding)
        {
            // Stop sliding after duration
            if (Time.time - slideStartTime >= slideDuration)
            {
                StopSlide();
            }
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
        // If recently jumped, preserve current horizontal velocity and only apply gravity
        if (movementLockTimer > 0f)
        {
            Vector3 v = rb.velocity;
            v.y += gravity * wallrunning.Gmultiplier * Time.deltaTime;
            rb.velocity = v;
            return;
        }
        // Sliding: allow limited input influence but don't overwrite full velocity
        if (sliding)
        {
            // small player control while sliding (forward influence stronger)
            Vector3 control = transform.TransformDirection(new Vector3(input.x * 0.2f, 0f, input.y * 0.6f));
            rb.AddForce(control * slideControlForce, ForceMode.Acceleration);
            return;
        }

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
        // Prevent repeated wall-jumps on the same wall: if we've already used the wall jump
        // while still touching this wall, disallow another jump until we touch ground or a different wall.
        if (wallrunning != null && wallrunning.isTouchingWall && !isGrounded && wallrunning.wallJumpUsed)
        {
            return;
        }
        Vector3 dir = new Vector3(rb.velocity.x, Mathf.Sqrt(jumpHeight * -2f * gravity), rb.velocity.z);
        canJump = false;
        if (wallrunning != null && wallrunning.isTouchingWall && !isGrounded && wallrunning.wallNormal != Vector3.zero)
        {
            // Mark used
            wallrunning.wallJumpUsed = true;
            // remember the wall normal we jumped from so we can ignore re-contacts
            wallrunning.lastWallNormal = wallrunning.wallNormal;
            // remember the exact GameObject we jumped from so re-contacts
            // with the same object while airborne can be ignored
            wallrunning.lastUsedWall = wallrunning.lastWall;
            wallrunning.hasWallJumped = true;

            // Horizontal push directly away from the wall (plane normal points outward)
            Vector3 horizontalPush = wallrunning.wallNormal.normalized * wallJumpHorizontalSpeed;

            // Vertical speed from jumpHeight (optionally scaled)
            float verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity) * wallJumpUpMultiplier;

            // Combine into final velocity
            dir = new Vector3(horizontalPush.x, verticalSpeed, horizontalPush.z);
        }

        rb.velocity = dir;
        // lock movement for a short time so ProcessMove won't overwrite our horizontal jump impulse
        movementLockTimer = movementLockDuration;
        Debug.Log(rb.velocity);
        // If jumping while sliding, stop the slide
        if (sliding)
        {
            StopSlide();
        }
    }

    public void Crouch()
    {
        // If player is sprinting and grounded, start a slide instead of regular crouch
        if (isSprinting && isGrounded && !sliding)
        {
            StartSlide();
            return;
        }

        isCrouching = !isCrouching;
        CrouchTimer = 0f;
        LerpCrouch = true;
    }

    private void StartSlide()
    {
        sliding = true;
        slideStartTime = Time.time;
        isCrouching = true;
        LerpCrouch = false; // snap to crouch height immediately
        if (capsule != null) capsule.height = originalCapsuleHeight * 0.5f;
        originalDrag = rb.drag;
        rb.AddForce(transform.forward * slideImpulse, ForceMode.VelocityChange);
        rb.drag = slideDrag;
        // optional: play sprint/slide visuals
        if (SprintLines != null && !SprintLines.isPlaying) SprintLines.Play();
    }

    private void StopSlide()
    {
        sliding = false;
        rb.drag = originalDrag;
        if (capsule != null) capsule.height = originalCapsuleHeight;
        isCrouching = false;
        LerpCrouch = true; // lerp back to standing
        // stop sprint visuals if not sprinting
        if (SprintLines != null && SprintLines.isPlaying && !isSprinting) SprintLines.Stop();
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
            if (wallrunning != null)
            {
                wallrunning.ResetWallJumpState();
                wallrunning.hasWallJumped = false;
            }
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