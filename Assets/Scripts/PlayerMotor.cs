using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using DG.Tweening;
using System.Runtime.CompilerServices;
using UnityEditor.Experimental.GraphView;
using TMPro;

public class PlayerMotor : MonoBehaviour
{

    
        [Header("References")] [Space(2)]
        [SerializeField] LayerMask layerMask;
        public ViewBobbing viewBobbing;
        public Wallrunning wallrunning;
        public Camera MainCam;
        public GameObject SprintLinesP;
        public ParticleSystem SprintLines;
        public Rigidbody rb;
        public Vector3 slideDirection;
        public TMP_Text speedText;

        private Vector3 playerVelocity;
        // Target horizontal velocity computed from input; applied in FixedUpdate via MovePosition
        private Vector3 targetHorizontalVelocity = Vector3.zero;
        private Vector2 cachedInput = Vector2.zero;
        // Whether horizontal MovePosition logic is enabled. Disabled during physics-driven sliding.
        // private bool movePositionEnabled = true;

        [Space(6)] [Header("Movement")]
        public float speed = 6f;
        public float currentSpeed;
        public float SprintMultiplier = 1.2f;
        public float CrouchMultiplier = 0.5f;
        public float CrouchTimer = 0f;

        [Space(6)] [Header("Jumping & Gravity")]
        public float gravity = -9.8f;
        public float jumpHeight = 2f;
        [Tooltip("Horizontal speed applied when jumping off a wall (push away from wall)")]
        public float wallJumpHorizontalSpeed = 6f;
        [Tooltip("Multiplier for vertical jump speed when wall-jumping")]
        public float wallJumpUpMultiplier = 1f;

        [Space(6)] [Header("Sliding")]
        // Sliding (Rigidbody-based)
        public bool sliding = false;
        public float slideDuration = 0.45f;
        public float slideImpulse = 32f; // initial impulse when sliding starts
        public float slideControlForce = 4f; // player input influence while sliding
        public float slideDrag = 0f; // drag while sliding (lower -> longer slide)
        [Tooltip("Horizontal velocity multiplier applied when stopping a slide (0 = zero horizontal momentum)")]
        public float slideStopMultiplier = 0.001f;
        [Tooltip("Duration over which horizontal velocity is damped when stopping a slide")]
        public float slideStopDuration = 0.08f;
        private Coroutine slideStopCoroutine = null;
        private float originalDrag;
        private float slideStartTime = 0f;
        private CapsuleCollider capsule;
        private float originalCapsuleHeight = 2f;

        [Space(6)] [Header("Control & Tuning")]
        // Movement lock after jump so ProcessMove doesn't overwrite jump velocity
        public float movementLockDuration = 0.2f;
        private float movementLockTimer = 0f;
        private float currentFovTarget = -1f;

        [Tooltip("How much control player has while airborne (0 = no control, 1 = full control)")]
        public float airControlMultiplier = 0.6f;

        [Tooltip("Acceleration applied when trying to change horizontal velocity (ground)")]
        public float groundAccel = 50f;
        [Tooltip("Acceleration applied when trying to change horizontal velocity (air)")]
        public float airAccel = 8f;

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
        speed = 6f;
        slideStopMultiplier = 0.001f;
        SprintMultiplier = 1.5f;
        slideDuration = 0.45f;
        gravity = -25f;
        jumpHeight = 3.3f;
        slideStopDuration = 0.08f;
        slideImpulse = 32f;
        wallJumpHorizontalSpeed = 10f;
        slideDrag = 0f;
    }

    void Update()
    {
        UpdateSpeedUI();

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

    void UpdateSpeedUI()
    {
        if (speedText == null || rb == null) return;
        float horizSpeed = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;
        speedText.text = $"{horizSpeed:0.0} m/s";
    }

    public void ProcessMove(Vector2 input)
    {
        // Cache the input; actual movement applied in FixedUpdate via MovePosition
        cachedInput = input;

        // If recently jumped, preserve current horizontal velocity and only apply gravity
        if (movementLockTimer > 0f)
        {
            targetHorizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            return;
        }

        // Sliding still uses forces (keep existing behavior)
        if (sliding)
        {
            // Player has limited control while sliding; add input-based influence
            Vector3 control = transform.TransformDirection(new Vector3(input.x * 0.2f, 0f, input.y * 0.6f));
            rb.AddForce(control * slideControlForce, ForceMode.Acceleration);
            // keep targetHorizontalVelocity at current horizontal velocity so MovePosition doesn't fight physics
            targetHorizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            return;
        }

        // Compute current horizontal velocity
        Vector3 horizVel = rb.velocity;
        horizVel.y = 0f;

        // Desired horizontal from input
        Vector3 desiredInput = new Vector3(input.x, 0f, input.y);
        float inputMag = desiredInput.magnitude;

        // Wallrunning: compute desired velocity along wall plane
        if (wallrunning.isTouchingWall && !isGrounded && wallrunning.wallNormal != Vector3.zero)
        {
            Vector3 moveDir = transform.TransformDirection(new Vector3(0, 0, input.y));
            Vector3 alongWall = Vector3.ProjectOnPlane(moveDir, wallrunning.wallNormal);
            Vector3 desiredHoriz = (alongWall.sqrMagnitude > 0.001f) ? alongWall.normalized * (inputMag > 0.01f ? currentSpeed : horizVel.magnitude) : horizVel;

            // Interpolate towards desired (instant on ground, partial in air)
            float t = isGrounded ? 1f : airControlMultiplier;
            targetHorizontalVelocity = Vector3.Lerp(horizVel, desiredHoriz, t);
            return;
        }

        // Normal movement path
        Vector3 desiredMoveWorld = transform.TransformDirection(desiredInput);
        Vector3 desiredHorizontal = (desiredMoveWorld.sqrMagnitude > 0.001f) ? desiredMoveWorld.normalized * (inputMag > 0.01f ? currentSpeed : horizVel.magnitude) : horizVel;

        float interp = isGrounded ? 1f : airControlMultiplier;
        targetHorizontalVelocity = Vector3.Lerp(horizVel, desiredHorizontal, interp);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // If sliding, let forces handle movement; still apply gravity manually
        if (sliding)
        {
            Vector3 vSlide = rb.velocity;
            vSlide.y += gravity * wallrunning.Gmultiplier * dt;
            rb.velocity = vSlide;
            // update view bobbing from actual horizontal movement
            Vector3 horizMove = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            viewBobbing.moveAmount = horizMove.magnitude;
            return;
        }

        // Apply horizontal movement via MovePosition
        Vector3 oldPos = rb.position;
        Vector3 displacement = targetHorizontalVelocity * dt;
        Vector3 newPos = oldPos + displacement;
        rb.MovePosition(newPos);

        // Calculate actual horizontal velocity achieved (accounts for collisions)
        Vector3 actualHoriz = (rb.position - oldPos) / dt;
        actualHoriz.y = 0f;

        // Apply gravity to vertical velocity manually
        float vert = rb.velocity.y;
        vert += gravity * wallrunning.Gmultiplier * dt;

        // Set the Rigidbody velocity: keep horizontal from MovePosition, set vertical from gravity
        rb.velocity = new Vector3(actualHoriz.x, vert, actualHoriz.z);

        // Update view bobbing from actual horizontal movement magnitude
        viewBobbing.moveAmount = actualHoriz.magnitude;
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
            StopSlideImmediate();
        }
    }
#region Crouching and Sliding

    public void Crouch()
    {
        // Don't allow initiating crouch or slide while in the air
        if (!isGrounded) return;

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
        // Safety: only start a slide if grounded
        if (!isGrounded) return;

        sliding = true;
        // remember initial slide direction for consistent physics handling
        Vector3 horiz = rb.velocity;
        horiz.y = 0f;
        if (horiz.sqrMagnitude > 0.01f)
            slideDirection = horiz.normalized;
        else
            slideDirection = transform.forward;
        slideStartTime = Time.time;
        isCrouching = true;
        LerpCrouch = false; // snap to crouch height immediately
        if (capsule != null) capsule.height = originalCapsuleHeight * 0.5f;
        originalDrag = rb.drag;
        // disable MovePosition while sliding so physics (velocity) control movement
        // movePositionEnabled = false;
        // ensure slide impulse is applied immediately and replace small existing horizontal vel
        Vector3 v = rb.velocity;
        v.x = slideDirection.x * slideImpulse;
        v.z = slideDirection.z * slideImpulse;
        rb.velocity = v;
        rb.drag = slideDrag;
        // optional: play sprint/slide visuals
        if (SprintLines != null && !SprintLines.isPlaying) SprintLines.Play();
    }

    private void StopSlide()
    {
        // Smoothly damp horizontal velocity over slideStopDuration
        if (slideStopCoroutine != null)
            StopCoroutine(slideStopCoroutine);
        slideStopCoroutine = StartCoroutine(SmoothStopSlide());
    }

    // Immediate cleanup without modifying velocity (used when jumping)
    private void StopSlideImmediate()
    {
        if (slideStopCoroutine != null)
        {
            StopCoroutine(slideStopCoroutine);
            slideStopCoroutine = null;
        }
        sliding = false;
        rb.drag = originalDrag;
        if (capsule != null) capsule.height = originalCapsuleHeight;
        isCrouching = false;
        LerpCrouch = true;
        if (SprintLines != null && SprintLines.isPlaying && !isSprinting) SprintLines.Stop();
        // sync MovePosition target to current velocity and re-enable MovePosition
        Vector3 v = rb.velocity; v.y = 0f; targetHorizontalVelocity = v;
        // movePositionEnabled = true;
    }

    private IEnumerator SmoothStopSlide()
    {
        sliding = false; // switch out of sliding state immediately for input
        float t = 0f;
        Vector3 startVel = rb.velocity;
        Vector3 startHoriz = new Vector3(startVel.x, 0f, startVel.z);
        rb.drag = originalDrag;
        if (capsule != null) capsule.height = originalCapsuleHeight;
        isCrouching = false;
        LerpCrouch = true;
        // stop sprint visuals if not sprinting
        if (SprintLines != null && SprintLines.isPlaying && !isSprinting) SprintLines.Stop();

        while (t < slideStopDuration)
        {
            if (rb == null) break;
            t += Time.deltaTime;
            float f = 1f - (t / slideStopDuration);
            f = Mathf.Clamp01(f);
            Vector3 horiz = startHoriz * (f * slideStopMultiplier + (1f - slideStopMultiplier));
            // preserve vertical
            float vy = rb.velocity.y;
            rb.velocity = new Vector3(horiz.x, vy, horiz.z);
            targetHorizontalVelocity = new Vector3(horiz.x, 0f, horiz.z);
            yield return null;
        }

        // final damping
        Vector3 final = rb.velocity; final.x *= slideStopMultiplier; final.z *= slideStopMultiplier; rb.velocity = final;
        targetHorizontalVelocity = new Vector3(final.x, 0f, final.z);
        // re-enable MovePosition now that sliding motion is finished
        // movePositionEnabled = true;
        slideStopCoroutine = null;
    }

#endregion

    public void Sprint()
    {
        speed *= SprintMultiplier;
    }


    public void DoFov(float endvalue)
    {
        if (Mathf.Approximately(currentFovTarget, endvalue)) return;

        currentFovTarget = endvalue;
        MainCam.DOKill();
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