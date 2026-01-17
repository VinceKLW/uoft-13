using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

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

        [MenuItem("VRShop/Quick Setup (Create Everything)", false, 0)]
        public static void QuickSetup()
        {
            var builder = ScriptableObject.CreateInstance<VRShopRoomBuilder>();
            try
            {
                builder.CreateAllMaterials();
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
