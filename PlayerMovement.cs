using UnityEngine;

/// <summary>
/// 2.5D Player Movement Controller for KALKI: NEON DHARMA.
/// Restricts movement to the 2.5D fighting plane (X/Y axis), supports smooth left/right
/// running with A/D or arrow keys, snappy jumping with ground detection, and animator synchronization.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Horizontal movement speed in units/sec.")]
    [SerializeField] private float moveSpeed = 8f;

    [Tooltip("Smoothing time for horizontal velocity.")]
    [SerializeField] private float accelerationTime = 0.05f;

    [Header("Jump & Gravity")]
    [Tooltip("Vertical impulse force applied when jumping.")]
    [SerializeField] private float jumpForce = 12f;

    [Tooltip("Additional gravity applied when falling for a punchy, responsive jump feel.")]
    [SerializeField] private float fallMultiplier = 2.5f;

    [Tooltip("Additional gravity applied when releasing the jump button early.")]
    [SerializeField] private float lowJumpMultiplier = 2.0f;

    [Tooltip("Coyote time window (seconds) allowing a jump shortly after leaving a ledge.")]
    [SerializeField] private float coyoteTimeDuration = 0.15f;

    [Tooltip("Jump buffer window (seconds) registering jump keypress right before landing.")]
    [SerializeField] private float jumpBufferDuration = 0.15f;

    [Header("Ground Detection")]
    [Tooltip("Transform representing the bottom check position. If null, bottom of collider is used.")]
    [SerializeField] private Transform groundCheckPoint;

    [Tooltip("Radius of the ground check sphere.")]
    [SerializeField] private float groundCheckRadius = 0.25f;

    [Tooltip("Layers recognized as solid ground.")]
    [SerializeField] private LayerMask groundLayer;

    [Header("2.5D Plane Constraints")]
    [Tooltip("Locked Z position for the 2.5D fighting plane.")]
    [SerializeField] private float fixedZPosition = 0f;

    [Tooltip("Enforce fixed Z position every frame to prevent any drift.")]
    [SerializeField] private bool lockZPosition = true;

    [Header("Facing Direction & Visuals")]
    [Tooltip("Mesh transform or visual root to rotate. If null, rotates this gameObject.")]
    [SerializeField] private Transform visualModel;

    [Tooltip("Rotation speed when turning left/right.")]
    [SerializeField] private float turnSpeed = 15f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string groundedParam = "IsGrounded";
    [SerializeField] private string verticalVelocityParam = "VerticalVelocity";
    [SerializeField] private string jumpTrigger = "Jump";

    // Component references
    private Rigidbody rb;
    private CapsuleCollider col;

    // State variables
    private float horizontalInput;
    private bool isGrounded;
    private bool isFacingRight = true;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float currentVelocityX;
    private bool canMove = true;

    public bool IsGrounded => isGrounded;
    public bool IsFacingRight => isFacingRight;
    public float HorizontalInput => horizontalInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (visualModel == null)
        {
            visualModel = transform;
        }

        // Configure Rigidbody constraints for 2.5D combat
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Remember current Z as the locked 2.5D plane if not explicitly set
        if (Mathf.Approximately(fixedZPosition, 0f))
        {
            fixedZPosition = transform.position.z;
        }
    }

    private void Update()
    {
        ReadInput();
        UpdateTimers();
        CheckGrounded();
        HandleJump();
        UpdateFacingDirection();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
        ApplyBetterJumpPhysics();

        if (lockZPosition)
        {
            EnforceFixedZ();
        }
    }

    /// <summary>
    /// Reads movement inputs from A/D or Arrow keys.
    /// </summary>
    private void ReadInput()
    {
        if (!canMove)
        {
            horizontalInput = 0f;
            return;
        }

        // Supports A/D and Left/Right arrow keys out-of-the-box
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Jump button pressed (Spacebar / Default Jump)
        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferTimer = jumpBufferDuration;
        }
    }

    private void UpdateTimers()
    {
        // Decrement jump buffer
        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        // Decrement coyote time
        if (isGrounded)
        {
            coyoteTimer = coyoteTimeDuration;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Checks if player is on the ground using OverlapSphere or bottom collider check.
    /// </summary>
    private void CheckGrounded()
    {
        Vector3 checkPos = GetGroundCheckPosition();
        
        // Sphere check against ground layers
        if (groundLayer.value != 0)
        {
            isGrounded = Physics.CheckSphere(checkPos, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
        }
        else
        {
            // Fallback: check any collider except self
            Collider[] hits = Physics.OverlapSphere(checkPos, groundCheckRadius, ~0, QueryTriggerInteraction.Ignore);
            isGrounded = false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject != gameObject && !hits[i].transform.IsChildOf(transform))
                {
                    isGrounded = true;
                    break;
                }
            }
        }
    }

    private Vector3 GetGroundCheckPosition()
    {
        if (groundCheckPoint != null)
        {
            return groundCheckPoint.position;
        }

        // Calculate bottom of capsule collider
        return transform.position + Vector3.up * 0.05f;
    }

    /// <summary>
    /// Executes jump if ground/coyote and jump buffer conditions are met.
    /// </summary>
    private void HandleJump()
    {
        if (!canMove) return;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            // Reset vertical velocity for consistent jump height
            rb.velocity = new Vector3(rb.velocity.x, jumpForce, 0f);

            // Consume timers to prevent double-jumping
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;

            if (animator != null && !string.IsNullOrEmpty(jumpTrigger))
            {
                animator.SetTrigger(jumpTrigger);
            }
        }
    }

    /// <summary>
    /// Applies responsive platformer gravity multipliers.
    /// </summary>
    private void ApplyBetterJumpPhysics()
    {
        if (rb.velocity.y < 0f)
        {
            // Faster falling
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0f && !(Input.GetButton("Jump") || Input.GetKey(KeyCode.Space)))
        {
            // Short hop if jump button released early
            rb.velocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    /// <summary>
    /// Applies horizontal velocity along X axis with smooth acceleration.
    /// </summary>
    private void ApplyMovement()
    {
        float targetSpeed = horizontalInput * moveSpeed;
        float smoothedSpeed = Mathf.SmoothDamp(rb.velocity.x, targetSpeed, ref currentVelocityX, accelerationTime);

        rb.velocity = new Vector3(smoothedSpeed, rb.velocity.y, 0f);
    }

    /// <summary>
    /// Enforces strict 2.5D plane by resetting Z position and Z velocity.
    /// </summary>
    private void EnforceFixedZ()
    {
        Vector3 pos = transform.position;
        if (!Mathf.Approximately(pos.z, fixedZPosition))
        {
            pos.z = fixedZPosition;
            transform.position = pos;
        }

        if (!Mathf.Approximately(rb.velocity.z, 0f))
        {
            rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y, 0f);
        }
    }

    /// <summary>
    /// Smoothly rotates character to face moving direction (Right: +X, Left: -X).
    /// </summary>
    private void UpdateFacingDirection()
    {
        if (Mathf.Abs(horizontalInput) > 0.05f)
        {
            isFacingRight = horizontalInput > 0f;
        }

        // Target angle on Y: 90 degrees faces right (+X), 270 degrees (-90) faces left (-X)
        float targetYAngle = isFacingRight ? 90f : 270f;
        Quaternion targetRotation = Quaternion.Euler(0f, targetYAngle, 0f);

        if (visualModel != null)
        {
            visualModel.rotation = Quaternion.Slerp(visualModel.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Sends speed, grounded state, and vertical velocity to Animator.
    /// </summary>
    private void UpdateAnimator()
    {
        if (animator == null) return;

        if (!string.IsNullOrEmpty(speedParam))
        {
            animator.SetFloat(speedParam, Mathf.Abs(horizontalInput));
        }

        if (!string.IsNullOrEmpty(groundedParam))
        {
            animator.SetBool(groundedParam, isGrounded);
        }

        if (!string.IsNullOrEmpty(verticalVelocityParam))
        {
            animator.SetFloat(verticalVelocityParam, rb.velocity.y);
        }
    }

    /// <summary>
    /// Can be toggled by PlayerCombat or cutscenes to restrict movement during attacks.
    /// </summary>
    public void SetMovementEnabled(bool enabled)
    {
        canMove = enabled;
        if (!enabled)
        {
            horizontalInput = 0f;
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize ground check in Unity Editor
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 pos = GetGroundCheckPosition();
        Gizmos.DrawWireSphere(pos, groundCheckRadius);
    }
}
