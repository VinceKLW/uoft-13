using UnityEngine;

namespace VRShop
{
    /// <summary>
    /// Automatically sets up specific prefabs (sample, textured_mesh_1, etc.) 
    /// as grabbable items that can be added to the cart.
    /// Attach this to any GameObject in the scene.
    /// </summary>
    public class PrefabItemSetup : MonoBehaviour
    {
        [Header("Prefab Names to Make Grabbable")]
        [Tooltip("Names of GameObjects to convert to grabbable items")]
        [SerializeField] private string[] prefabNames = new string[] 
        { 
            "sample",           // deer-head
            "sample (1)",       // deer-body  
            "sample (3)",       // deer-arm
            "textured_mesh_1"   // t-shirt
        };

        [Header("Item Settings")]
        [SerializeField] private float defaultPrice = 9.99f;
        [SerializeField] private bool addColliderIfMissing = true;
        [SerializeField] private float colliderPadding = 0.05f;

        private void Start()
        {
            SetupAllPrefabs();
        }

        public void SetupAllPrefabs()
        {
            foreach (string prefabName in prefabNames)
            {
                SetupPrefabByName(prefabName);
            }
        }

        private void SetupPrefabByName(string prefabName)
        {
            // Find all GameObjects with this name
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            
            foreach (var obj in allObjects)
            {
                if (obj.name.Equals(prefabName, System.StringComparison.OrdinalIgnoreCase) ||
                    obj.name.StartsWith(prefabName, System.StringComparison.OrdinalIgnoreCase))
                {
                    SetupAsGrabbableItem(obj, prefabName);
                }
            }
        }

        private void SetupAsGrabbableItem(GameObject obj, string itemName)
        {
            // Skip if already has BasketItem
            if (obj.GetComponent<BasketItem>() != null)
            {
                Debug.Log($"[PrefabItemSetup] {obj.name} already has BasketItem");
                return;
            }

            // Add BasketItem component
            BasketItem basketItem = obj.AddComponent<BasketItem>();
            basketItem.itemId = obj.name;
            basketItem.displayName = GetDisplayName(itemName);
            basketItem.price = GetPriceForItem(itemName);
            basketItem.canBeCollected = true;

            // Add collider if missing
            if (addColliderIfMissing && obj.GetComponent<Collider>() == null)
            {
                AddAutoCollider(obj);
            }

            // Make sure collider is enabled
            var collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = true;
            }

            Debug.Log($"[PrefabItemSetup] Set up '{obj.name}' as grabbable item: {basketItem.displayName} (${basketItem.price})");
        }

        private void AddAutoCollider(GameObject obj)
        {
            // Try to size collider based on renderer bounds
            Renderer renderer = obj.GetComponentInChildren<Renderer>();
            
            if (renderer != null)
            {
                BoxCollider collider = obj.AddComponent<BoxCollider>();
                
                // Calculate bounds in local space
                Bounds bounds = renderer.bounds;
                collider.center = obj.transform.InverseTransformPoint(bounds.center);
                collider.size = bounds.size + Vector3.one * colliderPadding;
                
                Debug.Log($"[PrefabItemSetup] Added BoxCollider to {obj.name}, size: {collider.size}");
            }
            else
            {
                // Fallback: add a small default collider
                BoxCollider collider = obj.AddComponent<BoxCollider>();
                collider.size = new Vector3(0.2f, 0.2f, 0.2f);
                Debug.Log($"[PrefabItemSetup] Added default BoxCollider to {obj.name}");
            }
        }

        private string GetDisplayName(string prefabName)
        {
            // Convert prefab names to friendly display names
            switch (prefabName.ToLower())
            {
                case "sample":
                    return "Deer Head";
                case "sample (1)":
                    return "Deer Body";
                case "sample (3)":
                    return "Deer Arm";
                case "textured_mesh_1":
                    return "T-Shirt";
                default:
                    // Capitalize first letter
                    return char.ToUpper(prefabName[0]) + prefabName.Substring(1);
            }
        }

        private float GetPriceForItem(string prefabName)
        {
            // Set prices for specific items
            switch (prefabName.ToLower())
            {
                case "sample":
                    return 49.99f;  // Deer Head
                case "sample (1)":
                    return 79.99f;  // Deer Body
                case "sample (3)":
                    return 19.99f;  // Deer Arm
                case "textured_mesh_1":
                    return 24.99f;  // T-Shirt
                default:
                    return defaultPrice;
            }
        }

        [ContextMenu("Setup Prefabs Now")]
        public void SetupNow()
        {
            SetupAllPrefabs();
        }
    }
}

