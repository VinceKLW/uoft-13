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
    public string storeDomain = "hydrogen-preview.myshopify.com";
    
    [Tooltip("Shopify Storefront API Access Token")]
    public string storefrontAccessToken = "3b580e70970c4528da70c98e097c2fa0";
    
    [Tooltip("Maximum number of products to fetch and display")]
    public int maxProducts = 16;

    [Header("Spawn Settings")]
    [Tooltip("Distance from center to left/right product rows")]
    public float sideDistance = 5f;
    
    [Tooltip("Spacing between products in the same row")]
    public float rowSpacing = 3f;
    
    [Tooltip("Distance between front and back rows")]
    public float rowDepth = 8f;
    
    [Tooltip("Height offset from origin (Y position)")]
    public float heightOffset = 0f;
    
    [Tooltip("Number of products per side per row (default: 4)")]
    public int productsPerSidePerRow = 4;

    [Header("Display Settings")]
    [Tooltip("Size of the product display plane")]
    public Vector2 displaySize = new Vector2(2f, 2f);
    
    [Tooltip("Distance between product displays")]
    public float spacing = 3f;
    
    [Tooltip("Show product title text")]
    public bool showTitle = true;
    
    [Tooltip("Show product price text")]
    public bool showPrice = true;
    
    [Tooltip("Show product description")]
    public bool showDescription = true;

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

        // Spawn product displays
        for (int i = 0; i < products.Count; i++)
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

            // Extract featured image URL
            int featuredImageIndex = json.IndexOf("\"featuredImage\"", nodeStartIndex);
            if (featuredImageIndex != -1 && featuredImageIndex < searchLimit)
            {
                int urlIndex = json.IndexOf("\"url\"", featuredImageIndex);
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
                                product.imageUrl = json.Substring(quoteStart, quoteEnd - quoteStart);
                            }
                        }
                    }
                }
            }
            else
            {
                // Try to get from images array (but limit search to avoid nested nodes)
                int imagesIndex = json.IndexOf("\"images\"", nodeStartIndex);
                if (imagesIndex != -1 && imagesIndex < searchLimit)
                {
                    int edgesIndex = json.IndexOf("\"edges\"", imagesIndex);
                    if (edgesIndex != -1 && edgesIndex < searchLimit)
                    {
                        int firstNodeIndex = json.IndexOf("\"node\"", edgesIndex);
                        if (firstNodeIndex != -1 && firstNodeIndex < searchLimit)
                        {
                            int urlIndex = json.IndexOf("\"url\"", firstNodeIndex);
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
                                            product.imageUrl = json.Substring(quoteStart, quoteEnd - quoteStart);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
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
        
        // Create main display object
        GameObject displayObject = new GameObject($"ProductDisplay_{product.title}");
        displayObject.transform.position = spawnPosition;
        
        // Create plane for product image
        GameObject imagePlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        imagePlane.transform.parent = displayObject.transform;
        imagePlane.transform.localPosition = Vector3.zero;
        imagePlane.transform.localScale = new Vector3(displaySize.x / 10f, 1f, displaySize.y / 10f);
        imagePlane.name = "ImagePlane";
        
        // Remove collider if not needed
        Destroy(imagePlane.GetComponent<Collider>());
        
        // Load and apply product image
        if (!string.IsNullOrEmpty(product.imageUrl))
        {
            yield return StartCoroutine(LoadProductImage(product.imageUrl, imagePlane));
        }
        
        // Create text display for product info
        CreateProductInfoDisplay(displayObject, product);
        
        // Rotate to face the center corridor
        // Left side products face right, right side products face left
        bool isLeftSide = spawnPosition.x < 0;
        if (isLeftSide)
        {
            // Face right (toward center)
            displayObject.transform.rotation = Quaternion.Euler(0, 90, 0);
        }
        else
        {
            // Face left (toward center)
            displayObject.transform.rotation = Quaternion.Euler(0, -90, 0);
        }
        
        spawnedProductDisplays.Add(displayObject);
    }

    IEnumerator LoadProductImage(string imageUrl, GameObject plane)
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
                    
                    Renderer renderer = plane.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = material;
                        Debug.Log($"Successfully loaded and applied image: {imageUrl} ({texture.width}x{texture.height})");
                    }
                    else
                    {
                        Debug.LogError("Renderer component not found on plane!");
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
        float yOffset = 1.2f;
        
        // Title
        if (showTitle && !string.IsNullOrEmpty(product.title))
        {
            CreateTextObject(parent, product.title, new Vector3(0, yOffset, 0), 0.4f, Color.white);
            yOffset += 0.4f;
        }
        
        // Price
        if (showPrice && !string.IsNullOrEmpty(product.price))
        {
            CreateTextObject(parent, product.price, new Vector3(0, yOffset, 0), 0.3f, Color.yellow);
            yOffset += 0.3f;
        }
        
        // Description
        if (showDescription && !string.IsNullOrEmpty(product.description))
        {
            // Truncate long descriptions
            string desc = product.description;
            if (desc.Length > 200)
            {
                desc = desc.Substring(0, 197) + "...";
            }
            CreateTextObject(parent, desc, new Vector3(0, yOffset, 0), 0.2f, Color.gray, new Vector2(3f, 1.5f));
            yOffset += 1.5f;
        }
    }

    void CreateTextObject(GameObject parent, string text, Vector3 position, float fontSize, Color color, Vector2? backgroundSize = null)
    {
        GameObject textObject = new GameObject("ProductInfo");
        textObject.transform.parent = parent.transform;
        textObject.transform.localPosition = position;
        textObject.transform.localRotation = Quaternion.identity;
        
        // Try to use TextMeshPro if available, otherwise use TextMesh
        #if TMP_PRESENT
        TMPro.TextMeshPro textMesh = textObject.AddComponent<TMPro.TextMeshPro>();
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.alignment = TMPro.TextAlignmentOptions.Center;
        textMesh.color = color;
        #else
        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = (int)(fontSize * 100);
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.color = color;
        textMesh.characterSize = 0.1f;
        #endif
        
        // Add background
        Vector2 bgSize = backgroundSize ?? new Vector2(2f, 0.5f);
        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.transform.parent = textObject.transform;
        background.transform.localPosition = new Vector3(0, 0, 0.01f);
        background.transform.localScale = bgSize;
        Material bgMaterial = new Material(Shader.Find("Standard"));
        bgMaterial.color = new Color(0, 0, 0, 0.7f);
        background.GetComponent<Renderer>().material = bgMaterial;
        Destroy(background.GetComponent<Collider>());
    }

    Vector3 GetSpawnPosition(int index)
    {
        // Arrange 16 products in 2 rows with 8 products per side total
        // Layout: 2 rows (front and back), 4 products per side per row
        // Left side: indices 0-3 (front row), 8-11 (back row)
        // Right side: indices 4-7 (front row), 12-15 (back row)
        
        int productsPerRow = productsPerSidePerRow * 2; // 4 left + 4 right = 8 per row
        int rowIndex = index / productsPerRow; // 0 = front row, 1 = back row
        int positionInRow = index % productsPerRow; // 0-7 position within the row
        
        // Determine if left or right side
        // Left: 0-3 in front row, 0-3 in back row (but index 8-11)
        // Right: 4-7 in front row, 4-7 in back row (but index 12-15)
        bool isLeftSide = positionInRow < productsPerSidePerRow;
        
        // Position within the side (0-3)
        int positionInSide = isLeftSide ? positionInRow : positionInRow - productsPerSidePerRow;
        
        // Calculate X position (negative for left, positive for right)
        float x = isLeftSide ? -sideDistance : sideDistance;
        
        // Calculate Z position (front row is negative, back row is positive)
        // Center products in the row
        float zOffset = (positionInSide - (productsPerSidePerRow - 1) / 2f) * rowSpacing;
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
