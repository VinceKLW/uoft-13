using System.Collections.Generic;
using UnityEngine;

namespace VRShop
{
    public class BasketTrigger : MonoBehaviour
    {
        public Transform basketRoot;
        public Transform itemContainer;
        public Vector3 itemSpacing = new Vector3(0.08f, 0.06f, 0.08f);
        public int maxItemsPerRow = 3;

        private readonly List<BasketItem> collectedItems = new List<BasketItem>();

        private void OnTriggerEnter(Collider other)
        {
            var item = other.GetComponentInParent<BasketItem>();
            if (item == null || !item.canBeCollected || item.isCollected)
            {
                return;
            }

            CollectItem(item);
        }

        private void CollectItem(BasketItem item)
        {
            item.isCollected = true;
            collectedItems.Add(item);

            var targetParent = itemContainer != null ? itemContainer : basketRoot;
            item.transform.SetParent(targetParent);

            var index = collectedItems.Count - 1;
            var row = index / maxItemsPerRow;
            var col = index % maxItemsPerRow;

            var localPosition = new Vector3(
                (col - 1) * itemSpacing.x,
                row * itemSpacing.y,
                0f
            );

            item.transform.localPosition = localPosition;
            item.transform.localRotation = Quaternion.identity;

            item.DisablePhysics();
        }
    }
}
