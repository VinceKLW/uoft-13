using UnityEngine;

namespace VRShop
{
    public class BasketItem : MonoBehaviour
    {
        public string itemId;
        public string displayName;
        public float price;
        public bool canBeCollected = true;
        public bool isCollected;

        private Rigidbody cachedRigidbody;

        private void Awake()
        {
            cachedRigidbody = GetComponent<Rigidbody>();
        }

        public void DisablePhysics()
        {
            if (cachedRigidbody == null)
            {
                cachedRigidbody = GetComponent<Rigidbody>();
            }

            if (cachedRigidbody != null)
            {
                cachedRigidbody.isKinematic = true;
                cachedRigidbody.useGravity = false;
            }

            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }
}
