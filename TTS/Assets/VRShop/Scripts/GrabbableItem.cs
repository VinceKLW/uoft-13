using UnityEngine;

namespace VRShop
{
    /// <summary>
    /// Handles grabbing items with Quest controllers.
    /// Items can be grabbed from shelves or from inside the basket.
    /// When released over the basket, items are collected; otherwise they drop with gravity.
    /// </summary>
    public class GrabbableItem : MonoBehaviour
    {
        [Header("Grab Settings")]
        [Tooltip("Maximum distance from controller to grab an item")]
        public float grabDistance = 0.3f;
        
        [Tooltip("Offset from controller when item is held")]
        public Vector3 holdOffset = new Vector3(0f, 0f, 0.1f);

        [Header("Controller References")]
        public Transform leftController;
        public Transform rightController;

        [Header("Basket Reference")]
        public BasketTrigger basketTrigger;
        public GrabbableBasket grabbableBasket;

        private BasketItem heldItem;
        private Transform holdingController;
        private OVRInput.Controller holdingControllerType;

        // Track if each controller was near a grabbable item last frame
        // This allows grabbing when moving hand INTO range while holding grip
        private bool leftWasNearItem = false;
        private bool rightWasNearItem = false;

        private void Start()
        {
            FindControllers();
            FindBasketComponents();
        }

        private void FindControllers()
        {
            if (leftController == null || rightController == null)
            {
                var cameraRig = FindObjectOfType<OVRCameraRig>();
                if (cameraRig != null)
                {
                    if (leftController == null)
                        leftController = cameraRig.leftHandAnchor;
                    if (rightController == null)
                        rightController = cameraRig.rightHandAnchor;
                }
            }
        }

        private void FindBasketComponents()
        {
            if (basketTrigger == null)
                basketTrigger = FindObjectOfType<BasketTrigger>();
            if (grabbableBasket == null)
                grabbableBasket = FindObjectOfType<GrabbableBasket>();
        }

        private void Update()
        {
            if (heldItem != null)
                HandleHeldItem();
            else
                CheckForGrab();
        }

        private void CheckForGrab()
        {
            // Use Get (continuous) instead of GetDown (single frame)
            // This allows grabbing when moving hand INTO item range while already holding grip
            bool leftGrip = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);
            bool rightGrip = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);

            // Check which controller is holding the basket
            bool leftHoldingBasket = false;
            bool rightHoldingBasket = false;
            
            if (grabbableBasket != null && grabbableBasket.IsGrabbed)
            {
                var basketController = grabbableBasket.GrabbingController;
                if (basketController == leftController)
                    leftHoldingBasket = true;
                else if (basketController == rightController)
                    rightHoldingBasket = true;
            }

            // Check if each controller is currently near any grabbable item
            bool leftNearItem = leftController != null && IsControllerNearItem(leftController);
            bool rightNearItem = rightController != null && IsControllerNearItem(rightController);

            // Grab when: grip held AND (just entered item range OR just pressed grip while near)
            // This handles both: moving hand to item while holding grip, AND pressing grip while near item
            
            // Right controller
            if (rightGrip && !rightHoldingBasket && rightNearItem && !rightWasNearItem)
            {
                TryGrabNearestItem(rightController, OVRInput.Controller.RTouch);
            }
            // Also check for fresh grip press while already near item
            else if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch) 
                     && !rightHoldingBasket && rightNearItem)
            {
                TryGrabNearestItem(rightController, OVRInput.Controller.RTouch);
            }

            // Left controller
            if (leftGrip && !leftHoldingBasket && leftNearItem && !leftWasNearItem)
            {
                TryGrabNearestItem(leftController, OVRInput.Controller.LTouch);
            }
            // Also check for fresh grip press while already near item
            else if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch) 
                     && !leftHoldingBasket && leftNearItem)
            {
                TryGrabNearestItem(leftController, OVRInput.Controller.LTouch);
            }

            // Update "was near" state for next frame
            leftWasNearItem = leftNearItem;
            rightWasNearItem = rightNearItem;
        }

        private bool IsControllerNearItem(Transform controller)
        {
            if (controller == null) return false;

            var allItems = FindObjectsOfType<BasketItem>();
            
            foreach (var item in allItems)
            {
                if (item == null || item.isHeld) continue;
                if (!item.canBeCollected && !item.isCollected) continue;

                float distance = Vector3.Distance(controller.position, item.transform.position);
                float checkDistance = item.isCollected ? grabDistance * 1.5f : grabDistance;
                
                if (distance < checkDistance)
                    return true;
            }
            
            return false;
        }

        private void TryGrabNearestItem(Transform controller, OVRInput.Controller controllerType)
        {
            BasketItem nearestItem = null;
            float nearestDistance = grabDistance * 1.5f; // Max possible grab distance (for basket items)

            var allItems = FindObjectsOfType<BasketItem>();
            
            foreach (var item in allItems)
            {
                if (item == null || item.isHeld) continue;
                if (!item.canBeCollected && !item.isCollected) continue;

                float distance = Vector3.Distance(controller.position, item.transform.position);
                
                // Use larger grab radius for items in basket (easier to grab)
                float checkDistance = item.isCollected ? grabDistance * 1.5f : grabDistance;
                
                if (distance < checkDistance && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestItem = item;
                }
            }

            if (nearestItem != null)
            {
                GrabItem(nearestItem, controller, controllerType);
            }
        }

        private void GrabItem(BasketItem item, Transform controller, OVRInput.Controller controllerType)
        {
            heldItem = item;
            holdingController = controller;
            holdingControllerType = controllerType;

            // Remove from basket if it was collected
            if (item.isCollected && basketTrigger != null)
            {
                basketTrigger.RemoveItem(item);
            }

            item.OnGrabbed();
            
            item.transform.SetParent(controller);
            item.transform.localPosition = holdOffset;
            item.transform.localRotation = Quaternion.identity;
            item.transform.localScale = Vector3.one;

            OVRInput.SetControllerVibration(0.2f, 0.2f, controllerType);
        }

        private void HandleHeldItem()
        {
            bool gripHeld = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, holdingControllerType);

            if (!gripHeld)
            {
                ReleaseItem();
            }
        }

        private void ReleaseItem()
        {
            if (heldItem == null) return;

            bool overBasket = IsOverBasket(heldItem);

            if (overBasket && basketTrigger != null)
            {
                heldItem.transform.SetParent(null);
                basketTrigger.CollectItem(heldItem);
            }
            else
            {
                heldItem.transform.SetParent(null);
                heldItem.OnReleased(dropWithPhysics: true);
            }

            OVRInput.SetControllerVibration(0.1f, 0.1f, holdingControllerType);

            heldItem = null;
            holdingController = null;
        }

        private bool IsOverBasket(BasketItem item)
        {
            if (basketTrigger == null) return false;

            var basketCollider = basketTrigger.GetComponent<Collider>();
            if (basketCollider == null) return false;

            Vector3 itemPos = item.transform.position;
            Vector3 closestPoint = basketCollider.ClosestPoint(itemPos);
            float distance = Vector3.Distance(itemPos, closestPoint);

            return distance < 0.15f;
        }

        public bool IsHoldingItem => heldItem != null;
        public BasketItem HeldItem => heldItem;
    }
}

