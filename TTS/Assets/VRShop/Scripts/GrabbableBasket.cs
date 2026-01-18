using UnityEngine;

namespace VRShop
{
    /// <summary>
    /// Makes the shopping basket grabbable with Quest controllers.
    /// Uses grip button to grab when controller is near the basket.
    /// </summary>
    public class GrabbableBasket : MonoBehaviour
    {
        [Header("Grab Settings")]
        [Tooltip("Maximum distance from controller to grab the basket")]
        public float grabDistance = 0.5f;
        
        [Tooltip("Offset from controller when grabbed")]
        public Vector3 grabOffset = new Vector3(0f, -0.1f, 0.1f);
        
        [Tooltip("Rotation offset when grabbed")]
        public Vector3 grabRotationOffset = new Vector3(0f, 0f, 0f);

        [Header("Controller References")]
        [Tooltip("Left controller transform (auto-found if not set)")]
        public Transform leftController;
        
        [Tooltip("Right controller transform (auto-found if not set)")]
        public Transform rightController;

        [Header("Components")]
        [Tooltip("BasketAttachment component to disable during grab")]
        public BasketAttachment basketAttachment;

        // Grab state
        private bool isGrabbed = false;
        private Transform grabbingController = null;
        private OVRInput.Controller grabbingControllerType;

        private void Start()
        {
            // Auto-find BasketAttachment if not assigned
            if (basketAttachment == null)
            {
                basketAttachment = GetComponent<BasketAttachment>();
            }

            // Try to find controller transforms if not assigned
            FindControllers();
        }

        private void FindControllers()
        {
            if (leftController == null || rightController == null)
            {
                // Look for OVRCameraRig and its anchors
                var cameraRig = FindObjectOfType<OVRCameraRig>();
                if (cameraRig != null)
                {
                    if (leftController == null)
                    {
                        leftController = cameraRig.leftHandAnchor;
                    }
                    if (rightController == null)
                    {
                        rightController = cameraRig.rightHandAnchor;
                    }
                }

                // Alternative: search by name
                if (leftController == null)
                {
                    var leftObj = GameObject.Find("LeftControllerAnchor");
                    if (leftObj != null) leftController = leftObj.transform;
                }
                if (rightController == null)
                {
                    var rightObj = GameObject.Find("RightControllerAnchor");
                    if (rightObj != null) rightController = rightObj.transform;
                }
            }
        }

        private void Update()
        {
            if (isGrabbed)
            {
                HandleGrabbedState();
            }
            else
            {
                CheckForGrab();
            }
        }

        private void CheckForGrab()
        {
            // Check left controller grip
            if (leftController != null && OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
            {
                float distance = Vector3.Distance(transform.position, leftController.position);
                if (distance <= grabDistance)
                {
                    StartGrab(leftController, OVRInput.Controller.LTouch);
                    return;
                }
            }

            // Check right controller grip
            if (rightController != null && OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
            {
                float distance = Vector3.Distance(transform.position, rightController.position);
                if (distance <= grabDistance)
                {
                    StartGrab(rightController, OVRInput.Controller.RTouch);
                    return;
                }
            }
        }

        private void StartGrab(Transform controller, OVRInput.Controller controllerType)
        {
            isGrabbed = true;
            grabbingController = controller;
            grabbingControllerType = controllerType;

            // Disable follow behavior
            if (basketAttachment != null)
            {
                basketAttachment.isGrabbed = true;
            }

            // Haptic feedback
            OVRInput.SetControllerVibration(0.3f, 0.3f, controllerType);

            Debug.Log($"[GrabbableBasket] Grabbed by {controllerType}");
        }

        private void HandleGrabbedState()
        {
            // Check if grip is released
            bool gripHeld = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, grabbingControllerType);
            
            if (!gripHeld)
            {
                EndGrab();
                return;
            }

            // Update position to follow controller
            if (grabbingController != null)
            {
                // Apply position with offset
                Vector3 targetPosition = grabbingController.TransformPoint(grabOffset);
                transform.position = targetPosition;

                // Apply rotation with offset
                Quaternion targetRotation = grabbingController.rotation * Quaternion.Euler(grabRotationOffset);
                transform.rotation = targetRotation;
            }
        }

        private void EndGrab()
        {
            // Haptic feedback on release
            OVRInput.SetControllerVibration(0.1f, 0.1f, grabbingControllerType);

            isGrabbed = false;
            grabbingController = null;

            // Re-enable follow behavior
            if (basketAttachment != null)
            {
                basketAttachment.isGrabbed = false;
            }

            Debug.Log("[GrabbableBasket] Released");
        }

        /// <summary>
        /// Check if the basket is currently being held
        /// </summary>
        public bool IsGrabbed => isGrabbed;

        /// <summary>
        /// Get the controller currently holding the basket (null if not grabbed)
        /// </summary>
        public Transform GrabbingController => grabbingController;

        private void OnDrawGizmosSelected()
        {
            // Visualize grab range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, grabDistance);
        }
    }
}

