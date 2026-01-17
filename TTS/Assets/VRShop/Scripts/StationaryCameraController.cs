using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VRShop
{
    /// <summary>
    /// Interface for look input - allows swapping between mouse and VR headset tracking
    /// </summary>
    public interface ILookInput
    {
        Vector2 GetLookDelta();
        bool IsVRMode { get; }
    }

    /// <summary>
    /// Default mouse-based look input implementation
    /// </summary>
    public class MouseLookInput : ILookInput
    {
        public bool IsVRMode => false;

        public Vector2 GetLookDelta()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current?.delta.ReadValue() ?? Vector2.zero;
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
        }
    }

    /// <summary>
    /// Stationary first-person camera controller with mouse look.
    /// VR-ready: when VR is enabled, the headset rotation will override this controller.
    /// </summary>
    public class StationaryCameraController : MonoBehaviour
    {
        [Header("Look Settings")]
        [Tooltip("Horizontal look sensitivity")]
        public float sensitivityX = 2.0f;
        
        [Tooltip("Vertical look sensitivity")]
        public float sensitivityY = 2.0f;
        
        [Tooltip("Minimum vertical angle (looking down)")]
        public float minVerticalAngle = -89f;
        
        [Tooltip("Maximum vertical angle (looking up)")]
        public float maxVerticalAngle = 89f;

        [Header("Cursor Settings")]
        [Tooltip("Lock cursor to center of screen")]
        public bool lockCursor = true;
        
        [Tooltip("Hide cursor when locked")]
        public bool hideCursor = true;

        [Header("VR Settings")]
        [Tooltip("When true, disables mouse look (VR headset takes over)")]
        public bool vrModeEnabled = false;

        // Current rotation state
        private float _yaw;   // Horizontal rotation
        private float _pitch; // Vertical rotation

        // Input provider (can be swapped for VR)
        private ILookInput _lookInput;

        /// <summary>
        /// Set a custom look input provider (for VR or other input methods)
        /// </summary>
        public void SetLookInput(ILookInput lookInput)
        {
            _lookInput = lookInput;
            vrModeEnabled = lookInput?.IsVRMode ?? false;
        }

        private void Awake()
        {
            // Default to mouse input
            _lookInput = new MouseLookInput();
        }

        private void Start()
        {
            // Initialize rotation from current transform
            Vector3 euler = transform.eulerAngles;
            _yaw = euler.y;
            _pitch = euler.x;
            
            // Normalize pitch to -180 to 180 range
            if (_pitch > 180f) _pitch -= 360f;

            UpdateCursorState();
        }

        private void OnEnable()
        {
            UpdateCursorState();
        }

        private void OnDisable()
        {
            // Restore cursor when disabled
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            // Skip mouse look in VR mode - headset handles rotation
            if (vrModeEnabled) return;

            HandleMouseLook();
        }

        private void HandleMouseLook()
        {
            Vector2 lookDelta = _lookInput?.GetLookDelta() ?? Vector2.zero;

            // Apply sensitivity
            _yaw += lookDelta.x * sensitivityX;
            _pitch -= lookDelta.y * sensitivityY; // Inverted for natural feel

            // Clamp vertical rotation to prevent flipping
            _pitch = Mathf.Clamp(_pitch, minVerticalAngle, maxVerticalAngle);

            // Apply rotation
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void UpdateCursorState()
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = !hideCursor;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        /// <summary>
        /// Reset camera to look at a specific direction
        /// </summary>
        public void ResetLook(float yaw = 0f, float pitch = 0f)
        {
            _yaw = yaw;
            _pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        /// <summary>
        /// Toggle cursor lock state (useful for UI interactions)
        /// </summary>
        public void SetCursorLock(bool locked)
        {
            lockCursor = locked;
            UpdateCursorState();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && lockCursor)
            {
                UpdateCursorState();
            }
        }
    }
}
