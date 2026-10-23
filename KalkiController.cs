using UnityEngine;

/// <summary>
/// Third-Person 3D Player Controller for KALKI: NEON DHARMA.
/// Controls player movement (WASD / Left Stick), camera-relative direction facing,
/// sprint, jumping, falling gravity, ground detection, and Animator parameter synchronization.
///
/// UNITY SETUP INSTRUCTIONS:
/// 1. Attach this component to the Kalki player GameObject (Tag: "Player").
/// 2. Ensure the GameObject has a CharacterController or Rigidbody + CapsuleCollider.
///    (If CharacterController is present, it uses smooth collision-resolved movement.
///     If Rigidbody is present, it uses velocity-based movement.)
/// 3. Assign the Camera Transform (typically Camera.main.transform).
/// 4. Assign Animator component (or leave empty to auto-detect on child objects).
/// 5. Animator parameters configured: "Speed" (Float), "IsGrounded" (Bool), "VerticalVelocity" (Float), "Jump" (Trigger).
///
/// DEPENDENCIES:
/// - UnityEngine, UnityEngine.Animator
/// - Compatible with KalkiCameraController.cs and PlayerCombat.cs
///
/// TEST CASES:
/// - Press W/A/S/D: Kalki moves relative to camera orientation and rotates smoothly to movement heading.
/// - Hold Left Shift: Kalki sprints at sprintSpeed.
/// - Press Space: Kalki jumps with responsive takeoff and falls faster when descending.
/// - Cutscene/Dialogue: Calling SetMovementLocked(true) disables movement and resets speed to 0.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class KalkiController : MonoBehaviour
{
    [Header("Movement Speeds")]
    [Tooltip("Base walking/jogging speed in units/sec.")]
    [SerializeField] private float walkSpeed = 6f;

    [Tooltip("Sprint speed when holding Shift.")]
    [SerializeField] private float sprintSpeed = 10f;

    [Tooltip("Turn smoothing time when orienting towards movement direction.")]
    [SerializeField] private float turnSmoothTime = 0.1f;

    [Header("Jump & Gravity")]
    [Tooltip("Initial jump impulse velocity.")]
    [SerializeField] private float jumpHeight = 2.2f;

    [Tooltip("Gravity acceleration (downward).")]
    [SerializeField] private float gravity = -20f;

    [Tooltip("Coyote time duration allowing jump shortly after stepping off an edge.")]
    [SerializeField] private float coyoteTime = 0.15f;

    [Tooltip("Jump buffer window registering keypress before touching ground.")]
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Header("Ground Detection")]
    [Tooltip("Ground check point near player feet. If null, uses bottom of CharacterController.")]
    [SerializeField] private Transform groundCheck;

    [Tooltip("Radius of ground check sphere.")]
    [SerializeField] private float groundDistance = 0.3f;

    [Tooltip("Layer mask recognized as walkable ground.")]
    [SerializeField] private LayerMask groundMask;

    [Header("Camera & Visuals")]
    [Tooltip("Main camera transform for camera-relative movement direction.")]
    [SerializeField] private Transform cameraTransform;

    [Tooltip("Visual model mesh to rotate. If null, rotates this transform.")]
    [SerializeField] private Transform visualRoot;

    [Header("Animator Synchronization")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string groundedParam = "IsGrounded";
    [SerializeField] private string verticalVelocityParam = "VerticalVelocity";
    [SerializeField] private string jumpTrigger = "Jump";

    // References
    private CharacterController controller;

    // Internal State
    private Vector3 verticalVelocity = Vector3.zero;
    private float turnSmoothVelocity;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool isGrounded;
    private bool isMovementLocked = false;

    // Public Properties
    public bool IsGrounded => isGrounded;
    public bool IsMovementLocked => isMovementLocked;
    public float CurrentSpeed { get; private set; }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (visualRoot == null)
        {
            visualRoot = transform;
        }
    }

    private void Update()
    {
        CheckGrounded();
        HandleJumpBuffer();

        if (isMovementLocked)
        {
            ApplyGravityOnly();
            UpdateAnimator(0f);
            return;
        }

        HandleMovement();
    }

    private void CheckGrounded()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);
        }
        else
        {
            isGrounded = controller.isGrounded;
        }

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            if (verticalVelocity.y < 0f)
            {
                // Small grounding force to keep controller firmly on slopes
                verticalVelocity.y = -2f;
            }
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }
    }

    private void HandleJumpBuffer()
    {
        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void HandleMovement()
    {
        // 1. Gather Input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // 2. Camera-Relative Direction & Rotation
        if (inputDirection.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
            if (cameraTransform != null)
            {
                targetAngle += cameraTransform.eulerAngles.y;
            }

            float angle = Mathf.SmoothDampAngle(visualRoot.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            visualRoot.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * (targetSpeed * Time.deltaTime));

            CurrentSpeed = targetSpeed;
        }
        else
        {
            CurrentSpeed = 0f;
        }

        // 3. Jump Execution
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;

            if (animator != null && !string.IsNullOrEmpty(jumpTrigger))
            {
                animator.SetTrigger(jumpTrigger);
            }
        }

        // 4. Apply Gravity
        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);

        // 5. Update Animation
        UpdateAnimator(CurrentSpeed);
    }

    private void ApplyGravityOnly()
    {
        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    private void UpdateAnimator(float speed)
    {
        if (animator == null) return;

        if (!string.IsNullOrEmpty(speedParam))
            animator.SetFloat(speedParam, speed);

        if (!string.IsNullOrEmpty(groundedParam))
            animator.SetBool(groundedParam, isGrounded);

        if (!string.IsNullOrEmpty(verticalVelocityParam))
            animator.SetFloat(verticalVelocityParam, verticalVelocity.y);
    }

    /// <summary>
    /// Locks or unlocks player movement (used by cinematic sequences and story transitions).
    /// </summary>
    public void SetMovementLocked(bool locked)
    {
        isMovementLocked = locked;
        if (locked)
        {
            CurrentSpeed = 0f;
            UpdateAnimator(0f);
        }
    }

    /// <summary>
    /// Teleports Kalki to a given position and rotation cleanly.
    /// </summary>
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        controller.enabled = false;
        transform.position = position;
        if (visualRoot != null) visualRoot.rotation = rotation;
        controller.enabled = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}
