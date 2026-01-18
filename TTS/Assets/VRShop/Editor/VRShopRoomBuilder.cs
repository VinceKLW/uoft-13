using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using VRShop;

namespace VRShop.Editor
{
    public class VRShopRoomBuilder : EditorWindow
    {
        private float roomWidth = 10f;
        private float roomHeight = 4f;
        private float roomDepth = 10f;
        private float cameraHeight = 1.6f;

        private const string SCENE_PATH = "Assets/VRShop/Scenes/VRShopScene.unity";
        private const string WALL_MATERIAL_PATH = "Assets/VRShop/Materials/WallMaterial.mat";
        private const string CEILING_MATERIAL_PATH = "Assets/VRShop/Materials/CeilingMaterial.mat";
        private const string ACCENT_WALL_MATERIAL_PATH = "Assets/VRShop/Materials/AccentWallMaterial.mat";
        private const string FLOOR_MATERIAL_PATH = "Assets/VRShop/Materials/FloorMaterial.mat";
        private const string FLOOR_TILE_MATERIAL_PATH = "Assets/VRShop/Materials/FloorTileMaterial.mat";
        private const string FIXTURE_MATERIAL_PATH = "Assets/VRShop/Materials/FixtureMaterial.mat";
        private const string EMISSIVE_PANEL_MATERIAL_PATH = "Assets/VRShop/Materials/EmissivePanelMaterial.mat";
        private const string SHOPIFY_MATERIAL_PATH = "Assets/VRShop/Materials/ShopifyLogoMaterial.mat";
        private const string BRAND_MATERIAL_PATH = "Assets/VRShop/Materials/BrandLogoMaterial.mat";
        private const string PRODUCTS_PREFAB_DIR = "Assets/VRShop/Prefabs/Products";
        private const string BASKET_PREFAB_DIR = "Assets/VRShop/Prefabs/Basket";
        private const string BASKET_PREFAB_PATH = "Assets/VRShop/Prefabs/Basket/Basket.prefab";

        [MenuItem("VRShop/Quick Setup (Create Everything)", false, 0)]
        public static void QuickSetup()
        {
            var builder = ScriptableObject.CreateInstance<VRShopRoomBuilder>();
            try
            {
                builder.CreateAllMaterials();
                builder.CreateProductPrefabs();
                builder.CreateBasketPrefab();
                builder.CreateScene();
                Debug.Log("[VRShop] Setup complete! Press Play to test.");
            }
            finally
            {
                DestroyImmediate(builder);
            }
        }

        [MenuItem("VRShop/Build Shop Room", false, 1)]
        public static void ShowWindow()
        {
            GetWindow<VRShopRoomBuilder>("VR Shop Room Builder");
        }

        private void OnGUI()
        {
            GUILayout.Label("VR Shop Room Builder", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            roomWidth = EditorGUILayout.FloatField("Room Width", roomWidth);
            roomHeight = EditorGUILayout.FloatField("Room Height", roomHeight);
            roomDepth = EditorGUILayout.FloatField("Room Depth", roomDepth);
            cameraHeight = EditorGUILayout.FloatField("Camera Height", cameraHeight);

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Everything", GUILayout.Height(40)))
            {
                CreateAllMaterials();
                CreateProductPrefabs();
                CreateBasketPrefab();
                CreateScene();
            }
        }

        private void CreateAllMaterials()
        {
            EnsureDirectoryExists("Assets/VRShop/Materials");

            // Delete old materials
            string[] materialsToDelete =
            {
                WALL_MATERIAL_PATH,
                CEILING_MATERIAL_PATH,
                ACCENT_WALL_MATERIAL_PATH,
                FLOOR_MATERIAL_PATH,
                FLOOR_TILE_MATERIAL_PATH,
                FIXTURE_MATERIAL_PATH,
                EMISSIVE_PANEL_MATERIAL_PATH,
                SHOPIFY_MATERIAL_PATH,
                BRAND_MATERIAL_PATH,
            };
            foreach (var path in materialsToDelete)
            {
                if (AssetDatabase.LoadAssetAtPath<Material>(path))
                    AssetDatabase.DeleteAsset(path);
            }

            // Use BUILT-IN shaders that are guaranteed to work
            // Sprites/Default is reliable for transparent PNGs on quads
            Shader spriteShader = Shader.Find("Sprites/Default");
            Shader unlitTextureShader = Shader.Find("Unlit/Texture");
            Shader unlitColorShader = Shader.Find("Unlit/Color");
            
            // For walls, try URP Lit first, fallback to Standard
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            if (litShader == null) litShader = unlitColorShader;

            Debug.Log($"[VRShop] Lit shader: {(litShader != null ? litShader.name : "NULL")}");
            Debug.Log($"[VRShop] Unlit shader: {(unlitTextureShader != null ? unlitTextureShader.name : "NULL")}");
            Debug.Log($"[VRShop] Sprite shader: {(spriteShader != null ? spriteShader.name : "NULL")}");

            // Base wall material (soft gray)
            var wallMat = new Material(litShader);
            SetLitMaterialProps(wallMat, new Color(0.85f, 0.86f, 0.9f), 0.05f, 0.3f);
            AssetDatabase.CreateAsset(wallMat, WALL_MATERIAL_PATH);

            // Ceiling material (slightly brighter)
            var ceilingMat = new Material(litShader);
            SetLitMaterialProps(ceilingMat, new Color(0.95f, 0.95f, 0.97f), 0.0f, 0.2f);
            AssetDatabase.CreateAsset(ceilingMat, CEILING_MATERIAL_PATH);

            // Accent wall (cool gray-blue)
            var accentWallMat = new Material(litShader);
            SetLitMaterialProps(accentWallMat, new Color(0.62f, 0.68f, 0.78f), 0.05f, 0.35f);
            AssetDatabase.CreateAsset(accentWallMat, ACCENT_WALL_MATERIAL_PATH);

            // Floor base (darker)
            var floorMat = new Material(litShader);
            SetLitMaterialProps(floorMat, new Color(0.18f, 0.18f, 0.2f), 0.0f, 0.4f);
            AssetDatabase.CreateAsset(floorMat, FLOOR_MATERIAL_PATH);

            // Floor tile (slightly glossy)
            var floorTileMat = new Material(litShader);
            SetLitMaterialProps(floorTileMat, new Color(0.25f, 0.25f, 0.28f), 0.0f, 0.65f);
            AssetDatabase.CreateAsset(floorTileMat, FLOOR_TILE_MATERIAL_PATH);

            // Shopify logo material - USE SPRITE shader with texture pre-assigned
            var shopifyTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VRShop/Textures/shopify-logo.png");
            var shopifyMat = new Material(spriteShader ?? unlitTextureShader);
            if (shopifyTexture != null)
            {
                shopifyMat.mainTexture = shopifyTexture;
                shopifyMat.renderQueue = 3000;
                Debug.Log($"[VRShop] Shopify texture set: {shopifyTexture.width}x{shopifyTexture.height}");
            }
            else
            {
                Debug.LogError("[VRShop] Shopify texture NOT FOUND!");
            }
            AssetDatabase.CreateAsset(shopifyMat, SHOPIFY_MATERIAL_PATH);

            // Fixture material (matte charcoal)
            var fixtureMat = new Material(litShader);
            SetLitMaterialProps(fixtureMat, new Color(0.12f, 0.12f, 0.12f), 0.0f, 0.2f);
            AssetDatabase.CreateAsset(fixtureMat, FIXTURE_MATERIAL_PATH);

            // Emissive panel material (soft white glow)
            var emissiveMat = new Material(litShader);
            SetLitMaterialProps(emissiveMat, new Color(0.95f, 0.95f, 0.98f), 0.0f, 0.4f);
            if (emissiveMat.HasProperty("_EmissionColor"))
            {
                var emissiveColor = new Color(1.2f, 1.2f, 1.3f) * 2.5f;
                emissiveMat.EnableKeyword("_EMISSION");
                emissiveMat.SetColor("_EmissionColor", emissiveColor);
                emissiveMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            AssetDatabase.CreateAsset(emissiveMat, EMISSIVE_PANEL_MATERIAL_PATH);

            // Brand logo material - hardcoded Hydrogen logo
            var brandTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VRShop/Textures/hydrogen-logo.png");
            var brandMat = new Material(spriteShader ?? unlitTextureShader);
            if (brandTexture != null)
            {
                brandMat.mainTexture = brandTexture;
                brandMat.renderQueue = 3000;
                Debug.Log($"[VRShop] Brand texture set: {brandTexture.width}x{brandTexture.height}");
            }
            else
            {
                Debug.LogError("[VRShop] Brand texture NOT FOUND!");
            }
            DisableCullingIfAvailable(shopifyMat);
            DisableCullingIfAvailable(brandMat);
            AssetDatabase.CreateAsset(brandMat, BRAND_MATERIAL_PATH);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("[VRShop] Materials created");
        }

        private void CreateProductPrefabs()
        {
            EnsureDirectoryExists(PRODUCTS_PREFAB_DIR);

            var products = new[]
            {
                new ProductDefinition("Product_Box_Small", "Assets/SourceFiles/Models/Box_350x250x200_Mesh.fbx", 0.35f),
                new ProductDefinition("Product_Box_Tall", "Assets/SourceFiles/Models/Box_350x250x300_Mesh.fbx", 0.4f),
                new ProductDefinition("Product_Star", "Assets/SourceFiles/Models/Star.FBX", 0.3f),
                new ProductDefinition("Product_CubeHollow", "Assets/SourceFiles/Models/CubeHollow.FBX", 0.35f),
                new ProductDefinition("Product_Platform", "Assets/SourceFiles/Models/Platform.FBX", 0.5f),
            };

            foreach (var product in products)
            {
                var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(product.ModelPath);
                if (modelAsset == null)
                {
                    Debug.LogWarning($"[VRShop] Model not found: {product.ModelPath}");
                    continue;
                }

                var prefabPath = $"{PRODUCTS_PREFAB_DIR}/{product.Name}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                {
                    AssetDatabase.DeleteAsset(prefabPath);
                }

                var root = new GameObject(product.Name);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
                instance.transform.SetParent(root.transform);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                var scale = CalculateUniformScale(instance, product.TargetSize);
                instance.transform.localScale = scale;

                var bounds = CalculateBounds(instance);
                instance.transform.localPosition = new Vector3(0, -bounds.min.y, 0);

                RemoveCollidersRecursive(root);
                EnsureBasketItemComponents(root, product.Name);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void CreateBasketPrefab()
        {
            EnsureDirectoryExists(BASKET_PREFAB_DIR);

            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BASKET_PREFAB_PATH);
            if (existingPrefab != null)
            {
                AssetDatabase.DeleteAsset(BASKET_PREFAB_PATH);
            }

            var basketRoot = new GameObject("Basket");
            var basketAttachment = basketRoot.AddComponent<BasketAttachment>();

            var basketModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRShop/Models/Basket.fbx");
            if (basketModel != null)
            {
                var basketVisual = (GameObject)PrefabUtility.InstantiatePrefab(basketModel);
                basketVisual.name = "BasketVisual";
                basketVisual.transform.SetParent(basketRoot.transform);
                basketVisual.transform.localPosition = Vector3.zero;
                basketVisual.transform.localRotation = Quaternion.identity;
                basketVisual.transform.localScale = Vector3.one;

                // Normalize scale to roughly match previous placeholder size
                var visualBounds = CalculateBounds(basketVisual);
                var maxExtent = Mathf.Max(visualBounds.size.x, visualBounds.size.y, visualBounds.size.z);
                if (maxExtent > 0.0001f)
                {
                    var uniformScale = 0.35f / maxExtent;
                    basketVisual.transform.localScale = Vector3.one * uniformScale;
                }
            }
            else
            {
                Debug.LogWarning("[VRShop] Basket model not found at Assets/VRShop/Models/Basket.fbx, using placeholder.");
                var basketBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                basketBody.name = "BasketBody";
                basketBody.transform.SetParent(basketRoot.transform);
                basketBody.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                basketBody.transform.localScale = new Vector3(0.35f, 0.15f, 0.25f);
            }

            var basketBodyCollider = basketRoot.AddComponent<BoxCollider>();
            basketBodyCollider.isTrigger = false;
            var basketBounds = CalculateBounds(basketRoot);
            basketBodyCollider.center = basketRoot.transform.InverseTransformPoint(basketBounds.center);
            basketBodyCollider.size = basketBounds.size;

            var basketContent = new GameObject("BasketContent");
            basketContent.transform.SetParent(basketRoot.transform);
            basketContent.transform.localPosition = Vector3.zero;

            var basketTrigger = new GameObject("BasketTrigger");
            basketTrigger.transform.SetParent(basketRoot.transform);
            var triggerCollider = basketTrigger.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            var triggerCenter = basketBounds.center + new Vector3(0f, basketBounds.extents.y * 0.5f, 0f);
            basketTrigger.transform.localPosition = basketRoot.transform.InverseTransformPoint(triggerCenter);
            triggerCollider.size = new Vector3(basketBounds.size.x * 0.8f, basketBounds.size.y * 0.6f, basketBounds.size.z * 0.8f);

            var triggerScript = basketTrigger.AddComponent<BasketTrigger>();
            triggerScript.basketRoot = basketRoot.transform;
            triggerScript.itemContainer = basketContent.transform;
            triggerScript.itemSpacing = new Vector3(0.08f, 0.06f, 0.08f);

            basketAttachment.basketRoot = basketRoot.transform;

            PrefabUtility.SaveAsPrefabAsset(basketRoot, BASKET_PREFAB_PATH);
            DestroyImmediate(basketRoot);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void CreateScene()
        {
            EnsureDirectoryExists("Assets/VRShop/Scenes");

            var wallMaterial = AssetDatabase.LoadAssetAtPath<Material>(WALL_MATERIAL_PATH);
            var ceilingMaterial = AssetDatabase.LoadAssetAtPath<Material>(CEILING_MATERIAL_PATH);
            var accentWallMaterial = AssetDatabase.LoadAssetAtPath<Material>(ACCENT_WALL_MATERIAL_PATH);
            var floorMaterial = AssetDatabase.LoadAssetAtPath<Material>(FLOOR_MATERIAL_PATH);
            var floorTileMaterial = AssetDatabase.LoadAssetAtPath<Material>(FLOOR_TILE_MATERIAL_PATH);
            var fixtureMaterial = AssetDatabase.LoadAssetAtPath<Material>(FIXTURE_MATERIAL_PATH);
            var emissivePanelMaterial = AssetDatabase.LoadAssetAtPath<Material>(EMISSIVE_PANEL_MATERIAL_PATH);
            var shopifyMaterial = AssetDatabase.LoadAssetAtPath<Material>(SHOPIFY_MATERIAL_PATH);
            var brandMaterial = AssetDatabase.LoadAssetAtPath<Material>(BRAND_MATERIAL_PATH);

            if (wallMaterial == null)
            {
                Debug.LogError("[VRShop] Wall material not found!");
                return;
            }

            // Create scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var existingRoom = GameObject.Find("ShopRoom");
            if (existingRoom != null)
            {
                DestroyImmediate(existingRoom);
            }
            var room = new GameObject("ShopRoom");

            // Floor
            var floor = CreateQuad("Floor", room.transform, Vector3.zero, Quaternion.Euler(90, 0, 0), new Vector3(roomWidth, roomDepth, 1));
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;

            // Ceiling
            var ceiling = CreateQuad("Ceiling", room.transform, new Vector3(0, roomHeight, 0), Quaternion.Euler(-90, 0, 0), new Vector3(roomWidth, roomDepth, 1));
            ceiling.GetComponent<Renderer>().sharedMaterial = ceilingMaterial ?? wallMaterial;

            // Walls
            CreateQuad("FrontWall", room.transform, new Vector3(0, roomHeight/2, roomDepth/2), Quaternion.Euler(0, 180, 0), new Vector3(roomWidth, roomHeight, 1)).GetComponent<Renderer>().sharedMaterial = wallMaterial;
            CreateQuad("BackWall", room.transform, new Vector3(0, roomHeight/2, -roomDepth/2), Quaternion.identity, new Vector3(roomWidth, roomHeight, 1)).GetComponent<Renderer>().sharedMaterial = wallMaterial;
            CreateQuad("LeftWall", room.transform, new Vector3(-roomWidth/2, roomHeight/2, 0), Quaternion.Euler(0, 90, 0), new Vector3(roomDepth, roomHeight, 1)).GetComponent<Renderer>().sharedMaterial = wallMaterial;
            CreateQuad("RightWall", room.transform, new Vector3(roomWidth/2, roomHeight/2, 0), Quaternion.Euler(0, -90, 0), new Vector3(roomDepth, roomHeight, 1)).GetComponent<Renderer>().sharedMaterial = accentWallMaterial ?? wallMaterial;

            // Floor tile inlay
            var floorTile = CreateQuad("FloorTile", room.transform, new Vector3(0, 0.01f, 0), Quaternion.Euler(90, 0, 0), new Vector3(roomWidth * 0.7f, roomDepth * 0.7f, 1));
            floorTile.GetComponent<Renderer>().sharedMaterial = floorTileMaterial ?? floorMaterial;

            // Emissive ceiling panels (baked)
            CreateEmissivePanel(room.transform, emissivePanelMaterial, new Vector3(-2.5f, roomHeight - 0.05f, 1.5f), new Vector3(2.5f, 0.3f, 1.2f));
            CreateEmissivePanel(room.transform, emissivePanelMaterial, new Vector3(2.5f, roomHeight - 0.05f, 1.5f), new Vector3(2.5f, 0.3f, 1.2f));
            CreateEmissivePanel(room.transform, emissivePanelMaterial, new Vector3(0f, roomHeight - 0.05f, -1.8f), new Vector3(3.5f, 0.3f, 1.4f));

            // SHOPIFY LOGO - FRONT wall, plastered facing the camera
            var shopifyLogo = CreateQuad("ShopifyLogo", room.transform,
                new Vector3(0, roomHeight / 2, roomDepth / 2 - 0.02f),
                Quaternion.Euler(0, 180, 0),
                new Vector3(-roomWidth * 0.8f, roomHeight * 0.5f, 1));
            shopifyLogo.GetComponent<Renderer>().sharedMaterial = shopifyMaterial;

            // BRAND LOGO - BACK wall, plastered facing into the room
            var brandLogo = CreateQuad("BrandLogo", room.transform,
                new Vector3(0, roomHeight / 2, -roomDepth / 2 + 0.02f),
                Quaternion.identity,
                new Vector3(-roomWidth * 0.8f, roomHeight * 0.5f, 1));
            brandLogo.GetComponent<Renderer>().sharedMaterial = brandMaterial;

            // Camera
            var cameraObj = new GameObject("VRShopCamera");
            cameraObj.transform.position = new Vector3(0, cameraHeight, 0);
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            camera.fieldOfView = 70f;
            camera.nearClipPlane = 0.1f;
            cameraObj.AddComponent<StationaryCameraController>();
            cameraObj.AddComponent<AudioListener>();

            // Hand anchor + basket
            var handAnchor = new GameObject("HandAnchor");
            handAnchor.transform.SetParent(cameraObj.transform);
            handAnchor.transform.localPosition = new Vector3(0.25f, -0.32f, 0.4f);
            handAnchor.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            var basketPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BASKET_PREFAB_PATH);
            if (basketPrefab != null)
            {
                var basketInstance = (GameObject)PrefabUtility.InstantiatePrefab(basketPrefab);
                basketInstance.transform.SetParent(handAnchor.transform);
                basketInstance.transform.localPosition = Vector3.zero;
                basketInstance.transform.localRotation = Quaternion.identity;

                var attachment = basketInstance.GetComponent<BasketAttachment>();
                if (attachment != null)
                {
                    attachment.anchor = handAnchor.transform;
                }
            }
            else
            {
                Debug.LogWarning($"[VRShop] Basket prefab not found at {BASKET_PREFAB_PATH}");
            }

            // ShopDataManager not needed when logos are hardcoded

            // Lighting - modern showroom feel (softer and wider)
            CreateSpotLight(room.transform, "SpotLight_FrontLeft", new Vector3(-2.5f, roomHeight - 0.2f, 2.5f), new Vector3(50f, -45f, 0f), 1300f, 70f);
            CreateSpotLight(room.transform, "SpotLight_FrontRight", new Vector3(2.5f, roomHeight - 0.2f, 2.5f), new Vector3(50f, 45f, 0f), 1300f, 70f);
            CreateSpotLight(room.transform, "SpotLight_BackLeft", new Vector3(-2.5f, roomHeight - 0.2f, -2.5f), new Vector3(50f, -135f, 0f), 1200f, 70f);
            CreateSpotLight(room.transform, "SpotLight_BackRight", new Vector3(2.5f, roomHeight - 0.2f, -2.5f), new Vector3(50f, 135f, 0f), 1200f, 70f);
            CreateSpotLight(room.transform, "SpotLight_Center", new Vector3(0f, roomHeight - 0.1f, 0f), new Vector3(90f, 0f, 0f), 1100f, 80f);

            var fillLight = new GameObject("FillLight");
            fillLight.transform.SetParent(room.transform);
            fillLight.transform.rotation = Quaternion.Euler(50, -30, 0);
            var light = fillLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.35f;
            light.color = new Color(0.98f, 0.98f, 1f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.7f, 0.72f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.56f, 0.6f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.36f, 0.38f);

            // Fixtures: display tables
            var tableCenter = CreateFixtureTable(room.transform, fixtureMaterial, new Vector3(0, 0.45f, 0), new Vector3(1.6f, 0.9f, 2.4f));
            var tableLeft = CreateFixtureTable(room.transform, fixtureMaterial, new Vector3(-2.2f, 0.45f, 0.6f), new Vector3(1.0f, 0.9f, 1.6f));
            var tableRight = CreateFixtureTable(room.transform, fixtureMaterial, new Vector3(2.2f, 0.45f, -0.6f), new Vector3(1.0f, 0.9f, 1.6f));

            // Fixtures: shelving units
            CreateShelfUnit(room.transform, fixtureMaterial, new Vector3(-roomWidth / 2 + 0.6f, 1.0f, 0f), new Vector3(0.3f, 2.0f, 2.2f));
            CreateShelfUnit(room.transform, fixtureMaterial, new Vector3(roomWidth / 2 - 0.6f, 1.0f, 0f), new Vector3(0.3f, 2.0f, 2.2f));

            // Table fill lights (subtle, no shadows)
            CreateFillPointLight(room.transform, "FillTable_Center", new Vector3(0f, 2.2f, 0f), 250f, 3.5f);
            CreateFillPointLight(room.transform, "FillTable_Left", new Vector3(-2.2f, 2.1f, 0.6f), 220f, 3.0f);
            CreateFillPointLight(room.transform, "FillTable_Right", new Vector3(2.2f, 2.1f, -0.6f), 220f, 3.0f);

            // Place product models
            PlaceProductPrefab("Product_Box_Small", room.transform, new Vector3(0f, 0.95f, 0f));
            PlaceProductPrefab("Product_Box_Tall", room.transform, new Vector3(-2.2f, 0.95f, 0.6f));
            PlaceProductPrefab("Product_Star", room.transform, new Vector3(2.2f, 0.95f, -0.6f));
            PlaceProductPrefab("Product_CubeHollow", room.transform, new Vector3(-4.0f, 1.3f, 0.6f));
            PlaceProductPrefab("Product_Platform", room.transform, new Vector3(4.0f, 1.3f, -0.6f));

            // Place Shoppy model with wanderer script
            PlaceShoppyModel(room.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene, SCENE_PATH, true);
            if (!saved)
            {
                Debug.LogError("[VRShop] Failed to save scene.");
            }
            EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

            var loadedShopifyLogo = GameObject.Find("ShopifyLogo");
            var loadedBrandLogo = GameObject.Find("BrandLogo");

            Debug.Log("[VRShop] Scene created! Press Play to test.");
            Debug.Log($"[VRShop] Logo objects after reload: ShopifyLogo={(loadedShopifyLogo != null ? "ok" : "missing")}, BrandLogo={(loadedBrandLogo != null ? "ok" : "missing")}");
        }

        private GameObject CreateQuad(string name, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent);
            quad.transform.localPosition = pos;
            quad.transform.localRotation = rot;
            quad.transform.localScale = scale;
            
            var col = quad.GetComponent<Collider>();
            if (col) DestroyImmediate(col);
            
            return quad;
        }

        private static void SetLitMaterialProps(Material material, Color baseColor, float metallic, float smoothness)
        {
            if (material == null) return;

            material.color = baseColor;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", baseColor);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
        }

        private static void CreateSpotLight(Transform parent, string name, Vector3 position, Vector3 eulerAngles, float intensity, float angle)
        {
            var lightObj = new GameObject(name);
            lightObj.transform.SetParent(parent);
            lightObj.transform.localPosition = position;
            lightObj.transform.localRotation = Quaternion.Euler(eulerAngles);

            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Spot;
            light.intensity = intensity;
            light.spotAngle = angle;
            light.range = 12f;
            light.color = new Color(0.98f, 0.98f, 1.0f);
        }

        private static void CreateFillPointLight(Transform parent, string name, Vector3 position, float intensity, float range)
        {
            var lightObj = new GameObject(name);
            lightObj.transform.SetParent(parent);
            lightObj.transform.localPosition = position;

            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = intensity;
            light.range = range;
            light.color = new Color(0.98f, 0.98f, 1.0f);
            light.shadows = LightShadows.None;
        }

        private GameObject CreateEmissivePanel(Transform parent, Material material, Vector3 position, Vector3 size)
        {
            var panel = CreateQuad("CeilingPanel", parent, position, Quaternion.Euler(-90f, 0f, 0f), size);
            if (material != null)
            {
                panel.GetComponent<Renderer>().sharedMaterial = material;
            }

            GameObjectUtility.SetStaticEditorFlags(panel, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
            return panel;
        }

        private static GameObject CreateFixtureTable(Transform parent, Material material, Vector3 position, Vector3 size)
        {
            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "DisplayTable";
            table.transform.SetParent(parent);
            table.transform.localPosition = position;
            table.transform.localScale = size;
            if (material != null)
            {
                table.GetComponent<Renderer>().sharedMaterial = material;
            }
            var col = table.GetComponent<Collider>();
            if (col) DestroyImmediate(col);
            return table;
        }

        private static void CreateShelfUnit(Transform parent, Material material, Vector3 position, Vector3 size)
        {
            var shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelf.name = "ShelfUnit";
            shelf.transform.SetParent(parent);
            shelf.transform.localPosition = position;
            shelf.transform.localScale = size;
            if (material != null)
            {
                shelf.GetComponent<Renderer>().sharedMaterial = material;
            }
            var col = shelf.GetComponent<Collider>();
            if (col) DestroyImmediate(col);
        }

        private void PlaceProductPrefab(string prefabName, Transform parent, Vector3 position)
        {
            var prefabPath = $"{PRODUCTS_PREFAB_DIR}/{prefabName}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[VRShop] Product prefab not found: {prefabPath}");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = prefabName;
            instance.transform.SetParent(parent);
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.identity;
        }

        private void PlaceShoppyModel(Transform parent)
        {
            const string SHOPPY_MODEL_PATH = "Assets/Models/shoppy_model.obj";
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SHOPPY_MODEL_PATH);
            
            if (modelAsset == null)
            {
                Debug.LogWarning($"[VRShop] Shoppy model not found at: {SHOPPY_MODEL_PATH}");
                return;
            }

            // Create root GameObject for Shoppy
            var shoppyRoot = new GameObject("ShoppyModel");
            shoppyRoot.transform.SetParent(parent);
            // Start at a random position within the wander area
            float startX = Random.Range(-roomWidth * 0.3f, roomWidth * 0.3f);
            float startZ = Random.Range(-roomDepth * 0.3f, roomDepth * 0.3f);
            shoppyRoot.transform.localPosition = new Vector3(startX, 0f, startZ);
            shoppyRoot.transform.localRotation = Quaternion.identity;

            // Instantiate the model
            var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            modelInstance.transform.SetParent(shoppyRoot.transform);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            // Calculate scale to make it a reasonable size (about 1.5 units tall)
            var bounds = CalculateBounds(modelInstance);
            var targetHeight = 1.5f;
            if (bounds.size.y > 0.001f)
            {
                var scale = targetHeight / bounds.size.y;
                modelInstance.transform.localScale = Vector3.one * scale;
            }

            // Position model so bottom is at ground level
            bounds = CalculateBounds(modelInstance);
            modelInstance.transform.localPosition = new Vector3(0, -bounds.min.y, 0);

            // Add ShoppyWanderer script
            var wanderer = shoppyRoot.AddComponent<ShoppyWanderer>();
            
            // Configure wanderer settings using SerializedObject
            var serializedWanderer = new SerializedObject(wanderer);
            serializedWanderer.FindProperty("wanderCenter").vector3Value = Vector3.zero;
            serializedWanderer.FindProperty("wanderSize").vector2Value = new Vector2(roomWidth * 0.8f, roomDepth * 0.8f);
            serializedWanderer.FindProperty("groundHeight").floatValue = 0f;
            serializedWanderer.ApplyModifiedProperties();

            Debug.Log("[VRShop] Shoppy model placed with wanderer script!");
        }

        private static void RemoveCollidersRecursive(GameObject root)
        {
            var colliders = root.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                DestroyImmediate(collider);
            }
        }

        private static void EnsureBasketItemComponents(GameObject root, string itemId)
        {
            var basketItem = root.GetComponent<BasketItem>();
            if (basketItem == null)
            {
                basketItem = root.AddComponent<BasketItem>();
            }

            basketItem.itemId = itemId;
            basketItem.displayName = itemId.Replace("Product_", string.Empty);
            basketItem.canBeCollected = true;

            var collider = root.GetComponent<Collider>();
            if (collider == null)
            {
                var boxCollider = root.AddComponent<BoxCollider>();
                var bounds = CalculateBounds(root);
                boxCollider.center = root.transform.InverseTransformPoint(bounds.center);
                boxCollider.size = bounds.size;
            }

            var rigidbody = root.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = root.AddComponent<Rigidbody>();
            }
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static Vector3 CalculateUniformScale(GameObject root, float targetSize)
        {
            var bounds = CalculateBounds(root);
            var max = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (max <= 0.0001f)
            {
                return Vector3.one;
            }

            var scale = targetSize / max;
            return Vector3.one * scale;
        }

        private readonly struct ProductDefinition
        {
            public ProductDefinition(string name, string modelPath, float targetSize)
            {
                Name = name;
                ModelPath = modelPath;
                TargetSize = targetSize;
            }

            public string Name { get; }
            public string ModelPath { get; }
            public float TargetSize { get; }
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parts = path.Split('/');
                var current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }

        private static void DisableCullingIfAvailable(Material material)
        {
            if (material == null) return;

            if (material.HasProperty("_Cull"))
            {
                material.SetInt("_Cull", 0);
            }

            if (material.HasProperty("_CullMode"))
            {
                material.SetInt("_CullMode", 0);
            }
        }
    }
}
