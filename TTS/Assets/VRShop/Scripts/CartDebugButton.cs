using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace VRShop
{
    /// <summary>
    /// A VR-pressable button that prints all shopping cart contents to the Unity console
    /// and sends an SMS via Twilio with the cart summary.
    /// Uses proximity detection with controller trigger press (same as GrabbableItem).
    /// </summary>
    public class CartDebugButton : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the basket trigger containing cart items. If null, will auto-find.")]
        [SerializeField] private BasketTrigger basketTrigger;

        [Header("Controller References")]
        [SerializeField] private Transform leftController;
        [SerializeField] private Transform rightController;

        [Header("Twilio Settings")]
        [Tooltip("Twilio Account SID")]
        [SerializeField] private string twilioAccountSid = "AC099450bb6800b8156b6634b79d8ad75c";
        
        [Tooltip("Twilio Auth Token")]
        [SerializeField] private string twilioAuthToken = "";
        
        [Tooltip("Phone number to send SMS to (with country code)")]
        [SerializeField] private string toPhoneNumber = "+15195052596";
        
        [Tooltip("Twilio Messaging Service SID")]
        [SerializeField] private string messagingServiceSid = "MG6eb2f38f890b0e3aad8396b62985ab83";

        [Header("Button Settings")]
        [Tooltip("Distance from controller to activate button")]
        [SerializeField] private float activationDistance = 0.15f;
        
        [Tooltip("Cooldown between button presses to prevent spam")]
        [SerializeField] private float pressCooldown = 0.5f;
        
        [Tooltip("Visual feedback color when pressed")]
        [SerializeField] private Color pressedColor = new Color(0.2f, 0.8f, 0.2f);
        
        [Tooltip("Normal button color")]
        [SerializeField] private Color normalColor = new Color(0.8f, 0.2f, 0.2f);
        
        [Tooltip("Hover color when controller is near")]
        [SerializeField] private Color hoverColor = new Color(1f, 0.6f, 0.2f);

        [Header("Auto-Setup")]
        [Tooltip("Automatically create visual button on Start")]
        [SerializeField] private bool autoSetupVisuals = true;
        
        [Tooltip("Size of the button")]
        [SerializeField] private Vector3 buttonSize = new Vector3(0.15f, 0.15f, 0.05f);
        
        [Tooltip("Auto-position button on wall facing camera at start")]
        [SerializeField] private bool autoPositionOnWall = true;
        
        [Tooltip("Z position for button (behind camera, between -5 and -7)")]
        [SerializeField] private float wallDistance = -6f;
        
        [Tooltip("Height above floor for button placement (0 = use camera height)")]
        [SerializeField] private float buttonHeight = 1.6f;
        
        [Tooltip("Show text label on button")]
        [SerializeField] private bool showLabel = true;
        
        [Tooltip("Label text to display")]
        [SerializeField] private string labelText = "Check out grab here";
        
        [Tooltip("Label offset above button")]
        [SerializeField] private Vector3 labelOffset = new Vector3(0f, 0.12f, 0f);
        
        [Tooltip("Label font size")]
        [SerializeField] private float labelFontSize = 0.25f;

        private float lastPressTime;
        private GameObject labelObject;
        private Renderer buttonRenderer;
        private Material buttonMaterial;
        private bool isPressed;
        private bool wasLeftNear;
        private bool wasRightNear;
        private bool isSendingSMS;

        private void Start()
        {
            Debug.Log("[CartDebugButton] Initializing...");
            
            // Auto-position on wall facing camera if enabled
            if (autoPositionOnWall)
            {
                PositionOnWallFacingCamera();
            }
            
            // Auto-find basket trigger if not assigned
            if (basketTrigger == null)
            {
                basketTrigger = FindObjectOfType<BasketTrigger>();
                if (basketTrigger == null)
                {
                    Debug.LogWarning("[CartDebugButton] BasketTrigger not found in scene!");
                }
                else
                {
                    Debug.Log("[CartDebugButton] Found BasketTrigger: " + basketTrigger.gameObject.name);
                }
            }

            // Find controllers
            FindControllers();

            // Setup visuals if requested
            if (autoSetupVisuals)
            {
                SetupButtonVisuals();
            }
            else
            {
                // Use existing renderer
                buttonRenderer = GetComponent<Renderer>();
                if (buttonRenderer != null)
                {
                    buttonMaterial = buttonRenderer.material;
                    buttonMaterial.color = normalColor;
                }
            }
            
            // Setup text label if enabled
            if (showLabel)
            {
                SetupButtonLabel();
            }

            // Warn if auth token is missing
            if (string.IsNullOrEmpty(twilioAuthToken))
            {
                Debug.LogWarning("[CartDebugButton] Twilio Auth Token is not set! SMS will fail.");
            }

            Debug.Log("[CartDebugButton] Ready! Position: " + transform.position);
        }

        private void FindControllers()
        {
            if (leftController == null || rightController == null)
            {
                var cameraRig = FindObjectOfType<OVRCameraRig>();
                if (cameraRig != null)
                {
                    if (leftController == null)
                    {
                        leftController = cameraRig.leftHandAnchor;
                        Debug.Log("[CartDebugButton] Found left controller: " + leftController.name);
                    }
                    if (rightController == null)
                    {
                        rightController = cameraRig.rightHandAnchor;
                        Debug.Log("[CartDebugButton] Found right controller: " + rightController.name);
                    }
                }
                else
                {
                    Debug.LogWarning("[CartDebugButton] OVRCameraRig not found! Controller detection won't work.");
                }
            }
        }

        /// <summary>
        /// Positions the button on the wall directly facing the VR camera at startup
        /// </summary>
        private void PositionOnWallFacingCamera()
        {
            Transform cameraTransform = null;
            
            // Try to find OVR camera rig first (for VR)
            var cameraRig = FindObjectOfType<OVRCameraRig>();
            if (cameraRig != null && cameraRig.centerEyeAnchor != null)
            {
                cameraTransform = cameraRig.centerEyeAnchor;
                Debug.Log("[CartDebugButton] Found OVRCameraRig center eye anchor");
            }
            else
            {
                // Fallback to main camera
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    cameraTransform = mainCamera.transform;
                    Debug.Log("[CartDebugButton] Using Camera.main as reference");
                }
            }

            if (cameraTransform == null)
            {
                Debug.LogWarning("[CartDebugButton] Could not find VR camera! Button will use default position.");
                return;
            }

            // Get camera position and forward direction
            Vector3 cameraPos = cameraTransform.position;
            
            // Position behind camera (negative Z direction, between -5 and -7)
            // Clamp wallDistance to be between -5 and -7
            float wallZ = Mathf.Clamp(wallDistance, -7f, -5f);
            
            // Use camera's X position for horizontal alignment, or center if camera is at origin
            float buttonX = Mathf.Abs(cameraPos.x) < 0.1f ? 0f : cameraPos.x;
            
            // Use configured height or camera height
            float height = buttonHeight > 0 ? buttonHeight : cameraPos.y;
            
            // Position button behind camera
            Vector3 buttonPosition = new Vector3(buttonX, height, wallZ);
            transform.position = buttonPosition;
            
            // Make button face the camera from behind (look at camera position, but keep it upright)
            Vector3 directionToCamera = (cameraPos - buttonPosition).normalized;
            // Project direction onto XZ plane to keep button upright
            directionToCamera.y = 0;
            if (directionToCamera.magnitude > 0.01f)
            {
                // Face towards camera (from behind)
                transform.rotation = Quaternion.LookRotation(directionToCamera, Vector3.up);
            }
            else
            {
                // If camera is directly in front, face forward (positive Z)
                transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            }
            
            Debug.Log($"[CartDebugButton] Positioned behind camera at {buttonPosition}, facing camera at {cameraPos}");
        }

        private void SetupButtonVisuals()
        {
            // Add mesh filter and renderer if not present
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
                meshFilter.mesh = CreateButtonMesh();
            }

            buttonRenderer = GetComponent<Renderer>();
            if (buttonRenderer == null)
            {
                buttonRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            // Create material
            buttonMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (buttonMaterial.shader == null || buttonMaterial.shader.name == "Hidden/InternalErrorShader")
            {
                buttonMaterial = new Material(Shader.Find("Standard"));
            }
            buttonMaterial.color = normalColor;
            buttonRenderer.material = buttonMaterial;
        }

        /// <summary>
        /// Creates a text label above the button
        /// </summary>
        private void SetupButtonLabel()
        {
            // Remove existing label if present
            if (labelObject != null)
            {
                Destroy(labelObject);
            }

            // Create label GameObject
            labelObject = new GameObject("ButtonLabel");
            labelObject.transform.SetParent(transform);
            labelObject.transform.localPosition = labelOffset;
            labelObject.transform.localRotation = Quaternion.identity;
            // Mirror the label by flipping X scale (so it's readable from behind)
            labelObject.transform.localScale = new Vector3(-1f, 1f, 1f);

            // Add TextMesh component
            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = labelText;
            textMesh.fontSize = (int)(labelFontSize * 100);
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
            textMesh.characterSize = 0.1f;
            textMesh.fontStyle = FontStyle.Bold;

            Debug.Log($"[CartDebugButton] Created mirrored label: '{labelText}'");
        }

        private Mesh CreateButtonMesh()
        {
            // Create a simple cube mesh for the button
            var mesh = new Mesh();
            
            Vector3 size = buttonSize;
            Vector3 half = size * 0.5f;

            Vector3[] vertices = new Vector3[]
            {
                // Front face
                new Vector3(-half.x, -half.y, half.z),
                new Vector3(half.x, -half.y, half.z),
                new Vector3(half.x, half.y, half.z),
                new Vector3(-half.x, half.y, half.z),
                // Back face
                new Vector3(half.x, -half.y, -half.z),
                new Vector3(-half.x, -half.y, -half.z),
                new Vector3(-half.x, half.y, -half.z),
                new Vector3(half.x, half.y, -half.z),
                // Top face
                new Vector3(-half.x, half.y, half.z),
                new Vector3(half.x, half.y, half.z),
                new Vector3(half.x, half.y, -half.z),
                new Vector3(-half.x, half.y, -half.z),
                // Bottom face
                new Vector3(-half.x, -half.y, -half.z),
                new Vector3(half.x, -half.y, -half.z),
                new Vector3(half.x, -half.y, half.z),
                new Vector3(-half.x, -half.y, half.z),
                // Left face
                new Vector3(-half.x, -half.y, -half.z),
                new Vector3(-half.x, -half.y, half.z),
                new Vector3(-half.x, half.y, half.z),
                new Vector3(-half.x, half.y, -half.z),
                // Right face
                new Vector3(half.x, -half.y, half.z),
                new Vector3(half.x, -half.y, -half.z),
                new Vector3(half.x, half.y, -half.z),
                new Vector3(half.x, half.y, half.z),
            };

            int[] triangles = new int[]
            {
                0, 2, 1, 0, 3, 2,       // Front
                4, 6, 5, 4, 7, 6,       // Back
                8, 10, 9, 8, 11, 10,    // Top
                12, 14, 13, 12, 15, 14, // Bottom
                16, 18, 17, 16, 19, 18, // Left
                20, 22, 21, 20, 23, 22  // Right
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private void Update()
        {
            CheckControllerInteraction();

            // Keyboard press for testing in editor
            if (Input.GetKeyDown(KeyCode.P))
            {
                Debug.Log("[CartDebugButton] Keyboard 'P' pressed - printing cart and sending SMS...");
                OnButtonPressed();
            }

            // Reset button color after press
            if (isPressed && Time.time - lastPressTime > 0.2f)
            {
                isPressed = false;
            }

            UpdateButtonColor();
            
            // Update label to face camera (billboard effect)
            if (labelObject != null)
            {
                UpdateLabelRotation();
            }
        }
        
        /// <summary>
        /// Makes the label always face the camera (billboard effect)
        /// </summary>
        private void UpdateLabelRotation()
        {
            Transform cameraTransform = null;
            
            // Try to find OVR camera rig first (for VR)
            var cameraRig = FindObjectOfType<OVRCameraRig>();
            if (cameraRig != null && cameraRig.centerEyeAnchor != null)
            {
                cameraTransform = cameraRig.centerEyeAnchor;
            }
            else
            {
                // Fallback to main camera
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    cameraTransform = mainCamera.transform;
                }
            }

            if (cameraTransform != null)
            {
                // Make label face camera
                Vector3 directionToCamera = cameraTransform.position - labelObject.transform.position;
                if (directionToCamera.magnitude > 0.01f)
                {
                    labelObject.transform.rotation = Quaternion.LookRotation(-directionToCamera, Vector3.up);
                }
            }
        }

        private void CheckControllerInteraction()
        {
            bool leftNear = IsControllerNear(leftController);
            bool rightNear = IsControllerNear(rightController);

            // Check for trigger press while near button
            // Method 1: Trigger just pressed while near
            if (rightNear && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                TryPressButton(OVRInput.Controller.RTouch);
            }
            else if (leftNear && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
            {
                TryPressButton(OVRInput.Controller.LTouch);
            }
            // Method 2: Controller moved into range while trigger held
            else if (rightNear && !wasRightNear && OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                TryPressButton(OVRInput.Controller.RTouch);
            }
            else if (leftNear && !wasLeftNear && OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
            {
                TryPressButton(OVRInput.Controller.LTouch);
            }
            // Method 3: Also support grip trigger for accessibility
            else if (rightNear && OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
            {
                TryPressButton(OVRInput.Controller.RTouch);
            }
            else if (leftNear && OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
            {
                TryPressButton(OVRInput.Controller.LTouch);
            }
            // Method 4: A/X button press while near
            else if (rightNear && OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                TryPressButton(OVRInput.Controller.RTouch);
            }
            else if (leftNear && OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
            {
                TryPressButton(OVRInput.Controller.LTouch);
            }

            wasLeftNear = leftNear;
            wasRightNear = rightNear;
        }

        private bool IsControllerNear(Transform controller)
        {
            if (controller == null) return false;
            
            float distance = Vector3.Distance(controller.position, transform.position);
            return distance < activationDistance;
        }

        private void UpdateButtonColor()
        {
            if (buttonMaterial == null) return;

            if (isPressed || isSendingSMS)
            {
                buttonMaterial.color = pressedColor;
            }
            else if (IsControllerNear(leftController) || IsControllerNear(rightController))
            {
                buttonMaterial.color = hoverColor;
            }
            else
            {
                buttonMaterial.color = normalColor;
            }
        }

        private void TryPressButton(OVRInput.Controller controller)
        {
            if (Time.time - lastPressTime < pressCooldown)
                return;

            if (isSendingSMS)
            {
                Debug.Log("[CartDebugButton] Already sending SMS, please wait...");
                return;
            }

            lastPressTime = Time.time;
            isPressed = true;

            Debug.Log("[CartDebugButton] Button pressed by " + controller);

            // Haptic feedback
            OVRInput.SetControllerVibration(0.5f, 0.5f, controller);

            // Execute button action
            OnButtonPressed();
        }

        private void OnButtonPressed()
        {
            // Try to sync items that might be in basket but not in CollectedItems list
            SyncBasketItems();
            
            // Print cart contents to console
            PrintCartContents();
            
            // Send SMS via Twilio
            string cartSummary = GetCartSummaryForSMS();
            StartCoroutine(SendTwilioSMS(cartSummary));
        }

        /// <summary>
        /// Syncs items that have isCollected=true but aren't in the BasketTrigger's CollectedItems list.
        /// This can happen if items are added manually or if the trigger detection fails.
        /// </summary>
        private void SyncBasketItems()
        {
            if (basketTrigger == null)
            {
                basketTrigger = FindObjectOfType<BasketTrigger>();
                if (basketTrigger == null)
                {
                    Debug.LogWarning("[CartDebugButton] Cannot sync - BasketTrigger not found");
                    return;
                }
            }

            // Find all items that are marked as collected but not in the list
            var allItems = FindObjectsOfType<BasketItem>();
            int syncedCount = 0;

            foreach (var item in allItems)
            {
                if (item != null && item.isCollected && !basketTrigger.ContainsItem(item))
                {
                    // Item is marked as collected but not in the list
                    // Reset the flag and re-add it properly via CollectItem
                    Debug.Log($"[CartDebugButton] Syncing item: {item.displayName} (was missing from CollectedItems)");
                    
                    // Temporarily reset the flag so CollectItem will accept it
                    item.isCollected = false;
                    basketTrigger.CollectItem(item);
                    syncedCount++;
                }
            }

            if (syncedCount > 0)
            {
                Debug.Log($"[CartDebugButton] Synced {syncedCount} item(s) to basket");
            }
        }

        /// <summary>
        /// Gets a concise cart summary suitable for SMS
        /// </summary>
        private string GetCartSummaryForSMS()
        {
            // Ensure we have a basket trigger reference
            if (basketTrigger == null)
            {
                Debug.LogWarning("[CartDebugButton] BasketTrigger is null, trying to find it...");
                basketTrigger = FindObjectOfType<BasketTrigger>();
                if (basketTrigger == null)
                {
                    Debug.LogError("[CartDebugButton] Cannot find BasketTrigger in scene!");
                    return "VR Shop Order: Unable to read cart (BasketTrigger not found)";
                }
                Debug.Log($"[CartDebugButton] Found BasketTrigger: {basketTrigger.gameObject.name}");
            }

            var items = basketTrigger.CollectedItems;
            
            Debug.Log($"[CartDebugButton] GetCartSummaryForSMS - ItemCount: {basketTrigger.ItemCount}, CollectedItems.Count: {items.Count}");

            // Fallback: If CollectedItems is empty but there might be items with isCollected=true
            if (items.Count == 0)
            {
                // Try to find all BasketItems with isCollected = true as a fallback
                var allItems = FindObjectsOfType<BasketItem>();
                int collectedCount = 0;
                foreach (var item in allItems)
                {
                    if (item != null && item.isCollected)
                    {
                        collectedCount++;
                    }
                }
                
                Debug.Log($"[CartDebugButton] Fallback check: Found {collectedCount} items with isCollected=true");
                
                if (collectedCount == 0)
                {
                    Debug.LogWarning("[CartDebugButton] Cart appears to be empty!");
                    return "VR Shop Order: Cart is empty!";
                }
                else
                {
                    // Items exist but aren't in CollectedItems list - use fallback detection
                    Debug.LogWarning($"[CartDebugButton] Found {collectedCount} items with isCollected=true but not in CollectedItems list!");
                    Debug.LogWarning("[CartDebugButton] This might indicate items were added manually. Using fallback detection.");
                    
                    return BuildSMSMessageWithURLs(allItems.Where(item => item != null && item.isCollected).ToList());
                }
            }

            // Normal path: items are in CollectedItems list
            return BuildSMSMessageWithURLs(items);
        }

        /// <summary>
        /// Builds SMS message with product descriptions and URLs based on items in cart
        /// </summary>
        private string BuildSMSMessageWithURLs(IEnumerable<BasketItem> items)
        {
            // Check for specific items by looking at itemId, displayName, or gameObject.name
            bool hasSample = false;
            bool hasTextureMesh1 = false;
            bool hasKeyboard = false;
            bool hasSnowboard = false;
            
            foreach (var item in items)
            {
                if (item == null) continue;
                
                string identifier = (item.itemId ?? "").ToLower() + 
                                   (item.displayName ?? "").ToLower() + 
                                   (item.gameObject.name ?? "").ToLower();
                
                if (identifier.Contains("sample"))
                {
                    hasSample = true;
                    Debug.Log($"[CartDebugButton] Found 'sample' item: {item.gameObject.name}");
                }
                
                if (identifier.Contains("textured_mesh_1"))
                {
                    hasTextureMesh1 = true;
                    Debug.Log($"[CartDebugButton] Found 'textured_mesh_1' item: {item.gameObject.name}");
                }
                
                if (identifier.Contains("keyboard"))
                {
                    hasKeyboard = true;
                    Debug.Log($"[CartDebugButton] Found 'keyboard' item: {item.gameObject.name}");
                }
                
                if (identifier.Contains("10535_snowboard_v1_l3"))
                {
                    hasSnowboard = true;
                    Debug.Log($"[CartDebugButton] Found '10535_Snowboard_v1_L3' item: {item.gameObject.name}");
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("🛒 VR Shop Order:");
            sb.AppendLine();

            // Build product descriptions and URLs based on what's in cart
            // Order items consistently for URL composition
            List<string> cartUrls = new List<string>();

            // Add items in a consistent order (alphabetical by product name for consistency)
            if (hasKeyboard)
            {
                sb.AppendLine("Shopify Keyboard K3 Max");
                sb.AppendLine("$149.99");
                sb.AppendLine();
                cartUrls.Add("51598421852216:1");
            }

            if (hasSample)
            {
                sb.AppendLine("LEGO UofT Hacks Deer");
                sb.AppendLine("$119.99");
                sb.AppendLine();
                cartUrls.Add("51598421917752:1");
            }

            if (hasSnowboard)
            {
                sb.AppendLine("Snow Devil - Snowboard");
                sb.AppendLine("$299.00");
                sb.AppendLine();
                cartUrls.Add("51598421688376:1");
            }

            if (hasTextureMesh1)
            {
                sb.AppendLine("Shopify Swag T-Shirt");
                sb.AppendLine("1");
                sb.AppendLine("$89.99");
                sb.AppendLine();
                cartUrls.Add("51598421950520:1");
            }

            // If no specific items found, fall back to generic listing
            if (!hasSample && !hasTextureMesh1 && !hasKeyboard && !hasSnowboard)
            {
                float totalPrice = 0f;
                int validItemCount = 0;
                foreach (var item in items)
                {
                    if (item != null)
                    {
                        string itemName = !string.IsNullOrEmpty(item.displayName) ? item.displayName : item.gameObject.name;
                        float itemPrice = item.price > 0 ? item.price : 0f;
                        sb.AppendLine($"• {itemName} - ${itemPrice:F2}");
                        totalPrice += itemPrice;
                        validItemCount++;
                    }
                }
                sb.AppendLine();
                sb.AppendLine($"Total: ${totalPrice:F2}");
                sb.AppendLine($"Items: {validItemCount}");
                
                return sb.ToString();
            }

            // Add URL(s) to message - Shopify format: baseUrl + itemId1:quantity,itemId2:quantity,...
            if (cartUrls.Count > 0)
            {
                sb.AppendLine();
                string baseUrl = "https://legoheaven.myshopify.com/cart/";
                // Join all item IDs with commas (Shopify checkout URL format)
                string urlParams = string.Join(",", cartUrls);
                string fullUrl = baseUrl + urlParams;
                sb.AppendLine(fullUrl);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Sends an SMS via Twilio API
        /// </summary>
        private IEnumerator SendTwilioSMS(string messageBody)
        {
            if (string.IsNullOrEmpty(twilioAuthToken))
            {
                Debug.LogError("[CartDebugButton] Twilio Auth Token is not set! Cannot send SMS.");
                yield break;
            }

            isSendingSMS = true;
            Debug.Log("[CartDebugButton] Sending SMS via Twilio...");

            // Build URL exactly as in curl command
            string url = $"https://api.twilio.com/2010-04-01/Accounts/{twilioAccountSid}/Messages.json";
            Debug.Log($"[CartDebugButton] URL: {url}");

            // Create form data (WWWForm automatically URL-encodes, matching --data-urlencode)
            WWWForm form = new WWWForm();
            form.AddField("To", toPhoneNumber);
            form.AddField("Body", messageBody);
            form.AddField("MessagingServiceSid", messagingServiceSid);

            using (UnityWebRequest request = UnityWebRequest.Post(url, form))
            {
                // Add Basic Auth header (matches -u AccountSid:AuthToken in curl)
                string auth = twilioAccountSid + ":" + twilioAuthToken;
                string authBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(auth));
                request.SetRequestHeader("Authorization", "Basic " + authBase64);

                Debug.Log($"[CartDebugButton] Auth: {twilioAccountSid}:***");
                Debug.Log($"[CartDebugButton] To: {toPhoneNumber}");
                Debug.Log($"[CartDebugButton] Body: {messageBody}");
                Debug.Log($"[CartDebugButton] MessagingServiceSid: {messagingServiceSid}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[CartDebugButton] ✅ SMS sent successfully!");
                    Debug.Log("[CartDebugButton] Response: " + request.downloadHandler.text);
                }
                else
                {
                    Debug.LogError($"[CartDebugButton] ❌ Failed to send SMS: {request.error}");
                    Debug.LogError($"[CartDebugButton] Response Code: {request.responseCode}");
                    Debug.LogError($"[CartDebugButton] Response: {request.downloadHandler.text}");
                }
            }

            isSendingSMS = false;
        }

        /// <summary>
        /// Prints all items in the shopping cart to Unity console logs.
        /// </summary>
        public void PrintCartContents()
        {
            Debug.Log("========== SHOPPING CART CONTENTS ==========");

            if (basketTrigger == null)
            {
                Debug.LogError("[CartDebugButton] No BasketTrigger reference - cannot read cart!");
                // Try to find it again
                basketTrigger = FindObjectOfType<BasketTrigger>();
                if (basketTrigger == null)
                {
                    Debug.LogError("[CartDebugButton] Still cannot find BasketTrigger in scene!");
                    return;
                }
                Debug.Log("[CartDebugButton] Found BasketTrigger on retry: " + basketTrigger.gameObject.name);
            }

            var items = basketTrigger.CollectedItems;
            
            Debug.Log($"[CartDebugButton] BasketTrigger.ItemCount = {basketTrigger.ItemCount}");
            Debug.Log($"[CartDebugButton] CollectedItems.Count = {items.Count}");

            if (items.Count == 0)
            {
                Debug.Log("[CartDebugButton] Cart is EMPTY - no items collected yet.");
                Debug.Log("============================================");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Cart contains {items.Count} item(s):");
            sb.AppendLine();

            float totalPrice = 0f;
            int index = 1;

            foreach (var item in items)
            {
                if (item != null)
                {
                    sb.AppendLine($"  {index}. {item.displayName}");
                    sb.AppendLine($"     ID: {item.itemId}");
                    sb.AppendLine($"     Price: ${item.price:F2}");
                    sb.AppendLine($"     GameObject: {item.gameObject.name}");
                    sb.AppendLine($"     Position: {item.transform.localPosition}");
                    sb.AppendLine();
                    
                    totalPrice += item.price;
                    index++;
                }
                else
                {
                    sb.AppendLine($"  {index}. [NULL ITEM - was destroyed]");
                    index++;
                }
            }

            sb.AppendLine($"  --------------------------------");
            sb.AppendLine($"  TOTAL: ${totalPrice:F2}");
            
            Debug.Log(sb.ToString());
            Debug.Log("============================================");
        }

        private void OnDestroy()
        {
            // Clean up material
            if (buttonMaterial != null)
            {
                Destroy(buttonMaterial);
            }
        }

        // Draw gizmo to show activation radius in editor
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, activationDistance);
        }
    }
}
