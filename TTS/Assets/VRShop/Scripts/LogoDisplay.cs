using UnityEngine;

namespace VRShop
{
    /// <summary>
    /// Displays a logo texture on a target renderer using MaterialPropertyBlock.
    /// </summary>
    public class LogoDisplay : MonoBehaviour
    {
        [Header("Logo Source")]
        public Texture2D staticTexture;
        public bool useStaticTexture = false;

        [Header("Dynamic Logo Source")]
        public ShopDataManager shopDataManager;

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private static readonly int BaseMapID = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexID = Shader.PropertyToID("_MainTex");

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            if (_renderer == null)
            {
                Debug.LogError($"[LogoDisplay] No renderer on {gameObject.name}");
                return;
            }

            if (useStaticTexture && staticTexture != null)
            {
                ApplyTexture(staticTexture);
            }
            else if (shopDataManager != null)
            {
                shopDataManager.OnBrandLogoLoaded += OnBrandLogoLoaded;
                if (shopDataManager.BrandLogoTexture != null)
                {
                    ApplyTexture(shopDataManager.BrandLogoTexture);
                }
            }
        }

        private void OnBrandLogoLoaded(Texture2D texture)
        {
            ApplyTexture(texture);
        }

        private void ApplyTexture(Texture2D texture)
        {
            if (texture == null || _renderer == null) return;

            // Method 1: Try MaterialPropertyBlock first
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetTexture(BaseMapID, texture);
            _propBlock.SetTexture(MainTexID, texture);
            _renderer.SetPropertyBlock(_propBlock);

            // Method 2: Also set directly on material instance as backup
            Material mat = _renderer.material;
            if (mat != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", texture);
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", texture);
                mat.mainTexture = texture;
            }

            Debug.Log($"[LogoDisplay] Applied texture to {gameObject.name}: {texture.width}x{texture.height}");
        }

        private void OnDestroy()
        {
            if (shopDataManager != null)
                shopDataManager.OnBrandLogoLoaded -= OnBrandLogoLoaded;
        }
    }
}
