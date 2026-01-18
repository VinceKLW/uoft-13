using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VRShop
{
    /// <summary>
    /// Makes the Shoppy model wander around the shop scene randomly
    /// </summary>
    public class ShoppyWanderer : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Walking speed in units per second")]
        [SerializeField] private float walkSpeed = 0.6f;
        
        [Tooltip("How fast the model rotates to face movement direction")]
        [SerializeField] private float rotationSpeed = 5f;

        [Tooltip("Yaw offset to align model's forward direction (degrees)")]
        [SerializeField] private float headingOffsetDegrees = 0f;
        
        [Tooltip("How long to wait at each destination before moving to the next")]
        [SerializeField] private float waitTime = 2f;

        [Header("Body Bob")]
        [Tooltip("Enable simple body bob while moving")]
        [SerializeField] private bool enableBodyBob = true;

        [Tooltip("Bob height in units")]
        [SerializeField] private float bobAmplitude = 0.05f;

        [Tooltip("Bob cycles per second")]
        [SerializeField] private float bobFrequency = 2f;

        [Header("Wander Area")]
        [Tooltip("Center of the wander area (usually shop center)")]
        [SerializeField] private Vector3 wanderCenter = Vector3.zero;
        
        [Tooltip("Size of the wander area (X = width, Z = depth). Should match floor size.")]
        [SerializeField] private Vector2 wanderSize = new Vector2(9f, 9f); // Slightly smaller than 10x10 floor
        
        [Tooltip("Y position to keep the model at")]
        [SerializeField] private float groundHeight = 0f;

        [Header("References")]
        [Tooltip("Optional: move this transform instead of the object holding this script")]
        [SerializeField] private Transform moveRoot;

        [Tooltip("Optional: use this collider to clamp movement to the floor bounds")]
        [SerializeField] private Collider floorCollider;

        [Tooltip("Extra lift above the floor surface")]
        [SerializeField] private float groundOffset = 0f;

        [Tooltip("Optional: separate bounds collider to keep Shoppy inside the walls")]
        [SerializeField] private Collider movementBoundsCollider;

        [Tooltip("Padding to keep away from bounds edges")]
        [SerializeField] private float boundaryPadding = 0.2f;

        [Header("Collision Settings")]
        [Tooltip("Use trigger collider to avoid physical clipping")]
        [SerializeField] private bool useTriggerCollider = true;

        [Tooltip("How far to redirect when colliding")]
        [SerializeField] private float collisionRedirectDistance = 2f;

        [Tooltip("Cooldown between collision redirects (seconds)")]
        [SerializeField] private float collisionRedirectCooldown = 0.2f;

        [Header("Rendering")]
        [Tooltip("Force an unlit material on Shoppy to avoid lighting flicker")]
        [SerializeField] private bool forceUnlitMaterial = true;

        [Tooltip("Tint for the unlit material")]
        [SerializeField] private Color unlitColor = Color.white;

        private Vector3 targetPosition;
        private float waitTimer = 0f;
        private bool isWaiting = false;
        private float collisionCooldownTimer = 0f;
        private Rigidbody rb;
        private Collider shoppyCollider;
        private Transform moveTransform;
        private Material[] cachedMaterials; // Cache materials to prevent recreation
        private MaterialPropertyBlock[] cachedMPBs; // Cache material property blocks per renderer
        private Renderer[] cachedRenderers; // Cache renderers
        private bool materialsApplied = false; // Track if materials have been applied

        private void Start()
        {
            moveTransform = moveRoot != null ? moveRoot : transform;
            AutoAssignFloorCollider();
            AutoAssignMovementBoundsCollider();
            EnsurePhysicsComponents();
            SyncRigidbodyTransform();
            
            // Cache renderers once
            cachedRenderers = moveTransform.GetComponentsInChildren<Renderer>();
            
            // Apply lighting and material settings once
            DisableBakedLighting();
            ApplyUnlitMaterialIfNeeded();
            
            // Ensure settings persist
            materialsApplied = true;

            // Ensure we start at ground level
            Vector3 pos = GetCurrentPosition();
            pos = SnapToFloor(pos);
            SetCurrentPosition(pos);
            
            // Set initial target position (make sure it's far enough away)
            do
            {
                targetPosition = GetRandomPosition();
            } while (Vector3.Distance(GetCurrentPosition(), targetPosition) < 1f); // Ensure at least 1 unit away
            
            Debug.Log($"[ShoppyWanderer] Started at {GetCurrentPosition()}, moving to {targetPosition}");
        }

        private void FixedUpdate()
        {
            // Safety check - ensure we're enabled
            if (!enabled) return;

            if (collisionCooldownTimer > 0f)
            {
                collisionCooldownTimer -= Time.fixedDeltaTime;
            }
            
            if (isWaiting)
            {
                waitTimer -= Time.fixedDeltaTime;
                if (waitTimer <= 0f)
                {
                    isWaiting = false;
                    targetPosition = GetRandomPosition();
                }
                return;
            }

            // Calculate direction to target
            Vector3 currentPosition = GetCurrentPosition();
            Vector3 direction = (targetPosition - currentPosition);
            direction.y = 0f; // Keep movement on horizontal plane
            float distance = direction.magnitude;

            // If we've reached the target, wait
            if (distance < 0.1f)
            {
                isWaiting = true;
                waitTimer = waitTime;
                return;
            }

            // Rotate towards target (Y axis only - no tilting)
            if (direction.magnitude > 0.1f)
            {
                // Ensure direction is purely horizontal
                Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z).normalized;
                if (flatDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(flatDirection);
                    if (Mathf.Abs(headingOffsetDegrees) > 0.001f)
                    {
                        targetRotation *= Quaternion.Euler(0f, headingOffsetDegrees, 0f);
                    }
                    
                    // Only interpolate Y rotation to prevent any tilting
                    float currentY = GetCurrentRotation().eulerAngles.y;
                    float targetY = targetRotation.eulerAngles.y;
                    float newY = Mathf.LerpAngle(currentY, targetY, rotationSpeed * Time.fixedDeltaTime);
                    SetCurrentRotation(Quaternion.Euler(0f, newY, 0f));
                }
            }

            // Move towards target
            Vector3 movement = direction.normalized * walkSpeed * Time.fixedDeltaTime;
            MoveCurrentPosition(movement);
            
            // Keep at ground height and ALWAYS clamp to bounds
            Vector3 grounded = GetCurrentPosition();
            grounded = ClampToBounds(grounded); // Hard clamp first
            grounded = SnapToFloor(grounded);   // Then snap to floor height
            grounded = ClampToBounds(grounded); // Clamp again after snap (belt and suspenders)
            
            if (enableBodyBob && distance > 0.1f)
            {
                // Use fixed time for consistent bob in physics updates
                float bob = Mathf.Sin(Time.fixedTime * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
                grounded.y += bob;
            }
            SetCurrentPosition(grounded);
            
            // Force upright rotation (no tilting) every frame
            Vector3 currentEuler = moveTransform.eulerAngles;
            if (Mathf.Abs(currentEuler.x) > 1f || Mathf.Abs(currentEuler.z) > 1f)
            {
                moveTransform.rotation = Quaternion.Euler(0f, currentEuler.y, 0f);
            }
            
            // Debug: Log movement occasionally
            if (Random.Range(0f, 1f) < 0.01f) // 1% chance per frame
            {
                Debug.Log($"[ShoppyWanderer] Moving from {GetCurrentPosition()} towards {targetPosition}, distance: {distance:F2}");
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            RedirectOnCollision(collision);
        }

        private void OnTriggerEnter(Collider other)
        {
            RedirectOnCollision(other);
        }

        private void RedirectOnCollision(Collision collision)
        {
            if (collisionCooldownTimer > 0f) return;
            
            // Ignore floor and furniture collisions - only redirect for walls/obstacles
            if (ShouldIgnoreCollider(collision.collider)) return;

            Vector3 away = Vector3.zero;
            if (collision.contactCount > 0)
            {
                away = collision.GetContact(0).normal;
            }
            if (away.sqrMagnitude < 0.001f)
            {
                away = (GetCurrentPosition() - collision.transform.position);
            }

            RedirectToNewTarget(away);
        }

        private void RedirectOnCollision(Collider other)
        {
            if (collisionCooldownTimer > 0f) return;
            if (other == null) return;
            
            // Ignore floor and furniture collisions - only redirect for walls/obstacles
            if (ShouldIgnoreCollider(other)) return;

            Vector3 away = (GetCurrentPosition() - other.transform.position);
            RedirectToNewTarget(away);
        }
        
        private bool ShouldIgnoreCollider(Collider col)
        {
            if (col == null) return true;
            
            // Ignore the floor collider we use for snapping
            if (col == floorCollider) return true;
            if (col == movementBoundsCollider) return true;
            
            // Ignore floor and furniture by name
            string name = col.gameObject.name.ToLower();
            if (name.Contains("floor") || 
                name.Contains("table") || 
                name.Contains("shelf") ||
                name.Contains("ceiling") ||
                name.Contains("tile"))
            {
                return true;
            }
            
            return false;
        }

        private void RedirectToNewTarget(Vector3 away)
        {
            Vector3 direction = away;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = (GetCurrentPosition() - wanderCenter);
            }

            direction.Normalize();
            targetPosition = GetPositionInDirection(direction, collisionRedirectDistance);
            isWaiting = false;
            waitTimer = 0f;
            collisionCooldownTimer = collisionRedirectCooldown;
        }

        private Vector3 GetRandomPosition()
        {
            Vector3 center = GetWanderCenter();
            Vector2 size = GetWanderSize();
            
            // Use smaller range to ensure we stay well within bounds
            float padding = boundaryPadding + 0.5f; // Extra padding for safety
            float halfX = Mathf.Max(0.5f, size.x / 2f - padding);
            float halfZ = Mathf.Max(0.5f, size.y / 2f - padding);
            
            float x = Random.Range(-halfX, halfX);
            float z = Random.Range(-halfZ, halfZ);
            
            Vector3 position = center + new Vector3(x, groundHeight, z);
            position = ClampToBounds(position); // Always clamp
            return SnapToFloor(position);
        }

        private Vector3 GetPositionInDirection(Vector3 direction, float distance)
        {
            Vector3 center = GetWanderCenter();
            Vector2 size = GetWanderSize();
            float halfX = size.x / 2f;
            float halfZ = size.y / 2f;

            Vector3 candidate = GetCurrentPosition() + direction * distance;
            candidate.x = Mathf.Clamp(candidate.x, center.x - halfX, center.x + halfX);
            candidate.z = Mathf.Clamp(candidate.z, center.z - halfZ, center.z + halfZ);
            candidate = SnapToFloor(candidate);

            if (Vector3.Distance(GetCurrentPosition(), candidate) < 0.5f)
            {
                return GetRandomPosition();
            }

            return candidate;
        }

        private void EnsurePhysicsComponents()
        {
            if (moveTransform != null)
            {
                // Ensure GameObject is not static (static objects can't move and cause lighting issues)
                if (moveTransform.gameObject.isStatic)
                {
                    moveTransform.gameObject.isStatic = false;
                }
                
                // Disable static batching and lightmapping (editor only)
                #if UNITY_EDITOR
                var flags = UnityEditor.GameObjectUtility.GetStaticEditorFlags(moveTransform.gameObject);
                flags &= ~UnityEditor.StaticEditorFlags.BatchingStatic;
                flags &= ~UnityEditor.StaticEditorFlags.ContributeGI;
                flags &= ~UnityEditor.StaticEditorFlags.OccluderStatic;
                flags &= ~UnityEditor.StaticEditorFlags.OccludeeStatic;
                UnityEditor.GameObjectUtility.SetStaticEditorFlags(moveTransform.gameObject, flags);
                #endif
            }

            if (rb == null)
            {
                rb = moveTransform.GetComponent<Rigidbody>();
            }

            if (rb == null)
            {
                rb = moveTransform.gameObject.AddComponent<Rigidbody>();
            }

            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            
            // Freeze rotation on X and Z to prevent tilting - only allow Y rotation (facing direction)
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            shoppyCollider = moveTransform.GetComponent<Collider>();
            if (shoppyCollider == null)
            {
                var capsule = moveTransform.gameObject.AddComponent<CapsuleCollider>();
                Bounds bounds = CalculateRenderBounds();
                float height = Mathf.Max(0.2f, bounds.size.y);
                float radius = Mathf.Max(0.1f, Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f);

                capsule.direction = 1; // Y axis
                capsule.height = height;
                capsule.radius = radius;
                capsule.center = moveTransform.InverseTransformPoint(bounds.center);
                shoppyCollider = capsule;
            }

            if (shoppyCollider != null)
            {
                shoppyCollider.isTrigger = useTriggerCollider;
            }
        }

        private void SyncRigidbodyTransform()
        {
            if (rb == null) return;
            // Use MovePosition and MoveRotation to avoid physics jitter
            rb.MovePosition(moveTransform.position);
            rb.MoveRotation(moveTransform.rotation);
        }

        private Bounds CalculateRenderBounds()
        {
            var renderers = moveTransform.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(GetCurrentPosition(), Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private Vector3 GetCurrentPosition()
        {
            return moveTransform.position;
        }

        private Quaternion GetCurrentRotation()
        {
            return moveTransform.rotation;
        }

        private void SetCurrentPosition(Vector3 position)
        {
            moveTransform.position = position;
            SyncRigidbodyTransform();
        }

        private void MoveCurrentPosition(Vector3 delta)
        {
            moveTransform.position += delta;
            SyncRigidbodyTransform();
        }

        private void SetCurrentRotation(Quaternion rotation)
        {
            moveTransform.rotation = rotation;
            SyncRigidbodyTransform();
        }

        private Vector3 SnapToFloor(Vector3 position)
        {
            if (floorCollider == null)
            {
                position.y = groundHeight;
                return position;
            }

            Vector3 origin = position;
            origin.y = floorCollider.bounds.max.y + 2f;
            Ray ray = new Ray(origin, Vector3.down);
            if (floorCollider.Raycast(ray, out RaycastHit hit, 10f))
            {
                position.y = hit.point.y;
                return ClampToBounds(ApplyFloorOffset(position, hit.point.y));
            }

            float fallbackY = floorCollider.bounds.max.y;
            position.y = fallbackY;
            return ClampToBounds(ApplyFloorOffset(position, fallbackY));
        }

        private Vector3 GetWanderCenter()
        {
            Collider boundsCollider = GetBoundsCollider();
            if (boundsCollider == null)
            {
                return wanderCenter;
            }

            Vector3 center = boundsCollider.bounds.center;
            center.y = groundHeight;
            return center;
        }

        private Vector2 GetWanderSize()
        {
            Collider boundsCollider = GetBoundsCollider();
            if (boundsCollider == null)
            {
                return wanderSize;
            }

            Vector3 size = boundsCollider.bounds.size;
            return new Vector2(size.x, size.z);
        }

        private Vector3 ClampToBounds(Vector3 position)
        {
            Collider boundsCollider = GetBoundsCollider();
            
            float minX, maxX, minZ, maxZ;
            
            if (boundsCollider != null)
            {
                Bounds bounds = boundsCollider.bounds;
                minX = bounds.min.x + boundaryPadding;
                maxX = bounds.max.x - boundaryPadding;
                minZ = bounds.min.z + boundaryPadding;
                maxZ = bounds.max.z - boundaryPadding;
            }
            else
            {
                // Fallback to wanderSize centered at wanderCenter
                Vector3 center = wanderCenter;
                float halfX = wanderSize.x / 2f - boundaryPadding;
                float halfZ = wanderSize.y / 2f - boundaryPadding;
                minX = center.x - halfX;
                maxX = center.x + halfX;
                minZ = center.z - halfZ;
                maxZ = center.z + halfZ;
            }
            
            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.z = Mathf.Clamp(position.z, minZ, maxZ);
            return position;
        }

        private Vector3 ApplyFloorOffset(Vector3 position, float floorY)
        {
            Bounds bounds = CalculateRenderBounds();
            float bottomY = bounds.min.y;
            float delta = floorY - bottomY + groundOffset;
            position.y += delta;
            return position;
        }

        private void AutoAssignFloorCollider()
        {
            if (floorCollider != null) return;

            var floor = GameObject.Find("Floor");
            if (floor == null) return;

            floorCollider = floor.GetComponent<Collider>();
        }

        private void AutoAssignMovementBoundsCollider()
        {
            if (movementBoundsCollider != null) return;
            if (floorCollider != null)
            {
                movementBoundsCollider = floorCollider;
                return;
            }

            var shopRoom = GameObject.Find("ShopRoom");
            if (shopRoom != null)
            {
                movementBoundsCollider = shopRoom.GetComponent<Collider>();
                if (movementBoundsCollider != null)
                {
                    return;
                }
            }

            var floor = GameObject.Find("Floor");
            if (floor == null) return;

            movementBoundsCollider = floor.GetComponent<Collider>();
        }

        private Collider GetBoundsCollider()
        {
            if (movementBoundsCollider != null)
            {
                return movementBoundsCollider;
            }

            return floorCollider;
        }

        private void DisableBakedLighting()
        {
            var renderers = cachedRenderers != null ? cachedRenderers : moveTransform.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;
                
                renderer.lightmapIndex = -1;
                renderer.lightmapScaleOffset = Vector4.zero;
                renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                // Force disable light probes completely to prevent flickering
                renderer.receiveShadows = false;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                
                // Also disable per-object light probes
                renderer.probeAnchor = null;
            }
        }
        
        private void LateUpdate()
        {
            // Re-apply lighting settings every frame to prevent Unity from re-enabling them
            if (materialsApplied && cachedRenderers != null)
            {
                for (int i = 0; i < cachedRenderers.Length; i++)
                {
                    var renderer = cachedRenderers[i];
                    if (renderer == null) continue;
                    
                    // Force these settings every frame to prevent flickering
                    if (renderer.lightProbeUsage != UnityEngine.Rendering.LightProbeUsage.Off)
                    {
                        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    }
                    if (renderer.reflectionProbeUsage != UnityEngine.Rendering.ReflectionProbeUsage.Off)
                    {
                        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                    }
                }
            }
        }

        private void ApplyUnlitMaterialIfNeeded()
        {
            if (!forceUnlitMaterial || materialsApplied) return;

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null)
            {
                unlitShader = Shader.Find("Unlit/Texture");
            }
            if (unlitShader == null)
            {
                return;
            }

            var renderers = cachedRenderers != null ? cachedRenderers : moveTransform.GetComponentsInChildren<Renderer>();
            if (cachedMaterials == null || cachedMaterials.Length != renderers.Length)
            {
                cachedMaterials = new Material[renderers.Length];
            }
            
            if (cachedMPBs == null || cachedMPBs.Length != renderers.Length)
            {
                cachedMPBs = new MaterialPropertyBlock[renderers.Length];
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;
                
                // Only create material once, reuse it
                if (cachedMaterials[i] == null)
                {
                    var original = renderer.sharedMaterial;
                    cachedMaterials[i] = new Material(unlitShader);
                    cachedMaterials[i].color = unlitColor;
                    if (original != null && original.mainTexture != null)
                    {
                        cachedMaterials[i].mainTexture = original.mainTexture;
                    }
                    // Disable all lighting features on the material
                    cachedMaterials[i].SetFloat("_Surface", 0); // Opaque
                    cachedMaterials[i].enableInstancing = false; // Disable instancing to prevent issues
                }
                
                // Set the cached material (only once)
                renderer.sharedMaterial = cachedMaterials[i];
                
                // Create and apply material property block once per renderer
                if (cachedMPBs[i] == null)
                {
                    cachedMPBs[i] = new MaterialPropertyBlock();
                    if (unlitShader.name.Contains("URP"))
                    {
                        cachedMPBs[i].SetColor("_BaseColor", unlitColor);
                    }
                    else
                    {
                        cachedMPBs[i].SetColor("_Color", unlitColor);
                    }
                    renderer.SetPropertyBlock(cachedMPBs[i]);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw wander area in editor
            Gizmos.color = Color.yellow;
            Vector3 center = wanderCenter;
            center.y = groundHeight;
            
            // Draw rectangle outline
            Vector3 corner1 = center + new Vector3(-wanderSize.x / 2f, 0, -wanderSize.y / 2f);
            Vector3 corner2 = center + new Vector3(wanderSize.x / 2f, 0, -wanderSize.y / 2f);
            Vector3 corner3 = center + new Vector3(wanderSize.x / 2f, 0, wanderSize.y / 2f);
            Vector3 corner4 = center + new Vector3(-wanderSize.x / 2f, 0, wanderSize.y / 2f);
            
            Gizmos.DrawLine(corner1, corner2);
            Gizmos.DrawLine(corner2, corner3);
            Gizmos.DrawLine(corner3, corner4);
            Gizmos.DrawLine(corner4, corner1);
            
            // Draw target position
            if (Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(targetPosition, 0.2f);
            }
        }
    }
}
