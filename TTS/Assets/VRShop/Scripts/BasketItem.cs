using UnityEngine;

namespace VRShop
{
    public class BasketItem : MonoBehaviour
    {
        [Header("Item Info")]
        public string itemId;
        public string displayName;
        public float price;
        
        [Header("State")]
        public bool canBeCollected = true;
        public bool isCollected;
        public bool isHeld;

        private Rigidbody cachedRigidbody;
        private Collider cachedCollider;

        private void Awake()
        {
            cachedRigidbody = GetComponent<Rigidbody>();
            cachedCollider = GetComponent<Collider>();
        }

        public void DisablePhysics()
        {
            if (cachedRigidbody == null)
                cachedRigidbody = GetComponent<Rigidbody>();

            if (cachedRigidbody != null)
            {
                cachedRigidbody.isKinematic = true;
                cachedRigidbody.useGravity = false;
                cachedRigidbody.linearVelocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
            }

            if (cachedCollider == null)
                cachedCollider = GetComponent<Collider>();
                
            if (cachedCollider != null)
            {
                cachedCollider.enabled = false;
            }
        }

        public void EnablePhysics()
        {
            // Add Rigidbody if not present
            if (cachedRigidbody == null)
            {
                cachedRigidbody = GetComponent<Rigidbody>();
                if (cachedRigidbody == null)
                    cachedRigidbody = gameObject.AddComponent<Rigidbody>();
            }

            // Add Collider if not present
            if (cachedCollider == null)
            {
                cachedCollider = GetComponent<Collider>();
                if (cachedCollider == null)
                {
                    var boxCollider = gameObject.AddComponent<BoxCollider>();
                    AutoSizeCollider(boxCollider);
                    cachedCollider = boxCollider;
                }
            }

            cachedRigidbody.isKinematic = false;
            cachedRigidbody.useGravity = true;
            cachedRigidbody.linearDamping = 1f;
            cachedRigidbody.angularDamping = 2f;
            cachedCollider.enabled = true;
        }

        private void AutoSizeCollider(BoxCollider collider)
        {
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;
                collider.center = transform.InverseTransformPoint(bounds.center);
                collider.size = new Vector3(
                    Mathf.Max(Mathf.Abs(transform.InverseTransformVector(bounds.size).x), 0.05f),
                    Mathf.Max(Mathf.Abs(transform.InverseTransformVector(bounds.size).y), 0.05f),
                    Mathf.Max(Mathf.Abs(transform.InverseTransformVector(bounds.size).z), 0.05f)
                );
            }
            else
            {
                collider.size = new Vector3(0.1f, 0.1f, 0.1f);
            }
        }

        public void OnGrabbed()
        {
            isHeld = true;
            DisablePhysics();
        }

        public void OnReleased(bool dropWithPhysics = true)
        {
            isHeld = false;
            
            if (dropWithPhysics && !isCollected)
            {
                transform.SetParent(null);
                EnablePhysics();
            }
        }
    }
}
