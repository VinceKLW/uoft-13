using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VRShop
{
    /// <summary>
    /// AI-powered shop assistant that comments on what's happening in the store.
    /// Uses OpenAI to generate contextual quips based on cart contents, nearby items, and user behavior.
    /// </summary>
    public class ShopAssistant : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private OpenAIClient openAIClient;
        [SerializeField] private BasketTrigger basketTrigger;
        [SerializeField] private Transform playerCamera;
        [SerializeField] private AudioSource audioSource;

        [Header("Quip Settings")]
        [Tooltip("How often to generate quips (seconds)")]
        [SerializeField] private float quipInterval = 10f;
        
        [Tooltip("Minimum time between quips")]
        [SerializeField] private float minQuipInterval = 5f;
        
        [Tooltip("Random variation in quip timing")]
        [SerializeField] private float quipRandomness = 3f;

        [Header("Detection Settings")]
        [Tooltip("How far to look for nearby products")]
        [SerializeField] private float nearbyProductRange = 5f;
        
        [Tooltip("Field of view for 'what user sees' detection")]
        [SerializeField] private float viewAngle = 60f;

        [Header("Personality")]
        [TextArea(3, 10)]
        [SerializeField] private string personalityPrompt = @"You are Shoppy, a silly squeaky shopping mascot. 
Reply with ONLY 3-8 words max! Make a quick joke about:
- If cart is empty: shopping puns, Shopify jokes, 'add to cart' humor
- If cart has items: joke about what they're buying
Be punny, silly, and squeaky! No full sentences - just quick quips!";

        private float nextQuipTime;
        private bool isGenerating = false;
        private string lastQuip = "";
        private int itemCountLastQuip = 0;
        private Vector3 lastPlayerPosition;

        private void Start()
        {
            Debug.Log("[ShopAssistant] Start() called!");
            
            // Auto-find references if not set
            if (openAIClient == null)
            {
                openAIClient = FindObjectOfType<OpenAIClient>();
                Debug.Log($"[ShopAssistant] Found OpenAIClient: {openAIClient != null}");
            }
            if (basketTrigger == null)
            {
                basketTrigger = FindObjectOfType<BasketTrigger>();
                Debug.Log($"[ShopAssistant] Found BasketTrigger: {basketTrigger != null}");
            }
            if (playerCamera == null && Camera.main != null)
            {
                playerCamera = Camera.main.transform;
                Debug.Log($"[ShopAssistant] Found Camera: {playerCamera != null}");
            }
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            // Set initial quip time
            nextQuipTime = Time.time + quipInterval + Random.Range(0f, quipRandomness);
            lastPlayerPosition = playerCamera != null ? playerCamera.position : Vector3.zero;
            
            Debug.Log($"[ShopAssistant] Initialized! First quip in {nextQuipTime - Time.time:F1} seconds");
        }

        private void Update()
        {
            // Check if it's time for a quip
            if (Time.time >= nextQuipTime && !isGenerating && (audioSource == null || !audioSource.isPlaying))
            {
                Debug.Log($"[ShopAssistant] Time for quip! Time: {Time.time:F1}, NextQuip: {nextQuipTime:F1}");
                TriggerQuip();
            }

            // Also trigger quip on significant events
            CheckForSignificantEvents();
        }

        private void CheckForSignificantEvents()
        {
            if (isGenerating || audioSource.isPlaying) return;
            if (basketTrigger == null) return;

            // Trigger quip when items are added to cart
            int currentItemCount = basketTrigger.ItemCount;
            if (currentItemCount > itemCountLastQuip && currentItemCount > 0)
            {
                if (Time.time > nextQuipTime - quipInterval + minQuipInterval) // Respect minimum interval
                {
                    itemCountLastQuip = currentItemCount;
                    TriggerQuip("item_added");
                }
            }
        }

        /// <summary>
        /// Trigger a quip generation
        /// </summary>
        public void TriggerQuip(string triggerReason = "timer")
        {
            if (isGenerating) return;
            if (openAIClient == null)
            {
                Debug.LogWarning("[ShopAssistant] OpenAIClient not found!");
                return;
            }

            isGenerating = true;
            string context = GatherContext(triggerReason);
            
            Debug.Log($"[ShopAssistant] Generating quip... Trigger: {triggerReason}");
            Debug.Log($"[ShopAssistant] Context: {context}");

            openAIClient.GenerateAndSpeak(
                personalityPrompt,
                context,
                OnQuipGenerated,
                OnQuipError
            );
        }

        private void OnQuipGenerated(string text, AudioClip audio)
        {
            isGenerating = false;
            lastQuip = text;
            
            // Schedule next quip
            nextQuipTime = Time.time + quipInterval + Random.Range(-quipRandomness, quipRandomness);
            
            Debug.Log($"[ShopAssistant] Quip: {text}");

            // Play the audio
            if (audio != null && audioSource != null)
            {
                audioSource.clip = audio;
                audioSource.Play();
            }
        }

        private void OnQuipError(string error)
        {
            isGenerating = false;
            Debug.LogError($"[ShopAssistant] Error generating quip: {error}");
            
            // Try again later
            nextQuipTime = Time.time + minQuipInterval;
        }

        private string GatherContext(string triggerReason)
        {
            StringBuilder context = new StringBuilder();

            // What triggered this quip
            context.AppendLine($"Trigger: {GetTriggerDescription(triggerReason)}");

            // Cart contents
            context.AppendLine(GetCartContext());

            // What user is looking at
            context.AppendLine(GetViewContext());

            // Nearby products
            context.AppendLine(GetNearbyProductsContext());

            // Shopping behavior
            context.AppendLine(GetBehaviorContext());

            context.AppendLine("\nGenerate a brief, natural quip based on this context. Keep it to 1-2 sentences.");

            return context.ToString();
        }

        private string GetTriggerDescription(string trigger)
        {
            switch (trigger)
            {
                case "item_added": return "The shopper just added an item to their cart.";
                case "item_removed": return "The shopper just removed an item from their cart.";
                case "timer": return "Some time has passed, make a casual observation.";
                case "greeting": return "The shopper just entered the store, greet them briefly.";
                default: return "Make a casual observation about the shopping experience.";
            }
        }

        private string GetCartContext()
        {
            if (basketTrigger == null) return "Cart: Unknown";

            var items = basketTrigger.CollectedItems;
            if (items.Count == 0)
            {
                return "Cart: Empty (shopper hasn't added anything yet)";
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Cart ({items.Count} items):");
            
            float totalPrice = 0f;
            foreach (var item in items)
            {
                if (item != null)
                {
                    sb.AppendLine($"  - {item.displayName} (${item.price:F2})");
                    totalPrice += item.price;
                }
            }
            sb.AppendLine($"  Total: ${totalPrice:F2}");
            
            return sb.ToString();
        }

        private string GetViewContext()
        {
            if (playerCamera == null) return "View: Unknown";

            // Raycast to see what player is looking at
            RaycastHit hit;
            if (Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, 20f))
            {
                // Check if looking at a product
                var basketItem = hit.collider.GetComponentInParent<BasketItem>();
                if (basketItem != null)
                {
                    return $"Looking at: {basketItem.displayName} (${basketItem.price:F2})";
                }

                // Check for other interesting objects
                string objName = hit.collider.gameObject.name;
                if (objName.ToLower().Contains("product") || objName.ToLower().Contains("display"))
                {
                    return $"Looking at: A product display";
                }
                if (objName.ToLower().Contains("shelf"))
                {
                    return $"Looking at: Store shelves";
                }

                return $"Looking at: {objName}";
            }

            return "Looking at: Nothing specific (browsing around)";
        }

        private string GetNearbyProductsContext()
        {
            if (playerCamera == null) return "Nearby: Unknown";

            var allItems = FindObjectsOfType<BasketItem>();
            List<BasketItem> nearbyItems = new List<BasketItem>();
            List<BasketItem> inViewItems = new List<BasketItem>();

            foreach (var item in allItems)
            {
                if (item == null || item.isCollected) continue;

                float distance = Vector3.Distance(playerCamera.position, item.transform.position);
                if (distance <= nearbyProductRange)
                {
                    nearbyItems.Add(item);

                    // Check if in field of view
                    Vector3 dirToItem = (item.transform.position - playerCamera.position).normalized;
                    float angle = Vector3.Angle(playerCamera.forward, dirToItem);
                    if (angle <= viewAngle)
                    {
                        inViewItems.Add(item);
                    }
                }
            }

            StringBuilder sb = new StringBuilder();
            
            if (inViewItems.Count > 0)
            {
                sb.Append($"Products in view ({inViewItems.Count}): ");
                for (int i = 0; i < Mathf.Min(3, inViewItems.Count); i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(inViewItems[i].displayName);
                }
                if (inViewItems.Count > 3) sb.Append($" and {inViewItems.Count - 3} more");
            }
            else if (nearbyItems.Count > 0)
            {
                sb.Append($"Products nearby ({nearbyItems.Count}): ");
                for (int i = 0; i < Mathf.Min(3, nearbyItems.Count); i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(nearbyItems[i].displayName);
                }
            }
            else
            {
                sb.Append("No products nearby");
            }

            return sb.ToString();
        }

        private string GetBehaviorContext()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Behavior: ");

            if (playerCamera == null)
            {
                sb.Append("Unknown");
                return sb.ToString();
            }

            // Check if player has moved much
            float distanceMoved = Vector3.Distance(playerCamera.position, lastPlayerPosition);
            lastPlayerPosition = playerCamera.position;

            if (distanceMoved < 0.1f)
            {
                sb.Append("Standing still, examining products. ");
            }
            else if (distanceMoved < 1f)
            {
                sb.Append("Slowly browsing. ");
            }
            else
            {
                sb.Append("Actively moving around the store. ");
            }

            // Cart fullness
            if (basketTrigger != null)
            {
                if (basketTrigger.ItemCount == 0)
                    sb.Append("Cart is empty.");
                else if (basketTrigger.ItemCount < 3)
                    sb.Append("Just started shopping.");
                else if (basketTrigger.ItemCount < 6)
                    sb.Append("Building up a nice haul.");
                else
                    sb.Append("Serious shopper with a full cart!");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Manually trigger a greeting quip
        /// </summary>
        public void Greet()
        {
            TriggerQuip("greeting");
        }

        /// <summary>
        /// Test button - call from Inspector context menu
        /// </summary>
        [ContextMenu("Test Quip Now")]
        public void TestQuipNow()
        {
            Debug.Log("[ShopAssistant] Manual test triggered!");
            TriggerQuip("timer");
        }

        /// <summary>
        /// Test greeting - call from Inspector context menu
        /// </summary>
        [ContextMenu("Test Greeting")]
        public void TestGreeting()
        {
            Debug.Log("[ShopAssistant] Greeting test triggered!");
            Greet();
        }

        /// <summary>
        /// Get the last generated quip text
        /// </summary>
        public string LastQuip => lastQuip;

        /// <summary>
        /// Check if currently generating/speaking
        /// </summary>
        public bool IsBusy => isGenerating || (audioSource != null && audioSource.isPlaying);
    }
}

