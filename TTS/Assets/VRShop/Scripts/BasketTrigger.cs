using System.Collections.Generic;
using UnityEngine;

namespace VRShop
{
    public class BasketTrigger : MonoBehaviour
    {
        [Header("References")]
        public Transform basketRoot;
        public Transform itemContainer;
        
        [Header("Layout")]
        public Vector3 itemSpacing = new Vector3(0.15f, 0.12f, 0.15f);
        public int maxItemsPerRow = 3;

        private readonly List<BasketItem> collectedItems = new List<BasketItem>();

        public IReadOnlyList<BasketItem> CollectedItems => collectedItems;
        public int ItemCount => collectedItems.Count;

        private void OnTriggerEnter(Collider other)
        {
            var item = other.GetComponentInParent<BasketItem>();
            if (item == null || !item.canBeCollected || item.isCollected || item.isHeld)
            {
                return;
            }

            CollectItem(item);
        }

        public void CollectItem(BasketItem item)
        {
            if (item == null || collectedItems.Contains(item))
                return;

            item.isCollected = true;
            item.isHeld = false;
            collectedItems.Add(item);

            var targetParent = itemContainer != null ? itemContainer : basketRoot;
            item.transform.SetParent(targetParent);

            PositionItemInBasket(item, collectedItems.Count - 1);
            item.DisablePhysics();
        }

        public void RemoveItem(BasketItem item)
        {
            if (item == null || !collectedItems.Contains(item))
                return;

            collectedItems.Remove(item);
            item.isCollected = false;
            
            ReorganizeItems();
        }

        public bool ContainsItem(BasketItem item)
        {
            return collectedItems.Contains(item);
        }

        private void PositionItemInBasket(BasketItem item, int index)
        {
            var row = index / maxItemsPerRow;
            var col = index % maxItemsPerRow;

            var localPosition = new Vector3(
                (col - 1) * itemSpacing.x,
                row * itemSpacing.y,
                0f
            );

            item.transform.localPosition = localPosition;
            item.transform.localRotation = Quaternion.identity;
            item.transform.localScale = Vector3.one * 0.8f;
        }

        private void ReorganizeItems()
        {
            for (int i = 0; i < collectedItems.Count; i++)
            {
                if (collectedItems[i] != null)
                    PositionItemInBasket(collectedItems[i], i);
            }
        }
    }
}
