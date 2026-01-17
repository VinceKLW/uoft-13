using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text.RegularExpressions;

[System.Serializable]
public class ScrapedProduct
{
    public string title;
    public string price;
    public string description;
    public string imageUrl;
    public string productUrl;
}

public class ModelSpawner : MonoBehaviour
{
    [Header("Shopify Store Configuration")]
    [Tooltip("Shopify store URL (e.g., 'https://legoheaven.myshopify.com')")]
    public string storeUrl = "https://legoheaven.myshopify.com";
    
    [Tooltip("Collections page URL")]
    public string collectionsUrl = "https://legoheaven.myshopify.com/collections/all";
    
    [Tooltip("Maximum number of products to fetch and display")]
    public int maxProducts = 20;

    [Header("Spawn Settings")]
    [Tooltip("Radius around origin to spawn product displays")]
    public float spawnRadius = 20f;
    
    [Tooltip("Minimum distance from origin")]
    public float minDistance = 5f;
    
    [Tooltip("Height offset from origin (Y position)")]
    public float heightOffset = 0f;

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
    private List<ScrapedProduct> scrapedProducts = new List<ScrapedProduct>();

    void Start()
    {
        StartCoroutine(ScrapeAndSpawnProducts());
    }

    IEnumerator ScrapeAndSpawnProducts()
    {
        if (isLoading)
        {
            Debug.LogWarning("Already loading products...");
            yield break;
        }

        isLoading = true;
        Debug.Log("Starting web scraping...");

        // Step 1: Get all product URLs from collections page
        Debug.Log($"Fetching product URLs from {collectionsUrl}...");
        List<string> productUrls = new List<string>();
        
        using (UnityWebRequest request = UnityWebRequest.Get(collectionsUrl))
        {
            request.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching collections page: {request.error}");
                isLoading = false;
                yield break;
            }

            string html = request.downloadHandler.text;
            productUrls = ExtractProductUrls(html);
            Debug.Log($"Found {productUrls.Count} product URLs");
        }

        if (productUrls.Count == 0)
        {
            Debug.LogError("No product URLs found!");
            isLoading = false;
            yield break;
        }

        // Step 2: Scrape each product page
        scrapedProducts.Clear();
        int productsToScrape = Mathf.Min(productUrls.Count, maxProducts);
        
        for (int i = 0; i < productsToScrape; i++)
        {
            string productUrl = productUrls[i];
            Debug.Log($"\n[{i + 1}/{productsToScrape}] Scraping: {productUrl}");
            
            yield return StartCoroutine(ScrapeProductPage(productUrl));
            
            // Small delay to avoid rate limiting
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log($"\n✅ Scraped {scrapedProducts.Count} products");

        // Step 3: Spawn product displays
        for (int i = 0; i < scrapedProducts.Count; i++)
        {
            StartCoroutine(SpawnProductDisplay(scrapedProducts[i], i));
        }

        isLoading = false;
    }

    List<string> ExtractProductUrls(string html)
    {
        List<string> urls = new List<string>();
        
        // Find all /products/ links
        Regex regex = new Regex(@"href=[""']([^""']*\/products\/[^""']*)[""']", RegexOptions.IgnoreCase);
        MatchCollection matches = regex.Matches(html);
        
        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                string url = match.Groups[1].Value;
                
                // Convert relative URLs to absolute
                if (url.StartsWith("/"))
                {
                    url = storeUrl + url;
                }
                else if (!url.StartsWith("http"))
                {
                    url = storeUrl + "/" + url;
                }
                
                // Remove variant parameters
                int questionMarkIndex = url.IndexOf('?');
                if (questionMarkIndex > 0)
                {
                    url = url.Substring(0, questionMarkIndex);
                }
                
                // Remove duplicates
                if (!urls.Contains(url))
                {
                    urls.Add(url);
                }
            }
        }
        
        return urls;
    }

    IEnumerator ScrapeProductPage(string productUrl)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(productUrl))
        {
            request.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Error scraping {productUrl}: {request.error}");
                yield break;
            }

            string html = request.downloadHandler.text;
            ScrapedProduct product = ParseProductHtml(html, productUrl);
            
            if (product != null && !string.IsNullOrEmpty(product.title))
            {
                scrapedProducts.Add(product);
                Debug.Log($"  ✓ Extracted: {product.title} - {product.price}");
            }
            else
            {
                Debug.LogWarning($"  ✗ Failed to extract product data from {productUrl}");
            }
        }
    }

    ScrapedProduct ParseProductHtml(string html, string productUrl)
    {
        ScrapedProduct product = new ScrapedProduct();
        product.productUrl = productUrl;

        // Extract title - try multiple selectors
        product.title = ExtractHtmlContent(html, new string[] {
            @"<h1[^>]*>(.*?)</h1>",
            @"data-product-title=[""']([^""']*)[""']",
            @"class=[""']product-title[""'][^>]*>(.*?)</",
            @"<title>(.*?)</title>"
        });
        
        // Clean up title (remove HTML tags)
        product.title = CleanHtml(product.title);

        // Extract price
        product.price = ExtractHtmlContent(html, new string[] {
            @"class=[""']price[""'][^>]*>(.*?)</",
            @"data-product-price=[""']([^""']*)[""']",
            @"class=[""']product-price[""'][^>]*>(.*?)</",
            @"\$(\d+\.?\d*)"
        });
        product.price = CleanHtml(product.price);
        
        // If price doesn't start with $, add it
        if (!string.IsNullOrEmpty(product.price) && !product.price.StartsWith("$"))
        {
            product.price = "$" + product.price.Trim();
        }

        // Extract description from og:description meta tag
        Regex metaDescRegex = new Regex(@"<meta\s+property=[""']og:description[""']\s+content=[""']([^""']*)[""']", RegexOptions.IgnoreCase);
        Match metaDescMatch = metaDescRegex.Match(html);
        if (metaDescMatch.Success && metaDescMatch.Groups.Count > 1)
        {
            product.description = metaDescMatch.Groups[1].Value;
            // Decode HTML entities in meta description
            product.description = product.description.Replace("&amp;", "&");
            product.description = product.description.Replace("&lt;", "<");
            product.description = product.description.Replace("&gt;", ">");
            product.description = product.description.Replace("&quot;", "\"");
            product.description = product.description.Replace("&#39;", "'");
            product.description = product.description.Replace("&apos;", "'");
        }
        else
        {
            // Fallback to other patterns if og:description not found
            product.description = ExtractHtmlContent(html, new string[] {
                @"class=[""']product-description[""'][^>]*>(.*?)</div>",
                @"data-product-description=[""']([^""']*)[""']",
                @"class=[""']product__description[""'][^>]*>(.*?)</div>",
                @"id=[""']product-description[""'][^>]*>(.*?)</div>"
            });
            product.description = CleanHtml(product.description);
        }
        
        // Extract main product image
        product.imageUrl = ExtractImageUrl(html, product.title);
        
        // If no image found, try to get from meta tags
        if (string.IsNullOrEmpty(product.imageUrl))
        {
            Regex metaImageRegex = new Regex(@"<meta\s+property=[""']og:image[""']\s+content=[""']([^""']*)[""']", RegexOptions.IgnoreCase);
            Match metaMatch = metaImageRegex.Match(html);
            if (metaMatch.Success && metaMatch.Groups.Count > 1)
            {
                product.imageUrl = NormalizeImageUrl(metaMatch.Groups[1].Value);
                Debug.Log($"Found image via og:image meta tag: {product.imageUrl}");
            }
        }
        
        // Log if we found an image
        if (!string.IsNullOrEmpty(product.imageUrl))
        {
            Debug.Log($"Image URL extracted: {product.imageUrl}");
        }
        else
        {
            Debug.LogWarning($"No image URL found for product: {product.title}");
        }

        return product;
    }

    string ExtractRteFormatterContent(string html)
    {
        // Find all rte-formatter elements
        Regex rteRegex = new Regex(@"<rte-formatter[^>]*class=[""'][^""']*rte[^""']*[""'][^>]*>", RegexOptions.IgnoreCase);
        MatchCollection rteMatches = rteRegex.Matches(html);
        
        if (rteMatches.Count == 0)
        {
            // Try without class requirement
            rteRegex = new Regex(@"<rte-formatter[^>]*>", RegexOptions.IgnoreCase);
            rteMatches = rteRegex.Matches(html);
        }
        
        if (rteMatches.Count > 0)
        {
            // Get the first rte-formatter match
            Match firstMatch = rteMatches[0];
            int startIndex = firstMatch.Index + firstMatch.Length;
            
            // Find the matching closing tag by counting nested tags
            int depth = 1;
            int currentIndex = startIndex;
            int endIndex = -1;
            
            while (currentIndex < html.Length && depth > 0)
            {
                // Look for opening or closing rte-formatter tags
                int nextOpen = html.IndexOf("<rte-formatter", currentIndex, StringComparison.OrdinalIgnoreCase);
                int nextClose = html.IndexOf("</rte-formatter>", currentIndex, StringComparison.OrdinalIgnoreCase);
                
                if (nextClose == -1)
                    break;
                
                if (nextOpen != -1 && nextOpen < nextClose)
                {
                    // Found nested opening tag
                    depth++;
                    currentIndex = nextOpen + 1;
                }
                else
                {
                    // Found closing tag
                    depth--;
                    if (depth == 0)
                    {
                        endIndex = nextClose;
                        break;
                    }
                    currentIndex = nextClose + 1;
                }
            }
            
            if (endIndex > startIndex)
            {
                string content = html.Substring(startIndex, endIndex - startIndex);
                Debug.Log($"Extracted rte-formatter content length: {content.Length} characters");
                return content;
            }
        }
        
        return "";
    }

    string ExtractHtmlContent(string html, string[] patterns)
    {
        foreach (string pattern in patterns)
        {
            try
            {
                Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                Match match = regex.Match(html);
                if (match.Success && match.Groups.Count > 1)
                {
                    string content = match.Groups[1].Value;
                    if (!string.IsNullOrEmpty(content))
                    {
                        return content;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Regex error with pattern {pattern}: {e.Message}");
            }
        }
        return "";
    }

    string ExtractImageUrl(string html, string productTitle)
    {
        // First try to get from data-src or data-lazy-src (common for lazy loading)
        Regex dataSrcRegex = new Regex(@"<img[^>]*(?:data-src|data-lazy-src)=[""']([^""']*(?:product|cdn\.shopify)[^""']*)[""']", RegexOptions.IgnoreCase);
        Match dataMatch = dataSrcRegex.Match(html);
        if (dataMatch.Success && dataMatch.Groups.Count > 1)
        {
            string url = dataMatch.Groups[1].Value;
            url = NormalizeImageUrl(url);
            if (!string.IsNullOrEmpty(url))
            {
                Debug.Log($"Found image via data-src: {url}");
                return url;
            }
        }
        
        // Try multiple image selectors
        string[] imagePatterns = {
            @"<img[^>]*data-product-image[^>]*(?:src|data-src)=[""']([^""']*)[""']",
            @"<img[^>]*class=[""'][^""']*product-image[^""']*[""'][^>]*(?:src|data-src)=[""']([^""']*)[""']",
            @"<img[^>]*class=[""'][^""']*product__image[^""']*[""'][^>]*(?:src|data-src)=[""']([^""']*)[""']",
            @"<img[^>]*alt=[""'][^""']*" + Regex.Escape(productTitle) + @"[^""']*[""'][^>]*(?:src|data-src)=[""']([^""']*)[""']"
        };

        foreach (string pattern in imagePatterns)
        {
            try
            {
                Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
                Match match = regex.Match(html);
                if (match.Success && match.Groups.Count > 1)
                {
                    string url = match.Groups[1].Value;
                    url = NormalizeImageUrl(url);
                    if (!string.IsNullOrEmpty(url))
                    {
                        Debug.Log($"Found image via pattern: {url}");
                        return url;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error extracting image with pattern {pattern}: {e.Message}");
            }
        }
        
        // Fallback: find any image that looks like a product image
        Regex fallbackRegex = new Regex(@"<img[^>]*(?:src|data-src)=[""']([^""']*(?:product|cdn\.shopify)[^""']*)[""']", RegexOptions.IgnoreCase);
        MatchCollection fallbackMatches = fallbackRegex.Matches(html);
        
        // Prefer larger images (usually first or largest)
        foreach (Match fallbackMatch in fallbackMatches)
        {
            if (fallbackMatch.Success && fallbackMatch.Groups.Count > 1)
            {
                string url = fallbackMatch.Groups[1].Value;
                url = NormalizeImageUrl(url);
                
                // Prefer images with "large" or "master" in the URL, or skip small thumbnails
                if (!string.IsNullOrEmpty(url) && 
                    (!url.Contains("_small") && !url.Contains("_thumb") && !url.Contains("_tiny")))
                {
                    Debug.Log($"Found image via fallback: {url}");
                    return url;
                }
            }
        }
        
        // Last resort: get any image from the matches
        if (fallbackMatches.Count > 0)
        {
            Match lastMatch = fallbackMatches[fallbackMatches.Count - 1];
            if (lastMatch.Success && lastMatch.Groups.Count > 1)
            {
                string url = NormalizeImageUrl(lastMatch.Groups[1].Value);
                if (!string.IsNullOrEmpty(url))
                {
                    Debug.Log($"Found image via last resort: {url}");
                    return url;
                }
            }
        }
        
        Debug.LogWarning($"No image found for product: {productTitle}");
        return "";
    }
    
    string NormalizeImageUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return "";
        
        // Remove query parameters that might cause issues
        int queryIndex = url.IndexOf('?');
        if (queryIndex > 0)
        {
            url = url.Substring(0, queryIndex);
        }
        
        // Convert relative URLs to absolute
        if (url.StartsWith("//"))
        {
            url = "https:" + url;
        }
        else if (url.StartsWith("/"))
        {
            url = storeUrl + url;
        }
        else if (!url.StartsWith("http"))
        {
            url = storeUrl + "/" + url;
        }
        
        return url;
    }

    string CleanHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return "";
        
        // Convert common block-level HTML tags to line breaks before removing tags
        html = html.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n");
        html = html.Replace("<p>", "\n").Replace("</p>", "\n");
        html = html.Replace("<div>", "\n").Replace("</div>", "\n");
        html = html.Replace("<li>", "\n• ").Replace("</li>", "");
        
        // Remove HTML tags
        html = Regex.Replace(html, @"<[^>]+>", "");
        
        // Decode HTML entities
        html = html.Replace("&amp;", "&");
        html = html.Replace("&lt;", "<");
        html = html.Replace("&gt;", ">");
        html = html.Replace("&quot;", "\"");
        html = html.Replace("&#39;", "'");
        html = html.Replace("&nbsp;", " ");
        html = html.Replace("&apos;", "'");
        
        // Clean up excessive whitespace but preserve line breaks
        // Replace multiple spaces with single space (but keep newlines)
        html = Regex.Replace(html, @"[ \t]+", " ");
        // Replace multiple consecutive newlines with double newline (paragraph break)
        html = Regex.Replace(html, @"\n\s*\n\s*\n+", "\n\n");
        // Trim each line
        string[] lines = html.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].Trim();
        }
        html = string.Join("\n", lines);
        
        // Final trim
        html = html.Trim();
        
        return html;
    }

    IEnumerator SpawnProductDisplay(ScrapedProduct product, int index)
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
        
        // Rotate to face origin
        displayObject.transform.LookAt(Vector3.zero);
        displayObject.transform.Rotate(0, 180, 0); // Flip to face outward
        
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
                
                // Try alternative URL format
                if (imageUrl.Contains("cdn.shopify.com"))
                {
                    // Try with different size parameter
                    string altUrl = imageUrl;
                    if (!altUrl.Contains("_"))
                    {
                        altUrl = altUrl.Replace(".jpg", "_large.jpg").Replace(".png", "_large.png");
                        Debug.Log($"Trying alternative URL: {altUrl}");
                        yield return StartCoroutine(LoadProductImage(altUrl, plane));
                    }
                }
            }
        }
    }

    void CreateProductInfoDisplay(GameObject parent, ScrapedProduct product)
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
        // Use actual product count for spacing
        int productCount = scrapedProducts.Count > 0 ? scrapedProducts.Count : maxProducts;
        
        // Arrange products in a circle around origin with even spacing
        float angle = (360f / productCount) * index * Mathf.Deg2Rad;
        
        // Use a more consistent distance based on spacing
        float distance = minDistance + (spawnRadius - minDistance) * ((float)index / Mathf.Max(1, productCount - 1));
        
        float x = Mathf.Cos(angle) * distance;
        float z = Mathf.Sin(angle) * distance;
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
        
        // Scrape and spawn new products
        StartCoroutine(ScrapeAndSpawnProducts());
    }
}
