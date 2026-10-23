using UnityEngine;

/// <summary>
/// Smooth Third-Person Camera Controller for KALKI: NEON DHARMA.
/// Provides orbit mouse-look, responsive damping follow, obstacle/ground collision avoidance,
/// and support for smooth blending into cinematic angles during story cutscenes.
///
/// UNITY SETUP INSTRUCTIONS:
/// 1. Attach this script to your Main Camera GameObject.
/// 2. Set 'Target' to Kalki's Transform (or a child 'CameraFollowTarget' around head/chest level).
/// 3. Configure 'Collision Layers' to include Ground and Environment/Obstacles (exclude Player layer!).
/// 4. Adjust Distance (e.g. 4.5), Height (e.g. 1.8), and Pitch limits (-20 to 60 degrees).
///
/// DEPENDENCIES:
/// - UnityEngine
/// - Works seamlessly with KalkiController.cs and KalkiStoryManager.cs
///
/// TEST CASES:
/// - Move mouse: Camera orbits smoothly around Kalki.
/// - Walk near a wall or slope: Camera smoothly moves closer without clipping inside geometry.
/// - Trigger cinematic cutscene: Camera blends to cinematic shot position/rotation.
/// </summary>
public class KalkiCameraController : MonoBehaviour
{
    [Header("Target Tracking")]
    [Tooltip("Target transform to follow (Kalki or target pivot).")]
    [SerializeField] private Transform target;

    [Tooltip("Offset from target position (e.g. head/chest elevation).")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.8f, 0f);

    [Header("Distance & Orbit Settings")]
    [Tooltip("Default desired distance behind target.")]
    [SerializeField] private float defaultDistance = 4.5f;

    [Tooltip("Minimum allowable distance when colliding.")]
    [SerializeField] private float minDistance = 1.0f;

    [Tooltip("Maximum camera distance when zooming out.")]
    [SerializeField] private float maxDistance = 7.0f;

    [Tooltip("Mouse orbit sensitivity on X axis (yaw).")]
    [SerializeField] private float mouseSensitivityX = 3.0f;

    [Tooltip("Mouse orbit sensitivity on Y axis (pitch).")]
    [SerializeField] private float mouseSensitivityY = 2.0f;

    [Tooltip("Minimum vertical angle (pitch downward).")]
    [SerializeField] private float minPitch = -25f;

    [Tooltip("Maximum vertical angle (pitch upward).")]
    [SerializeField] private float maxPitch = 65f;

    [Header("Smoothing")]
    [Tooltip("Time to reach target position.")]
    [SerializeField] private float followSmoothTime = 0.08f;

    [Tooltip("Time to damp distance during collisions.")]
    [SerializeField] private float collisionSmoothTime = 0.05f;

    [Header("Collision & Ground Clipping Prevention")]
    [Tooltip("Layers camera will collide against to prevent clipping through geometry.")]
    [SerializeField] private LayerMask collisionLayers;

    [Tooltip("Radius of collision sphere check.")]
    [SerializeField] private float collisionRadius = 0.25f;

    [Tooltip("Distance buffer away from hit surface.")]
    [SerializeField] private float collisionBuffer = 0.15f;

    [Header("Cinematic Override")]
    [Tooltip("Whether camera is currently controlled by a cinematic sequence.")]
    [SerializeField] private bool isCinematicActive = false;

    // Internal State
    private float yaw = 0f;
    private float pitch = 15f;
    private float currentDistance;
    private Vector3 currentVelocity;
    private float distanceVelocity;

    // Cinematic Blending
    private Transform cinematicShotTarget;
    private float cinematicBlendSpeed = 3.0f;

    public bool IsCinematicActive => isCinematicActive;
    public Transform Target => target;

    private void Awake()
    {
        currentDistance = defaultDistance;
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        // Auto-find player if not assigned
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    private void Start()
    {
        // Cursor lock for smooth mouse orbit
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (isCinematicActive && cinematicShotTarget != null)
        {
            HandleCinematicMode();
            return;
        }

        if (target == null) return;

        HandleInput();
        CalculateCameraPosition();
    }

    private void HandleInput()
    {
        // Allow mouse look when cursor is locked or right click held
        yaw += Input.GetAxis("Mouse X") * mouseSensitivityX;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivityY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Optional scroll wheel zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            defaultDistance = Mathf.Clamp(defaultDistance - scroll * 2.5f, minDistance, maxDistance);
        }

        // Toggle cursor lock with Escape / Alt
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
        }
    }

    private void CalculateCameraPosition()
    {
        Vector3 pivotPosition = target.position + targetOffset;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Calculate desired collision-free distance
        Vector3 direction = rotation * -Vector3.forward;
        float targetDistance = defaultDistance;

        Ray ray = new Ray(pivotPosition, direction);
        if (Physics.SphereCast(ray, collisionRadius, out RaycastHit hit, defaultDistance, collisionLayers, QueryTriggerInteraction.Ignore))
        {
            targetDistance = Mathf.Clamp(hit.distance - collisionBuffer, minDistance, defaultDistance);
        }

        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, collisionSmoothTime);

        // Calculate final position
        Vector3 targetCameraPos = pivotPosition + direction * currentDistance;
        transform.position = Vector3.SmoothDamp(transform.position, targetCameraPos, ref currentVelocity, followSmoothTime);
        transform.LookAt(pivotPosition);
    }

    private void HandleCinematicMode()
    {
        transform.position = Vector3.Lerp(transform.position, cinematicShotTarget.position, Time.deltaTime * cinematicBlendSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, cinematicShotTarget.rotation, Time.deltaTime * cinematicBlendSpeed);
    }

    /// <summary>
    /// Smoothly transitions camera to a cinematic shot angle.
    /// </summary>
    public void EnterCinematicShot(Transform shotTransform, float blendSpeed = 3.0f)
    {
        cinematicShotTarget = shotTransform;
        cinematicBlendSpeed = blendSpeed;
        isCinematicActive = true;
    }

    /// <summary>
    /// Restores standard third-person gameplay camera follow.
    /// </summary>
    public void ExitCinematicShot()
    {
        cinematicShotTarget = null;
        isCinematicActive = false;

        // Sync angles to current camera view to avoid jarring snap
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;
        if (pitch > 180f) pitch -= 360f;
    }

    /// <summary>
    /// Changes the target followed by the camera.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
