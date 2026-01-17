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

        private void LateUpdate()
        {
            var target = anchor;
            if (target == null && Camera.main != null)
            {
                target = Camera.main.transform;
            }

            if (target == null || basketRoot == null)
            {
                return;
            }

            basketRoot.position = target.TransformPoint(positionOffset);
            basketRoot.rotation = target.rotation * Quaternion.Euler(rotationOffset);
        }
    }
}
