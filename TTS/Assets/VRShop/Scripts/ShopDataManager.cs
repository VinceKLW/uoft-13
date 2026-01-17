using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace VRShop
{
    /// <summary>
    /// Manages fetching shop data from the Storefront API server
    /// </summary>
    public class ShopDataManager : MonoBehaviour
    {
        [Header("API Configuration")]
        [Tooltip("Base URL of the Storefront API server")]
        public string apiBaseUrl = "http://localhost:3000";
        
        [Tooltip("Timeout for API requests in seconds")]
        public float requestTimeout = 10f;

        [Header("Auto-fetch")]
        [Tooltip("Automatically fetch shop data on Start")]
        public bool fetchOnStart = true;

        // Events
        public event Action<ShopData> OnShopDataLoaded;
        public event Action<Texture2D> OnBrandLogoLoaded;
        public event Action<string> OnError;

        // Cached data
        public ShopData CurrentShopData { get; private set; }
        public Texture2D BrandLogoTexture { get; private set; }
        public bool IsLoading { get; private set; }

        private void Start()
        {
            if (fetchOnStart)
            {
                FetchShopData();
            }
        }

        /// <summary>
        /// Fetch shop data from the API
        /// </summary>
        public void FetchShopData()
        {
            if (IsLoading)
            {
                Debug.LogWarning("[ShopDataManager] Already loading shop data");
                return;
            }

            StartCoroutine(FetchShopDataCoroutine());
        }

        private IEnumerator FetchShopDataCoroutine()
        {
            IsLoading = true;
            string url = $"{apiBaseUrl}/api/shop";

            Debug.Log($"[ShopDataManager] Fetching shop data from: {url}");

            UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = (int)requestTimeout;
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            string logoUrlToFetch = null;

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log($"[ShopDataManager] Received response: {json.Substring(0, Mathf.Min(200, json.Length))}...");

                try
                {
                    ShopApiResponse response = JsonUtility.FromJson<ShopApiResponse>(json);
                    
                    if (response.shop != null)
                    {
                        CurrentShopData = response.shop;
                        Debug.Log($"[ShopDataManager] Shop loaded: {CurrentShopData.name}");
                        OnShopDataLoaded?.Invoke(CurrentShopData);

                        // Get logo URL to fetch after try-catch
                        logoUrlToFetch = GetBrandLogoUrl();
                    }
                    else
                    {
                        string errorMsg = response.error?.message ?? "Shop data is null";
                        Debug.LogError($"[ShopDataManager] API error: {errorMsg}");
                        OnError?.Invoke(errorMsg);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ShopDataManager] JSON parse error: {e.Message}");
                    OnError?.Invoke($"Failed to parse shop data: {e.Message}");
                }
            }
            else
            {
                string errorMsg = $"Request failed: {request.error}";
                Debug.LogError($"[ShopDataManager] {errorMsg}");
                OnError?.Invoke(errorMsg);
            }

            request.Dispose();

            // Fetch brand logo outside of try-catch (yield can't be inside try-catch)
            if (!string.IsNullOrEmpty(logoUrlToFetch))
            {
                yield return FetchLogoCoroutine(logoUrlToFetch);
            }
            else if (CurrentShopData != null)
            {
                Debug.Log("[ShopDataManager] No brand logo URL found");
            }

            IsLoading = false;
        }

        private IEnumerator FetchLogoCoroutine(string logoUrl)
        {
            Debug.Log($"[ShopDataManager] Fetching brand logo from: {logoUrl}");

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(logoUrl))
            {
                request.timeout = (int)requestTimeout;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    BrandLogoTexture = DownloadHandlerTexture.GetContent(request);
                    Debug.Log($"[ShopDataManager] Brand logo loaded: {BrandLogoTexture.width}x{BrandLogoTexture.height}");
                    OnBrandLogoLoaded?.Invoke(BrandLogoTexture);
                }
                else
                {
                    Debug.LogError($"[ShopDataManager] Failed to load logo: {request.error}");
                    OnError?.Invoke($"Failed to load brand logo: {request.error}");
                }
            }
        }

        private string GetBrandLogoUrl()
        {
            return CurrentShopData?.brand?.logo?.image?.url;
        }

        private void OnDestroy()
        {
            // Clean up texture
            if (BrandLogoTexture != null)
            {
                Destroy(BrandLogoTexture);
            }
        }
    }

    #region Data Models

    /// <summary>
    /// Root API response from /api/shop
    /// </summary>
    [Serializable]
    public class ShopApiResponse
    {
        public ShopData shop;
        public bool loading;
        public ApiError error;
    }

    [Serializable]
    public class ApiError
    {
        public string message;
    }

    [Serializable]
    public class ShopData
    {
        public string id;
        public string name;
        public string description;
        public PrimaryDomain primaryDomain;
        public ShopBrand brand;
        public PaymentSettings paymentSettings;
    }

    [Serializable]
    public class PrimaryDomain
    {
        public string url;
        public string host;
    }

    [Serializable]
    public class ShopBrand
    {
        public BrandLogo logo;
        public BrandColors colors;
    }

    [Serializable]
    public class BrandLogo
    {
        public BrandImage image;
    }

    [Serializable]
    public class BrandImage
    {
        public string url;
        public string altText;
    }

    [Serializable]
    public class BrandColors
    {
        public ColorSet[] primary;
    }

    [Serializable]
    public class ColorSet
    {
        public string background;
        public string foreground;
    }

    [Serializable]
    public class PaymentSettings
    {
        public string currencyCode;
        public string[] acceptedCardBrands;
    }

    #endregion
}
