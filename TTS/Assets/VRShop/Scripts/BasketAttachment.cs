using UnityEngine;

namespace VRShop
{
    public class BasketAttachment : MonoBehaviour
    {
        [Header("Attachment")]
        public Transform anchor;
        public Transform basketRoot;
        public Vector3 positionOffset = Vector3.zero;
        public Vector3 rotationOffset = Vector3.zero;

        [Header("Grab State")]
        [Tooltip("Set by GrabbableBasket when the basket is being held")]
        public bool isGrabbed = false;

        [Header("Follow Settings")]
        [Tooltip("Smoothing speed for position lerp (higher = faster)")]
        public float followSpeed = 8f;
        
        [Tooltip("Use smooth following instead of instant")]
        public bool useSmoothFollow = true;

        private void LateUpdate()
        {
            // Skip follow behavior when grabbed by controller
            if (isGrabbed)
            {
                return;
            }

            var target = anchor;
            if (target == null && Camera.main != null)
            {
                target = Camera.main.transform;
            }

            if (target == null || basketRoot == null)
            {
                return;
            }

            Vector3 targetPosition = target.TransformPoint(positionOffset);
            Quaternion targetRotation = target.rotation * Quaternion.Euler(rotationOffset);

            if (useSmoothFollow)
            {
                // Smooth follow for nicer feel when released
                basketRoot.position = Vector3.Lerp(basketRoot.position, targetPosition, Time.deltaTime * followSpeed);
                basketRoot.rotation = Quaternion.Slerp(basketRoot.rotation, targetRotation, Time.deltaTime * followSpeed);
            }
            else
            {
                // Instant follow (original behavior)
                basketRoot.position = targetPosition;
                basketRoot.rotation = targetRotation;
            }
        }
    }
}
