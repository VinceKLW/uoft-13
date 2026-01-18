using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;

// GraphQL Response Types
[System.Serializable]
public class GraphQLResponse<T>
{
    public T data;
    public GraphQLError[] errors;
}

[System.Serializable]
public class GraphQLError
{
    public string message;
}

[System.Serializable]
public class ProductsData
{
    public ProductsConnection products;
}

[System.Serializable]
public class ProductsConnection
{
    public ProductEdge[] edges;
}

[System.Serializable]
public class ProductEdge
{
    public ProductNode node;
}

[System.Serializable]
public class ProductNode
{
    public string id;
    public string title;
    public string handle;
    public string description;
    public string descriptionHtml;
    public PriceRange priceRange;
    public ProductImage featuredImage;
    public ProductImagesConnection images;
    public ProductVariantsConnection variants;
}

[System.Serializable]
public class PriceRange
{
    public MoneyV2 minVariantPrice;
    public MoneyV2 maxVariantPrice;
}

[System.Serializable]
public class MoneyV2
{
    public string amount;
    public string currencyCode;
}

[System.Serializable]
public class ProductImage
{
    public string url;
    public string altText;
    public int width;
    public int height;
}

[System.Serializable]
public class ProductImagesConnection
{
    public ProductImageEdge[] edges;
}

[System.Serializable]
public class ProductImageEdge
{
    public ProductImage node;
}

[System.Serializable]
public class ProductVariantsConnection
{
    public ProductVariantEdge[] edges;
}

[System.Serializable]
public class ProductVariantEdge
{
    public ProductVariantNode node;
}

[System.Serializable]
public class ProductVariantNode
{
    public string id;
    public string title;
    public bool availableForSale;
    public MoneyV2 price;
}

// Simplified Product class for display
[System.Serializable]
public class ShopifyProduct
{
    public string id;
    public string title;
    public string handle;
    public string description;
    public string price;
    public string imageUrl;
    public string productUrl;
}

public class ModelSpawner : MonoBehaviour
{
    [Header("Shopify Store Configuration")]
    [Tooltip("Shopify store domain (e.g., 'hydrogen-preview.myshopify.com' or 'legoheaven.myshopify.com')")]
    public string storeDomain = "legoheaven.myshopify.com";
    
    [Tooltip("Shopify Storefront API Access Token")]
    public string storefrontAccessToken = "3e2ce1095834bb3dfb2c092f29db57ab";
    
    [Tooltip("Maximum number of products to fetch and display")]
    public int maxProducts = 8;

    [Header("Spawn Settings")]
    [Tooltip("Distance from center to left/right product rows")]
    public float sideDistance = 5f;
    
    [Tooltip("Spacing between products in the same row")]
    public float rowSpacing = 6f;
    
    [Tooltip("Distance between front and back rows")]
    public float rowDepth = 8f;
    
    [Tooltip("Height offset from origin (Y position)")]
    public float heightOffset = 0f;
    
    [Tooltip("Number of products per side per row (default: 2)")]
    public int productsPerSidePerRow = 2;

    [Header("Display Settings")]
    [Tooltip("Size of the product image (width x height)")]
    public Vector2 imageSize = new Vector2(1.5f, 1.5f);
    
    [Tooltip("Billboard width")]
    public float billboardWidth = 2.5f;
    
    [Tooltip("Billboard height")]
    public float billboardHeight = 4f;
    
    [Tooltip("Show product title text")]
    public bool showTitle = true;
    
    [Tooltip("Show product price text")]
    public bool showPrice = true;
    
    [Tooltip("Show product description")]
    public bool showDescription = true;
    
    [Tooltip("Maximum description length before wrapping")]
    public int maxDescriptionLength = 150;
    
    [Tooltip("Vertical offset to move all display elements up")]
    public float verticalOffset = 2f;

    private List<GameObject> spawnedProductDisplays = new List<GameObject>();
    private bool isLoading = false;
    private List<ShopifyProduct> products = new List<ShopifyProduct>();

    private string GetStorefrontApiUrl()
    {
        return $"https://{storeDomain}/api/2024-01/graphql.json";
    }

    void Start()
    {
        StartCoroutine(FetchAndSpawnProducts());
    }

    IEnumerator FetchAndSpawnProducts()
    {
        if (isLoading)
        {
            Debug.LogWarning("Already loading products...");
            yield break;
        }

        isLoading = true;
        Debug.Log("Starting Shopify API fetch...");

        // Fetch products from Shopify Storefront API
        yield return StartCoroutine(FetchProductsFromShopify());

        Debug.Log($"\n✅ Fetched {products.Count} products from Shopify");

        // Spawn product displays - limit to 12 products (6 per side)
        int productsToSpawn = Mathf.Min(products.Count, 12);
        for (int i = 0; i < productsToSpawn; i++)
        {
            StartCoroutine(SpawnProductDisplay(products[i], i));
        }

        isLoading = false;
    }

    IEnumerator FetchProductsFromShopify()
    {
        products.Clear();

        // GraphQL query to search for products (minified to avoid whitespace issues)
        string query = "query SearchProducts($query: String!, $first: Int!) { products(first: $first, query: $query) { edges { node { id title handle description descriptionHtml priceRange { minVariantPrice { amount currencyCode } maxVariantPrice { amount currencyCode } } featuredImage { url altText width height } images(first: 5) { edges { node { url altText width height } } } variants(first: 1) { edges { node { id title availableForSale price { amount currencyCode } } } } } } } }";

        // Create request body with proper JSON formatting
        string requestBody = $"{{\"query\":\"{query.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "")}\",\"variables\":{{\"query\":\"*\",\"first\":{maxProducts}}}}}";

        string apiUrl = GetStorefrontApiUrl();
        Debug.Log($"Fetching products from: {apiUrl}");

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Shopify-Storefront-Access-Token", storefrontAccessToken);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching products: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
                yield break;
            }

            string jsonResponse = request.downloadHandler.text;
            Debug.Log($"Received response: {jsonResponse.Substring(0, Mathf.Min(500, jsonResponse.Length))}...");

            // Parse the response
            ParseProductsResponse(jsonResponse);
        }
    }

    void ParseProductsResponse(string jsonResponse)
    {
        try
        {
            // Unity's JsonUtility doesn't handle nested structures well, so we'll parse manually
            // First check for errors
            if (jsonResponse.Contains("\"errors\""))
            {
                Debug.LogError($"GraphQL errors in response: {jsonResponse}");
                return;
            }

            // Extract products array using simple JSON parsing
            // This is a simplified parser - for production, consider using a proper JSON library
            int productsIndex = jsonResponse.IndexOf("\"products\"");
            if (productsIndex == -1)
            {
                Debug.LogError("No products found in response");
                return;
            }

            int edgesIndex = jsonResponse.IndexOf("\"edges\"", productsIndex);
            if (edgesIndex == -1)
            {
                Debug.LogError("No edges found in products");
                return;
            }

            // Find the opening bracket of the edges array
            int edgesArrayStart = jsonResponse.IndexOf("[", edgesIndex);
            if (edgesArrayStart == -1)
            {
                Debug.LogError("No edges array found");
                return;
            }

            // Parse each edge object in the edges array
            // Each edge has structure: { "node": { ...product data... } }
            int currentIndex = edgesArrayStart + 1;
            int maxIterations = maxProducts + 10; // Safety limit
            int iterationCount = 0;
            HashSet<string> parsedProductIds = new HashSet<string>();
            
            while (iterationCount < maxIterations && currentIndex < jsonResponse.Length)
            {
                iterationCount++;
                
                // Find the next edge object start (opening brace)
                int edgeStart = jsonResponse.IndexOf("{", currentIndex);
                if (edgeStart == -1) break;
                
                // Find the "node" keyword within this edge
                int nodeKeywordIndex = jsonResponse.IndexOf("\"node\"", edgeStart);
                if (nodeKeywordIndex == -1 || nodeKeywordIndex > edgeStart + 50)
                {
                    // Not a valid edge, skip to next
                    currentIndex = edgeStart + 1;
                    continue;
                }
                
                // Find the opening brace of the node object
                int nodeObjectStart = jsonResponse.IndexOf("{", nodeKeywordIndex);
                if (nodeObjectStart == -1)
                {
                    currentIndex = nodeKeywordIndex + 6;
                    continue;
                }
                
                // Check if this node has product-level fields (id and title at top level)
                int idCheck = jsonResponse.IndexOf("\"id\"", nodeObjectStart);
                int titleCheck = jsonResponse.IndexOf("\"title\"", nodeObjectStart);
                
                // Only parse if it looks like a product node (has id and title within first 500 chars)
                // Also check that we're not in a nested structure by verifying the node is directly after "node":
                if (idCheck != -1 && titleCheck != -1 && 
                    idCheck < nodeObjectStart + 500 && titleCheck < nodeObjectStart + 500 &&
                    nodeObjectStart - nodeKeywordIndex < 20) // node object should start soon after "node" keyword
                {
                    ShopifyProduct product = ParseProductNode(jsonResponse, nodeObjectStart);
                    if (product != null && !string.IsNullOrEmpty(product.title) && !string.IsNullOrEmpty(product.id))
                    {
                        // Check for duplicates using ID
                        if (!parsedProductIds.Contains(product.id))
                        {
                            parsedProductIds.Add(product.id);
                            products.Add(product);
                            Debug.Log($"  ✓ Parsed [{products.Count}/{maxProducts}]: {product.title} - {product.price}");
                            
                            // Stop if we've reached max products
                            if (products.Count >= maxProducts)
                            {
                                break;
                            }
                        }
                    }
                }
                
                // Find the closing brace of this edge object to advance
                int braceDepth = 0;
                int searchIndex = edgeStart;
                int edgeEnd = -1;
                
                while (searchIndex < jsonResponse.Length && searchIndex < edgeStart + 10000)
                {
                    if (jsonResponse[searchIndex] == '{')
                        braceDepth++;
                    else if (jsonResponse[searchIndex] == '}')
                    {
                        braceDepth--;
                        if (braceDepth == 0)
                        {
                            edgeEnd = searchIndex;
                            break;
                        }
                    }
                    searchIndex++;
                }
                
                if (edgeEnd != -1)
                {
                    currentIndex = edgeEnd + 1;
                }
                else
                {
                    // Couldn't find closing brace, advance safely
                    currentIndex = edgeStart + 1000;
                }
            }
            
            Debug.Log($"Finished parsing: {products.Count} products found after {iterationCount} iterations");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing products response: {e.Message}\n{e.StackTrace}");
        }
    }

    ShopifyProduct ParseProductNode(string json, int nodeStartIndex)
    {
        ShopifyProduct product = new ShopifyProduct();

        try
        {
            // Limit search to first 2000 characters of the node to avoid parsing nested structures
            int searchLimit = Mathf.Min(nodeStartIndex + 2000, json.Length);
            
            // Extract title
            int titleIndex = json.IndexOf("\"title\"", nodeStartIndex);
            if (titleIndex != -1 && titleIndex < searchLimit)
            {
                int colonIndex = json.IndexOf(":", titleIndex);
                if (colonIndex != -1 && colonIndex < searchLimit)
                {
                    int quoteStart = json.IndexOf("\"", colonIndex) + 1;
                    if (quoteStart > colonIndex && quoteStart < searchLimit)
                    {
                        int quoteEnd = json.IndexOf("\"", quoteStart);
                        if (quoteEnd != -1 && quoteEnd < searchLimit)
                        {
                            product.title = json.Substring(quoteStart, quoteEnd - quoteStart);
                        }
                    }
                }
            }

            // Extract handle
            int handleIndex = json.IndexOf("\"handle\"", nodeStartIndex);
            if (handleIndex != -1 && handleIndex < searchLimit)
            {
                int colonIndex = json.IndexOf(":", handleIndex);
                if (colonIndex != -1 && colonIndex < searchLimit)
                {
                    int quoteStart = json.IndexOf("\"", colonIndex) + 1;
                    if (quoteStart > colonIndex && quoteStart < searchLimit)
                    {
                        int quoteEnd = json.IndexOf("\"", quoteStart);
                        if (quoteEnd != -1 && quoteEnd < searchLimit)
                        {
                            product.handle = json.Substring(quoteStart, quoteEnd - quoteStart);
                            product.productUrl = $"https://{storeDomain}/products/{product.handle}";
                        }
                    }
                }
            }

            // Extract description
            int descIndex = json.IndexOf("\"description\"", nodeStartIndex);
            if (descIndex != -1 && descIndex < searchLimit)
            {
                int colonIndex = json.IndexOf(":", descIndex);
                if (colonIndex != -1 && colonIndex < searchLimit)
                {
                    int quoteStart = json.IndexOf("\"", colonIndex) + 1;
                    if (quoteStart > colonIndex && quoteStart < searchLimit)
                    {
                        int quoteEnd = json.IndexOf("\"", quoteStart);
                        if (quoteEnd != -1 && quoteEnd < searchLimit)
                        {
                            product.description = json.Substring(quoteStart, quoteEnd - quoteStart);
                            // Decode HTML entities
                            product.description = product.description.Replace("\\n", "\n").Replace("\\\"", "\"");
                        }
                    }
                }
            }

            // Extract price from priceRange
            int priceRangeIndex = json.IndexOf("\"priceRange\"", nodeStartIndex);
            if (priceRangeIndex != -1 && priceRangeIndex < searchLimit)
            {
                int minPriceIndex = json.IndexOf("\"minVariantPrice\"", priceRangeIndex);
                if (minPriceIndex != -1 && minPriceIndex < searchLimit)
                {
                    int amountIndex = json.IndexOf("\"amount\"", minPriceIndex);
                    if (amountIndex != -1 && amountIndex < searchLimit)
                    {
                        int colonIndex = json.IndexOf(":", amountIndex);
                        if (colonIndex != -1 && colonIndex < searchLimit)
                        {
                            int quoteStart = json.IndexOf("\"", colonIndex) + 1;
                            if (quoteStart > colonIndex && quoteStart < searchLimit)
                            {
                                int quoteEnd = json.IndexOf("\"", quoteStart);
                                if (quoteEnd != -1 && quoteEnd < searchLimit)
                                {
                                    string amount = json.Substring(quoteStart, quoteEnd - quoteStart);
                                    
                                    int currencyIndex = json.IndexOf("\"currencyCode\"", minPriceIndex);
                                    string currency = "USD";
                                    if (currencyIndex != -1 && currencyIndex < searchLimit)
                                    {
                                        int currColonIndex = json.IndexOf(":", currencyIndex);
                                        if (currColonIndex != -1 && currColonIndex < searchLimit)
                                        {
                                            int currQuoteStart = json.IndexOf("\"", currColonIndex) + 1;
                                            if (currQuoteStart > currColonIndex && currQuoteStart < searchLimit)
                                            {
                                                int currQuoteEnd = json.IndexOf("\"", currQuoteStart);
                                                if (currQuoteEnd != -1 && currQuoteEnd < searchLimit)
                                                {
                                                    currency = json.Substring(currQuoteStart, currQuoteEnd - currQuoteStart);
                                                }
                                            }
                                        }
                                    }

                                    if (float.TryParse(amount, out float priceValue))
                                    {
                                        product.price = $"{GetCurrencySymbol(currency)}{priceValue:F2}";
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Extract all image URLs and debug print them
            // Use a much larger search limit for images since they might be further in the JSON
            int imageSearchLimit = Mathf.Min(nodeStartIndex + 10000, json.Length);
            string productName = string.IsNullOrEmpty(product.title) ? "Unknown Product" : product.title;
            List<string> allImageUrls = new List<string>();
            
            // Extract featured image URL - search with larger limit
            int featuredImageIndex = json.IndexOf("\"featuredImage\"", nodeStartIndex);
            if (featuredImageIndex != -1)
            {
                Debug.Log($"[{productName}] Found 'featuredImage' at index {featuredImageIndex} (searchLimit: {imageSearchLimit})");
                
                // Check if featuredImage is null
                int nullCheck = json.IndexOf("null", featuredImageIndex);
                int colonAfterFeatured = json.IndexOf(":", featuredImageIndex);
                if (nullCheck != -1 && colonAfterFeatured != -1 && nullCheck < colonAfterFeatured + 10)
                {
                    Debug.LogWarning($"[{productName}] 'featuredImage' is null");
                }
                else if (featuredImageIndex < imageSearchLimit)
                {
                    int urlIndex = json.IndexOf("\"url\"", featuredImageIndex);
                    if (urlIndex != -1 && urlIndex < imageSearchLimit)
                    {
                        int colonIndex = json.IndexOf(":", urlIndex);
                        if (colonIndex != -1 && colonIndex < imageSearchLimit)
                        {
                            int quoteStart = json.IndexOf("\"", colonIndex) + 1;
                            if (quoteStart > colonIndex && quoteStart < imageSearchLimit)
                            {
                                int quoteEnd = json.IndexOf("\"", quoteStart);
                                if (quoteEnd != -1 && quoteEnd < imageSearchLimit)
                                {
                                    product.imageUrl = json.Substring(quoteStart, quoteEnd - quoteStart);
                                    allImageUrls.Add(product.imageUrl);
                                    Debug.Log($"[{productName}] Featured Image URL: {product.imageUrl}");
                                }
                                else
                                {
                                    Debug.LogWarning($"[{productName}] Could not find closing quote for featuredImage URL");
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"[{productName}] Could not find opening quote for featuredImage URL");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[{productName}] Could not find colon after 'url' in featuredImage");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[{productName}] Could not find 'url' field in featuredImage (searched up to index {imageSearchLimit})");
                    }
                }
                else
                {
                    Debug.LogWarning($"[{productName}] 'featuredImage' found but beyond search limit ({featuredImageIndex} >= {imageSearchLimit})");
                }
            }
            else
            {
                Debug.Log($"[{productName}] 'featuredImage' not found in JSON");
            }
            
            // Extract all images from images array - search with larger limit
            int imagesIndex = json.IndexOf("\"images\"", nodeStartIndex);
            if (imagesIndex != -1)
            {
                Debug.Log($"[{productName}] Found 'images' at index {imagesIndex} (searchLimit: {imageSearchLimit})");
                if (imagesIndex < imageSearchLimit)
                {
                    List<string> imageUrls = ExtractAllImageUrls(json, imagesIndex, imageSearchLimit);
                    foreach (string url in imageUrls)
                    {
                        if (!allImageUrls.Contains(url))
                        {
                            allImageUrls.Add(url);
                        }
                    }
                    
                    if (imageUrls.Count > 0)
                    {
                        Debug.Log($"[{productName}] Found {imageUrls.Count} image(s) in images array:");
                        for (int i = 0; i < imageUrls.Count; i++)
                        {
                            Debug.Log($"[{productName}]   Image {i + 1}: {imageUrls[i]}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[{productName}] 'images' array found but no URLs extracted");
                    }
                    
                    // Set imageUrl from images array if featured image wasn't found
                    if (string.IsNullOrEmpty(product.imageUrl) && imageUrls.Count > 0)
                    {
                        product.imageUrl = imageUrls[0];
                    }
                }
                else
                {
                    Debug.LogWarning($"[{productName}] 'images' found but beyond search limit ({imagesIndex} >= {imageSearchLimit})");
                }
            }
            else
            {
                Debug.Log($"[{productName}] 'images' not found in JSON");
            }
            
            // Debug print summary of all image URLs for this product
            if (allImageUrls.Count > 0)
            {
                Debug.Log($"[{productName}] === TOTAL IMAGE URLs: {allImageUrls.Count} ===");
                for (int i = 0; i < allImageUrls.Count; i++)
                {
                    Debug.Log($"[{productName}]   URL {i + 1}/{allImageUrls.Count}: {allImageUrls[i]}");
                }
            }
            else
            {
                Debug.LogWarning($"[{productName}] No image URLs found for this product!");
            }

            // Extract ID
            int idIndex = json.IndexOf("\"id\"", nodeStartIndex);
            if (idIndex != -1 && idIndex < searchLimit)
            {
                int colonIndex = json.IndexOf(":", idIndex);
                if (colonIndex != -1 && colonIndex < searchLimit)
                {
                    int quoteStart = json.IndexOf("\"", colonIndex) + 1;
                    if (quoteStart > colonIndex && quoteStart < searchLimit)
                    {
                        int quoteEnd = json.IndexOf("\"", quoteStart);
                        if (quoteEnd != -1 && quoteEnd < searchLimit)
                        {
                            product.id = json.Substring(quoteStart, quoteEnd - quoteStart);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Error parsing product node: {e.Message}");
            return null;
        }

        return product;
    }

    List<string> ExtractAllImageUrls(string json, int imagesStartIndex, int searchLimit)
    {
        List<string> imageUrls = new List<string>();
        
        try
        {
            // Find the edges array within images
            int edgesIndex = json.IndexOf("\"edges\"", imagesStartIndex);
            if (edgesIndex == -1 || edgesIndex >= searchLimit)
                return imageUrls;
            
            // Find the opening bracket of the edges array
            int edgesArrayStart = json.IndexOf("[", edgesIndex);
            if (edgesArrayStart == -1 || edgesArrayStart >= searchLimit)
                return imageUrls;
            
            // Parse each edge in the edges array
            int currentIndex = edgesArrayStart + 1;
            int maxIterations = 20; // Safety limit for images
            int iterationCount = 0;
            
            while (iterationCount < maxIterations && currentIndex < searchLimit && currentIndex < json.Length)
            {
                iterationCount++;
                
                // Find the next edge object start
                int edgeStart = json.IndexOf("{", currentIndex);
                if (edgeStart == -1 || edgeStart >= searchLimit)
                    break;
                
                // Find the "node" keyword within this edge
                int nodeKeywordIndex = json.IndexOf("\"node\"", edgeStart);
                if (nodeKeywordIndex == -1 || nodeKeywordIndex >= searchLimit || nodeKeywordIndex > edgeStart + 100)
                {
                    currentIndex = edgeStart + 1;
                    continue;
                }
                
                // Find the opening brace of the node object
                int nodeObjectStart = json.IndexOf("{", nodeKeywordIndex);
                if (nodeObjectStart == -1 || nodeObjectStart >= searchLimit)
                {
                    currentIndex = nodeKeywordIndex + 6;
                    continue;
                }
                
                // Extract URL from this node
                int urlIndex = json.IndexOf("\"url\"", nodeObjectStart);
                if (urlIndex != -1 && urlIndex < searchLimit)
                {
                    int colonIndex = json.IndexOf(":", urlIndex);
                    if (colonIndex != -1 && colonIndex < searchLimit)
                    {
                        int quoteStart = json.IndexOf("\"", colonIndex) + 1;
                        if (quoteStart > colonIndex && quoteStart < searchLimit)
                        {
                            int quoteEnd = json.IndexOf("\"", quoteStart);
                            if (quoteEnd != -1 && quoteEnd < searchLimit)
                            {
                                string url = json.Substring(quoteStart, quoteEnd - quoteStart);
                                if (!string.IsNullOrEmpty(url) && !imageUrls.Contains(url))
                                {
                                    imageUrls.Add(url);
                                }
                            }
                        }
                    }
                }
                
                // Find the closing brace of this edge object to advance
                int braceDepth = 0;
                int searchIndex = edgeStart;
                int edgeEnd = -1;
                
                while (searchIndex < searchLimit && searchIndex < json.Length && searchIndex < edgeStart + 5000)
                {
                    if (json[searchIndex] == '{')
                        braceDepth++;
                    else if (json[searchIndex] == '}')
                    {
                        braceDepth--;
                        if (braceDepth == 0)
                        {
                            edgeEnd = searchIndex;
                            break;
                        }
                    }
                    searchIndex++;
                }
                
                if (edgeEnd != -1)
                {
                    currentIndex = edgeEnd + 1;
                }
                else
                {
                    // Couldn't find closing brace, advance safely
                    currentIndex = edgeStart + 1000;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Error extracting image URLs: {e.Message}");
        }
        
        return imageUrls;
    }

    string GetCurrencySymbol(string currencyCode)
    {
        switch (currencyCode.ToUpper())
        {
            case "USD": return "$";
            case "EUR": return "€";
            case "GBP": return "£";
            case "CAD": return "C$";
            case "AUD": return "A$";
            default: return currencyCode + " ";
        }
    }


    IEnumerator SpawnProductDisplay(ShopifyProduct product, int index)
    {
        // Calculate spawn position
        Vector3 spawnPosition = GetSpawnPosition(index);
        Debug.Log($"Spawning product {index} '{product.title}' at position {spawnPosition}");
        
        // Create main display object
        GameObject displayObject = new GameObject($"ProductDisplay_{product.title}");
        displayObject.transform.position = spawnPosition;
        
        // Billboard background removed to prevent purple blocks
        
        // Create vertical quad for product image - left aligned at top
        GameObject imageQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        imageQuad.transform.parent = displayObject.transform;
        // Position left-aligned: start from left edge, offset by half image width
        float leftEdgeX = -billboardWidth / 2f;
        float imageX = leftEdgeX + imageSize.x / 2f;
        float imageY = billboardHeight / 2f - imageSize.y / 2f + verticalOffset; // Top of billboard, accounting for image height, plus vertical offset
        imageQuad.transform.localPosition = new Vector3(imageX, imageY, 0.01f);
        imageQuad.transform.localScale = new Vector3(imageSize.x, imageSize.y, 1f);
        imageQuad.name = "ProductImage";
        Destroy(imageQuad.GetComponent<Collider>());
        
        // Load and apply product image (frame will be created after image loads to match aspect ratio)
        if (!string.IsNullOrEmpty(product.imageUrl))
        {
            yield return StartCoroutine(LoadProductImage(product.imageUrl, imageQuad));
        }
        else
        {
            // Create frame even if no image
            CreateImageFrame(imageQuad, imageSize);
        }
        
        // Create text display for product info
        CreateProductInfoDisplay(displayObject, product);
        
        // Rotate to face the center corridor
        // Left side products face right, right side products face left
        bool isLeftSide = spawnPosition.x < 0;
        if (isLeftSide)
        {
            // Face right (toward center) - flipped direction
            displayObject.transform.rotation = Quaternion.Euler(0, -90, 0);
        }
        else
        {
            // Face left (toward center) - flipped direction
            displayObject.transform.rotation = Quaternion.Euler(0, 90, 0);
        }
        
        spawnedProductDisplays.Add(displayObject);
    }

    void CreateImageFrame(GameObject imageQuad, Vector2 imageSize)
    {
        // Frame disabled to prevent purple blocks
        // If you want a frame, ensure proper shader/material setup
    }
    
    IEnumerator LoadProductImage(string imageUrl, GameObject imageQuad)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            Debug.LogWarning("Image URL is empty, cannot load image");
            yield break;
        }
        
        Debug.Log($"Loading image from: {imageUrl}");
        
        using (UnityWebRequest textureRequest = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            // Set timeout
            textureRequest.timeout = 10;
            
            yield return textureRequest.SendWebRequest();

            if (textureRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(textureRequest);
                
                if (texture != null)
                {
                    // Ensure texture is readable and properly formatted
                    texture.wrapMode = TextureWrapMode.Clamp;
                    texture.filterMode = FilterMode.Bilinear;
                    texture.anisoLevel = 1;
                    
                    // Preserve aspect ratio
                    float aspectRatio = (float)texture.width / texture.height;
                    Vector2 currentScale = imageQuad.transform.localScale;
                    if (aspectRatio > 1f)
                    {
                        // Landscape: adjust height
                        currentScale.y = currentScale.x / aspectRatio;
                    }
                    else
                    {
                        // Portrait: adjust width
                        currentScale.x = currentScale.y * aspectRatio;
                    }
                    imageQuad.transform.localScale = new Vector3(currentScale.x, currentScale.y, 1f);
                    
                    // Try to use Unlit shader first (simpler, more reliable for images)
                    // Fall back to Standard if Unlit is not available
                    Shader shader = Shader.Find("Unlit/Texture");
                    if (shader == null)
                    {
                        shader = Shader.Find("Standard");
                    }
                    
                    if (shader == null)
                    {
                        Debug.LogError("Could not find Unlit/Texture or Standard shader!");
                        yield break;
                    }
                    
                    // Create material with texture
                    Material material = new Material(shader);
                    material.mainTexture = texture;
                    
                    // If using Standard shader, configure it properly
                    if (shader.name == "Standard")
                    {
                        material.SetFloat("_Metallic", 0f);
                        material.SetFloat("_Glossiness", 0.5f);
                        material.SetFloat("_Mode", 0); // Opaque mode
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt("_ZWrite", 1);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.DisableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = -1;
                    }
                    
                    Renderer renderer = imageQuad.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = material;
                        
                        // Create frame after image is loaded with correct aspect ratio
                        CreateImageFrame(imageQuad, currentScale);
                        
                        Debug.Log($"Successfully loaded and applied image: {imageUrl} ({texture.width}x{texture.height})");
                    }
                    else
                    {
                        Debug.LogError("Renderer component not found on image quad!");
                    }
                }
                else
                {
                    Debug.LogWarning($"Texture is null after download from: {imageUrl}");
                }
            }
            else
            {
                Debug.LogError($"Failed to load image: {imageUrl}");
                Debug.LogError($"Error: {textureRequest.error}");
                Debug.LogError($"Response Code: {textureRequest.responseCode}");
            }
        }
    }

    void CreateProductInfoDisplay(GameObject parent, ShopifyProduct product)
    {
        // Calculate positions relative to billboard - all left-aligned
        float billboardTop = billboardHeight / 2f;
        float billboardBottom = -billboardHeight / 2f;
        float leftEdgeX = -billboardWidth / 2f;
        
        // Start from top, below image
        // Image is at: billboardTop - imageSize.y/2, so text starts below it
        float spacing = 0.4f;
        float currentY = billboardTop - imageSize.y - spacing + verticalOffset; // Below image, plus vertical offset
        
        // Title (left-aligned, below image)
        if (showTitle && !string.IsNullOrEmpty(product.title))
        {
            CreateTextObject(parent, product.title, new Vector3(leftEdgeX, currentY, 0.02f), 0.35f, new Color(0.1f, 0.1f, 0.1f), 
                new Vector2(billboardWidth * 0.9f, 0.4f), false);
            currentY -= 0.35f;
        }
        
        // Price (left-aligned, below title)
        if (showPrice && !string.IsNullOrEmpty(product.price))
        {
            CreateTextObject(parent, product.price, new Vector3(leftEdgeX, currentY, 0.02f), 0.2f, new Color(0.2f, 0.6f, 0.2f), 
                new Vector2(billboardWidth * 0.9f, 0.35f), false);
            currentY -= 0.3f;
        }
        
        // Description (left-aligned, below price)
        if (showDescription && !string.IsNullOrEmpty(product.description))
        {
            string desc = product.description;
            
            // Print original description
            Debug.Log($"Product Description (Original): {product.title}\n{desc}");
            
            // Clean up description
            desc = desc.Trim();
            // No truncation - display full description with text wrapping
            
            // Print processed description
            Debug.Log($"Product Description (Processed): {product.title}\n{desc}");
            
            // Calculate how much vertical space we have left
            float availableHeight = currentY - billboardBottom + 0.2f;
            float descHeight = Mathf.Min(availableHeight, 3.0f); // Increased max description height to show full descriptions
            
            // Position description top-aligned, starting right below price/title
            CreateWrappedTextObject(parent, desc, new Vector3(leftEdgeX, currentY, 0.02f), 
                0.09f, new Color(0.3f, 0.3f, 0.3f), new Vector2(billboardWidth * 0.85f, descHeight), false);
        }
    }

    void CreateTextObject(GameObject parent, string text, Vector3 position, float fontSize, Color color, Vector2? backgroundSize = null, bool hasBackground = false)
    {
        GameObject textObject = new GameObject("ProductInfo");
        textObject.transform.parent = parent.transform;
        textObject.transform.localPosition = position;
        textObject.transform.localRotation = Quaternion.identity;
        
        // Try to use TextMeshPro if available, otherwise use TextMesh
        #if TMP_PRESENT
        TMPro.TextMeshPro textMesh = textObject.AddComponent<TMPro.TextMeshPro>();
        
        // Assign default font if not set
        if (textMesh.font == null)
        {
            var defaultFont = TMPro.TMP_Settings.defaultFontAsset;
            if (defaultFont != null)
            {
                textMesh.font = defaultFont;
            }
            else
            {
                // Try loading from Resources
                defaultFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (defaultFont != null)
                {
                    textMesh.font = defaultFont;
                }
            }
        }
        
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.alignment = TMPro.TextAlignmentOptions.Left;
        textMesh.color = color;
        textMesh.enableWordWrapping = true;
        if (backgroundSize.HasValue)
        {
            textMesh.rectTransform.sizeDelta = new Vector2(backgroundSize.Value.x, backgroundSize.Value.y);
        }
        #else
        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = (int)(fontSize * 100);
        textMesh.anchor = TextAnchor.MiddleLeft;
        textMesh.color = color;
        textMesh.characterSize = 0.1f;
        #endif
        
        // Background disabled to prevent purple blocks
        // Text backgrounds removed to avoid shader/material issues
    }
    
    void CreateWrappedTextObject(GameObject parent, string text, Vector3 position, float fontSize, Color color, Vector2 size, bool hasBackground = false)
    {
        GameObject textObject = new GameObject("ProductDescription");
        textObject.transform.parent = parent.transform;
        textObject.transform.localPosition = position;
        textObject.transform.localRotation = Quaternion.identity;
        
        // Try to use TextMeshPro if available (better text wrapping)
        #if TMP_PRESENT
        TMPro.TextMeshPro textMesh = textObject.AddComponent<TMPro.TextMeshPro>();
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.alignment = TMPro.TextAlignmentOptions.TopLeft;
        textMesh.color = color;
        textMesh.enableWordWrapping = true;
        textMesh.rectTransform.sizeDelta = new Vector2(size.x, size.y);
        textMesh.overflowMode = TMPro.TextOverflowModes.Truncate;
        textMesh.wordSpacing = 0f; // Tight word spacing
        textMesh.characterSpacing = 0f; // Tight character spacing
        #else
        // For regular TextMesh, manually wrap text - more aggressive wrapping
        int maxCharsPerLine = (int)((size.x / (fontSize * 0.1f)) * 0.6f); // More aggressive: 60% of calculated width
        maxCharsPerLine = Mathf.Max(maxCharsPerLine, 10); // Minimum 10 chars per line (reduced from 15)
        text = WrapText(text, maxCharsPerLine);
        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = (int)(fontSize * 100);
        textMesh.anchor = TextAnchor.UpperLeft;
        textMesh.color = color;
        textMesh.characterSize = 0.1f;
        #endif
        
        // Background disabled to prevent purple blocks
        // Text backgrounds removed to avoid shader/material issues
    }
    
    string WrapText(string text, int maxCharsPerLine)
    {
        if (string.IsNullOrEmpty(text) || maxCharsPerLine <= 0)
            return text;
        
        string[] words = text.Split(' ');
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        string currentLine = "";
        
        foreach (string word in words)
        {
            // More aggressive wrapping: wrap if adding this word would exceed 50% of max line length
            int threshold = (int)(maxCharsPerLine * 0.5f);
            
            if (currentLine.Length + word.Length + 1 <= threshold)
            {
                if (currentLine.Length > 0)
                    currentLine += " ";
                currentLine += word;
            }
            else
            {
                // If current line is not empty, add it and start new line
                if (currentLine.Length > 0)
                {
                    if (result.Length > 0)
                        result.Append("\n");
                    result.Append(currentLine);
                }
                
                // If word itself is longer than max, try to break it (simple approach)
                if (word.Length > maxCharsPerLine)
                {
                    // Break long word into chunks
                    int startIndex = 0;
                    while (startIndex < word.Length)
                    {
                        int chunkLength = Mathf.Min(maxCharsPerLine, word.Length - startIndex);
                        string chunk = word.Substring(startIndex, chunkLength);
                        if (result.Length > 0)
                            result.Append("\n");
                        result.Append(chunk);
                        startIndex += chunkLength;
                    }
                    currentLine = "";
                }
                else
                {
                    currentLine = word;
                }
            }
        }
        
        if (currentLine.Length > 0)
        {
            if (result.Length > 0)
                result.Append("\n");
            result.Append(currentLine);
        }
        
        return result.ToString();
    }

    Vector3 GetSpawnPosition(int index)
    {
        // Arrange products evenly spaced in 2 rows with left/right sides
        // Layout: 2 rows (front and back), products per side per row
        // Left side: indices 0-3 (front row), then back row
        // Right side: indices 4-7 (front row), then back row
        
        int productsPerRow = productsPerSidePerRow * 2; // left + right per row
        int rowIndex = index / productsPerRow; // 0 = front row, 1 = back row
        int positionInRow = index % productsPerRow; // 0 to (productsPerRow-1)
        
        // Determine if left or right side
        bool isLeftSide = positionInRow < productsPerSidePerRow;
        
        // Position within the side (0 to productsPerSidePerRow-1)
        int positionInSide = isLeftSide ? positionInRow : positionInRow - productsPerSidePerRow;
        
        // Calculate X position (negative for left, positive for right)
        float x = isLeftSide ? -sideDistance : sideDistance;
        
        // Calculate Z position with perfectly even linear spacing
        // Each product is spaced exactly rowSpacing units apart
        // First product (positionInSide=0) at -span/2, last at +span/2
        float zOffset;
        if (productsPerSidePerRow <= 1)
        {
            // Single product: center it
            zOffset = 0f;
        }
        else
        {
            // Multiple products: evenly space them
            float totalSpan = (productsPerSidePerRow - 1) * rowSpacing;
            zOffset = -totalSpan / 2f + (positionInSide * rowSpacing);
        }
        
        // Apply row offset (front row negative, back row positive)
        float z = rowIndex == 0 ? -rowDepth / 2f + zOffset : rowDepth / 2f + zOffset;
        
        float y = heightOffset;
        
        return new Vector3(x, y, z);
    }

    [ContextMenu("Refresh Products")]
    public void RefreshProducts()
    {
        // Clear existing displays
        foreach (GameObject display in spawnedProductDisplays)
        {
            if (display != null)
            {
                Destroy(display);
            }
        }
        spawnedProductDisplays.Clear();
        products.Clear();
        
        // Fetch and spawn new products
        StartCoroutine(FetchAndSpawnProducts());
    }
}
