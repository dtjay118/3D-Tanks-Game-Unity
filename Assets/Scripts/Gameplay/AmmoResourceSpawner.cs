using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AmmoResourceSpawner : MonoBehaviour
{
    [System.Serializable]
    public class ResourceSpawnConfig
    {
        public string m_Name = "Standard Ammo";
        public Rigidbody m_ShellPrefab;
        public int m_AmmoAmount = 5;
        public float m_Weight = 1f;
    }

    [Header("Prefabs")]
    public AmmoResourcePoint m_ResourcePointPrefab;
    public bool m_UseRuntimeGeneratedPointWhenPrefabMissing = true;

    [Header("Spawn Area")]
    public Vector3 m_SpawnCenter = Vector3.zero;
    public Vector3 m_SpawnSize = new Vector3(40f, 10f, 40f);
    public LayerMask m_GroundMask = -1;

    [Header("Spawn Settings")]
    public int m_PointCount = 6;
    public float m_MinDistanceBetweenPoints = 6f;
    public int m_MaxTriesPerPoint = 25;
    public bool m_ClearOldChildrenOnSpawn = true;

    [Header("Resource Types")]
    public ResourceSpawnConfig[] m_ResourceTypes;
    public bool m_AutoBuildTypesFromTankWeapons = true;
    public int m_DefaultAmmoAmountPerPoint = 5;
    public bool m_DelayedAutoSpawn = true;
    public float m_InitialSpawnDelay = 0.5f;
    public int m_AutoSpawnRetryCount = 8;
    public float m_AutoSpawnRetryInterval = 0.5f;

    private void Start()
    {
        if (m_DelayedAutoSpawn)
        {
            StartCoroutine(SpawnWhenReady());
        }
        else
        {
            SpawnResourcePoints();
        }
    }

    [ContextMenu("Spawn Resource Points")]
    public void SpawnResourcePoints()
    {
        ResourceSpawnConfig[] effectiveTypes = GetEffectiveResourceTypes();
        if (effectiveTypes == null || effectiveTypes.Length == 0)
        {
            Debug.LogWarning("AmmoResourceSpawner: No available resource types.");
            return;
        }
        if (m_ResourcePointPrefab == null && !m_UseRuntimeGeneratedPointWhenPrefabMissing)
        {
            Debug.LogWarning("AmmoResourceSpawner: Missing resource point prefab.");
            return;
        }

        if (m_ClearOldChildrenOnSpawn)
        {
            ClearSpawnedPoints();
        }

        int spawnedCount = 0;
        for (int i = 0; i < m_PointCount; i++)
        {
            if (!TryFindSpawnPosition(out Vector3 spawnPosition))
            {
                continue;
            }

            AmmoResourcePoint point = CreatePointInstance(spawnPosition);
            if (point == null)
            {
                continue;
            }

            ApplyRandomType(point, effectiveTypes);
            spawnedCount++;
        }

        Debug.Log($"AmmoResourceSpawner: Spawned {spawnedCount}/{m_PointCount} resource points.");
    }

    private IEnumerator SpawnWhenReady()
    {
        if (m_InitialSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(m_InitialSpawnDelay);
        }

        int retriesLeft = Mathf.Max(0, m_AutoSpawnRetryCount);
        while (retriesLeft >= 0)
        {
            ResourceSpawnConfig[] effectiveTypes = GetEffectiveResourceTypes();
            if (effectiveTypes != null && effectiveTypes.Length > 0)
            {
                SpawnResourcePoints();
                yield break;
            }

            if (retriesLeft == 0)
            {
                break;
            }

            retriesLeft--;
            yield return new WaitForSeconds(Mathf.Max(0.05f, m_AutoSpawnRetryInterval));
        }

        Debug.LogWarning("AmmoResourceSpawner: Could not build resource types after retries. Ensure tanks are spawned and weapon shells are assigned.");
    }

    private void ClearSpawnedPoints()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    private bool TryFindSpawnPosition(out Vector3 spawnPosition)
    {
        for (int i = 0; i < m_MaxTriesPerPoint; i++)
        {
            float x = Random.Range(-m_SpawnSize.x * 0.5f, m_SpawnSize.x * 0.5f);
            float z = Random.Range(-m_SpawnSize.z * 0.5f, m_SpawnSize.z * 0.5f);
            Vector3 candidate = m_SpawnCenter + new Vector3(x, m_SpawnSize.y * 0.5f, z);

            if (!TryProjectToGround(candidate, out Vector3 grounded))
            {
                continue;
            }

            if (IsTooCloseToExistingPoints(grounded))
            {
                continue;
            }

            spawnPosition = grounded;
            return true;
        }

        spawnPosition = Vector3.zero;
        return false;
    }

    private bool TryProjectToGround(Vector3 samplePoint, out Vector3 groundedPosition)
    {
        if (Physics.Raycast(samplePoint, Vector3.down, out RaycastHit hit, m_SpawnSize.y + 5f, m_GroundMask))
        {
            groundedPosition = hit.point + Vector3.up * 0.2f;
            return true;
        }

        groundedPosition = Vector3.zero;
        return false;
    }

    private bool IsTooCloseToExistingPoints(Vector3 position)
    {
        float minDistanceSqr = m_MinDistanceBetweenPoints * m_MinDistanceBetweenPoints;
        for (int i = 0; i < transform.childCount; i++)
        {
            Vector3 existingPos = transform.GetChild(i).position;
            if ((existingPos - position).sqrMagnitude < minDistanceSqr)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyRandomType(AmmoResourcePoint point, ResourceSpawnConfig[] typePool)
    {
        ResourceSpawnConfig type = PickWeightedType(typePool);
        if (type == null)
        {
            return;
        }

        point.m_PointName = type.m_Name;
        point.m_ShellPrefab = type.m_ShellPrefab;
        point.m_AmmoAmount = type.m_AmmoAmount;
        point.RefreshNameLabel();

        Renderer renderer = point.GetComponentInChildren<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            renderer.sharedMaterial.color = GetColorFromTypeName(type.m_Name);
        }
    }

    private AmmoResourcePoint CreatePointInstance(Vector3 spawnPosition)
    {
        if (m_ResourcePointPrefab != null)
        {
            return Instantiate(m_ResourcePointPrefab, spawnPosition, Quaternion.identity, transform);
        }

        if (!m_UseRuntimeGeneratedPointWhenPrefabMissing)
        {
            return null;
        }

        GameObject root = new GameObject("AmmoResourcePoint_Runtime");
        root.transform.SetParent(transform);
        root.transform.position = spawnPosition;

        SphereCollider trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 3.5f;

        AmmoResourcePoint point = root.AddComponent<AmmoResourcePoint>();
        point.m_RequiredStayTime = 2f;
        point.m_RefillCooldown = 2.5f;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "Visual";
        visual.transform.SetParent(root.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(3.5f, 0.2f, 3.5f);

        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
        {
            Destroy(visualCollider);
        }

        return point;
    }

    private ResourceSpawnConfig PickWeightedType(ResourceSpawnConfig[] typePool)
    {
        float totalWeight = 0f;
        for (int i = 0; i < typePool.Length; i++)
        {
            totalWeight += Mathf.Max(0f, typePool[i].m_Weight);
        }

        if (totalWeight <= 0f)
        {
            return typePool[Random.Range(0, typePool.Length)];
        }

        float randomWeight = Random.value * totalWeight;
        float accumulatedWeight = 0f;
        for (int i = 0; i < typePool.Length; i++)
        {
            accumulatedWeight += Mathf.Max(0f, typePool[i].m_Weight);
            if (randomWeight <= accumulatedWeight)
            {
                return typePool[i];
            }
        }

        return typePool[typePool.Length - 1];
    }

    private ResourceSpawnConfig[] GetEffectiveResourceTypes()
    {
        if (!m_AutoBuildTypesFromTankWeapons && m_ResourceTypes != null && m_ResourceTypes.Length > 0)
        {
            return m_ResourceTypes;
        }

        ResourceSpawnConfig[] autoTypes = BuildResourceTypesFromTankWeapons();
        if (autoTypes.Length > 0)
        {
            return autoTypes;
        }

        return m_ResourceTypes;
    }

    private ResourceSpawnConfig[] BuildResourceTypesFromTankWeapons()
    {
        TankShooting[] shootingComponents = FindObjectsOfType<TankShooting>();
        if (shootingComponents == null || shootingComponents.Length == 0)
        {
            return new ResourceSpawnConfig[0];
        }

        List<ResourceSpawnConfig> generated = new List<ResourceSpawnConfig>();

        for (int i = 0; i < shootingComponents.Length; i++)
        {
            TankShooting.WeaponConfig[] weapons = shootingComponents[i].WeaponConfigs;
            if (weapons == null)
            {
                continue;
            }

            for (int j = 0; j < weapons.Length; j++)
            {
                TankShooting.WeaponConfig weapon = weapons[j];
                if (weapon == null || weapon.m_ShellPrefab == null)
                {
                    continue;
                }

                generated.Add(new ResourceSpawnConfig
                {
                    m_Name = SanitizeDisplayName(weapon.m_WeaponName),
                    m_ShellPrefab = weapon.m_ShellPrefab,
                    m_AmmoAmount = Mathf.Max(1, m_DefaultAmmoAmountPerPoint),
                    m_Weight = 1f
                });
            }
        }

        return generated.ToArray();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(m_SpawnCenter + Vector3.up * 0.1f, new Vector3(m_SpawnSize.x, 0.1f, m_SpawnSize.z));
    }

    private Color GetColorFromTypeName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return Color.cyan;
        }

        int hash = typeName.GetHashCode();
        float hue = Mathf.Abs(hash % 1000) / 1000f;
        return Color.HSVToRGB(hue, 0.65f, 0.95f);
    }

    private string SanitizeDisplayName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
        {
            return "Ammo";
        }

        int bracketStart = rawName.IndexOf('(');
        string nameWithoutBracket = bracketStart >= 0 ? rawName.Substring(0, bracketStart) : rawName;
        string cleaned = nameWithoutBracket.Replace("Ammo", "").Trim();
        return string.IsNullOrEmpty(cleaned) ? "Ammo" : cleaned;
    }
}
