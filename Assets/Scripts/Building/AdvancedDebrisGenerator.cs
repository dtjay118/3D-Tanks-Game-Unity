using UnityEngine;
using System.Collections.Generic;

public class AdvancedDebrisGenerator : MonoBehaviour
{
    [Header("Fracture Method")]
    public FractureMethod m_FractureMethod = FractureMethod.SimpleFracture;
    public bool m_PreGenerateDebris = false;        // 预生成碎片（性能更好）
    
    [Header("Fracture Settings")]
    public int m_FragmentCount = 8;                 // 碎片数量
    public float m_MinFragmentSize = 0.1f;          // 最小碎片大小
    public float m_MaxFragmentSize = 1.0f;          // 最大碎片大小
    public bool m_UseOriginalMaterial = true;       // 使用原始材质
    public Material m_DebrisMaterial;               // 自定义碎片材质
    
    [Header("Physics Settings")]
    public float m_ExplosionForce = 500f;           // 爆炸力
    public float m_ExplosionRadius = 5f;            // 爆炸半径
    public float m_UpwardModifier = 0.3f;           // 向上的力
    public float m_DebrisLifetime = 10f;            // 碎片生命周期
    public float m_DebrisMass = 1f;                 // 碎片质量
    
    [Header("Visual Effects")]
    public bool m_AddTrailEffect = false;           // 添加拖尾效果
    public bool m_AddSmokeEffect = true;            // 添加烟雾效果
    public GameObject m_SmokeEffectPrefab;          // 烟雾效果预制体
    
    private List<GameObject> m_PreGeneratedDebris;
    private Mesh m_OriginalMesh;
    private Material[] m_OriginalMaterials;
    private bool m_IsGenerated = false;

    public enum FractureMethod
    {
        SimpleFracture,     // 简单网格分割
        GeometricDebris,    // 几何体碎片
        VoronoiFracture,    // Voronoi分割
        RadialFracture      // 径向分割
    }

    private void Start()
    {
        if (m_PreGenerateDebris)
        {
            PreGenerateDebris();
        }
    }

    public void PreGenerateDebris()
    {
        if (m_IsGenerated) return;

        // 获取原始网格和材质
        CacheOriginalMeshData();
        
        // 生成碎片但不激活
        m_PreGeneratedDebris = GenerateDebrisInternal(false);
        m_IsGenerated = true;
        
        Debug.Log($"预生成了 {m_PreGeneratedDebris.Count} 个碎片");
    }

    public List<GameObject> GenerateAndExplodeDebris(Vector3 explosionCenter)
    {
        List<GameObject> debris;
        
        if (m_PreGenerateDebris && m_IsGenerated)
        {
            // 使用预生成的碎片
            debris = m_PreGeneratedDebris;
            ActivatePreGeneratedDebris();
        }
        else
        {
            // 实时生成碎片
            debris = GenerateDebrisInternal(true);
        }

        // 应用爆炸力
        ApplyExplosionForce(debris, explosionCenter);
        
        // 添加视觉效果
        AddVisualEffects(explosionCenter);
        
        return debris;
    }

    private void CacheOriginalMeshData()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        
        if (meshFilter != null && meshFilter.mesh != null)
        {
            m_OriginalMesh = meshFilter.mesh;
        }
        
        if (meshRenderer != null)
        {
            m_OriginalMaterials = meshRenderer.materials;
        }
    }

    private List<GameObject> GenerateDebrisInternal(bool activate)
    {
        List<GameObject> debris = new List<GameObject>();
        
        switch (m_FractureMethod)
        {
            case FractureMethod.SimpleFracture:
                debris = GenerateSimpleFractureDebris(activate);
                break;
                
            case FractureMethod.GeometricDebris:
                debris = GenerateGeometricDebris(activate);
                break;
                
            case FractureMethod.VoronoiFracture:
                debris = GenerateVoronoiDebris(activate);
                break;
                
            case FractureMethod.RadialFracture:
                debris = GenerateRadialDebris(activate);
                break;
        }
        
        return debris;
    }

    private List<GameObject> GenerateSimpleFractureDebris(bool activate)
    {
        List<GameObject> debris = new List<GameObject>();
        
        if (m_OriginalMesh == null)
        {
            Debug.LogWarning("没有找到原始网格，使用几何体碎片替代");
            return GenerateGeometricDebris(activate);
        }

        // 使用SimpleFractureUtility分割网格
        List<Mesh> fragmentMeshes = SimpleFractureUtility.FractureMesh(
            m_OriginalMesh, m_FragmentCount, m_MinFragmentSize);

        foreach (Mesh fragmentMesh in fragmentMeshes)
        {
            GameObject fragment = CreateDebrisFromMesh(fragmentMesh, activate);
            debris.Add(fragment);
        }

        return debris;
    }

    private List<GameObject> GenerateGeometricDebris(bool activate)
    {
        Material material = GetDebrisMaterial();
        return SimpleFractureUtility.CreateSimpleDebris(
            transform, m_FragmentCount, material, m_MinFragmentSize, m_MaxFragmentSize);
    }

    private List<GameObject> GenerateVoronoiDebris(bool activate)
    {
        // 简化的Voronoi实现
        List<GameObject> debris = new List<GameObject>();
        Bounds bounds = GetObjectBounds();
        
        // 生成Voronoi种子点
        List<Vector3> seedPoints = new List<Vector3>();
        for (int i = 0; i < m_FragmentCount; i++)
        {
            Vector3 randomPoint = bounds.center + new Vector3(
                Random.Range(-bounds.size.x * 0.4f, bounds.size.x * 0.4f),
                Random.Range(-bounds.size.y * 0.4f, bounds.size.y * 0.4f),
                Random.Range(-bounds.size.z * 0.4f, bounds.size.z * 0.4f)
            );
            seedPoints.Add(randomPoint);
        }

        // 为每个种子点创建碎片
        for (int i = 0; i < seedPoints.Count; i++)
        {
            GameObject fragment = CreateVoronoiFragment(seedPoints[i], i, activate);
            debris.Add(fragment);
        }

        return debris;
    }

    private List<GameObject> GenerateRadialDebris(bool activate)
    {
        List<GameObject> debris = new List<GameObject>();
        Bounds bounds = GetObjectBounds();
        Vector3 center = bounds.center;
        
        // 径向分割
        float angleStep = 360f / m_FragmentCount;
        
        for (int i = 0; i < m_FragmentCount; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 position = center + direction * bounds.size.magnitude * 0.2f;
            
            GameObject fragment = CreateRadialFragment(position, direction, activate);
            debris.Add(fragment);
        }

        return debris;
    }

    private GameObject CreateDebrisFromMesh(Mesh mesh, bool activate)
    {
        GameObject debris = new GameObject($"Debris_{mesh.GetInstanceID()}");
        debris.transform.position = transform.position;
        debris.transform.rotation = transform.rotation;
        
        // 添加网格组件
        MeshFilter meshFilter = debris.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        
        MeshRenderer renderer = debris.AddComponent<MeshRenderer>();
        renderer.materials = GetDebrisMaterials();
        
        // 添加物理组件
        SetupDebrisPhysics(debris);
        
        // 添加碎片脚本
        SetupDebrisScript(debris);
        
        debris.SetActive(activate);
        return debris;
    }

    private GameObject CreateVoronoiFragment(Vector3 seedPoint, int index, bool activate)
    {
        GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fragment.name = $"VoronoiFragment_{index}";
        fragment.transform.position = seedPoint;
        fragment.transform.rotation = Random.rotation;
        
        // 随机变形
        Vector3 scale = new Vector3(
            Random.Range(m_MinFragmentSize, m_MaxFragmentSize),
            Random.Range(m_MinFragmentSize, m_MaxFragmentSize),
            Random.Range(m_MinFragmentSize, m_MaxFragmentSize)
        );
        fragment.transform.localScale = scale;
        
        // 设置材质
        fragment.GetComponent<Renderer>().materials = GetDebrisMaterials();
        
        SetupDebrisPhysics(fragment);
        SetupDebrisScript(fragment);
        
        fragment.SetActive(activate);
        return fragment;
    }

    private GameObject CreateRadialFragment(Vector3 position, Vector3 direction, bool activate)
    {
        PrimitiveType[] types = { PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Capsule };
        PrimitiveType selectedType = types[Random.Range(0, types.Length)];
        
        GameObject fragment = GameObject.CreatePrimitive(selectedType);
        fragment.name = $"RadialFragment_{position.GetHashCode()}";
        fragment.transform.position = position;
        fragment.transform.rotation = Quaternion.LookRotation(direction);
        
        float size = Random.Range(m_MinFragmentSize, m_MaxFragmentSize);
        fragment.transform.localScale = Vector3.one * size;
        
        fragment.GetComponent<Renderer>().materials = GetDebrisMaterials();
        
        SetupDebrisPhysics(fragment);
        SetupDebrisScript(fragment);
        
        fragment.SetActive(activate);
        return fragment;
    }

    private void SetupDebrisPhysics(GameObject debris)
    {
        Rigidbody rb = debris.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = debris.AddComponent<Rigidbody>();
        }
        
        rb.mass = m_DebrisMass;
        rb.drag = 0.5f;
        rb.angularDrag = 0.5f;
        
        // 确保有碰撞体
        Collider collider = debris.GetComponent<Collider>();
        if (collider != null && collider is MeshCollider)
        {
            ((MeshCollider)collider).convex = true;
        }
    }

    private void SetupDebrisScript(GameObject debris)
    {
        BuildingDebris debrisScript = debris.AddComponent<BuildingDebris>();
        debrisScript.m_LifeTime = m_DebrisLifetime;
        debrisScript.m_FadeStartTime = m_DebrisLifetime * 0.7f;
        
        // 添加拖尾效果
        if (m_AddTrailEffect)
        {
            TrailRenderer trail = debris.AddComponent<TrailRenderer>();
            trail.time = 0.5f;
            trail.startWidth = 0.1f;
            trail.endWidth = 0.01f;
            trail.material = GetDebrisMaterial();
        }
    }

    private void ActivatePreGeneratedDebris()
    {
        if (m_PreGeneratedDebris == null) return;
        
        foreach (GameObject debris in m_PreGeneratedDebris)
        {
            if (debris != null)
            {
                debris.SetActive(true);
            }
        }
    }

    private void ApplyExplosionForce(List<GameObject> debris, Vector3 explosionCenter)
    {
        foreach (GameObject debrisObj in debris)
        {
            if (debrisObj == null) continue;
            
            Rigidbody rb = debrisObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(m_ExplosionForce, explosionCenter, m_ExplosionRadius, m_UpwardModifier);
                
                // 添加随机旋转
                rb.AddTorque(Random.insideUnitSphere * m_ExplosionForce * 0.1f);
            }
        }
    }

    private void AddVisualEffects(Vector3 position)
    {
        if (m_AddSmokeEffect && m_SmokeEffectPrefab != null)
        {
            GameObject smoke = Instantiate(m_SmokeEffectPrefab, position, Quaternion.identity);
            Destroy(smoke, 5f);
        }
    }

    private Material GetDebrisMaterial()
    {
        if (!m_UseOriginalMaterial && m_DebrisMaterial != null)
        {
            return m_DebrisMaterial;
        }
        
        if (m_OriginalMaterials != null && m_OriginalMaterials.Length > 0)
        {
            return m_OriginalMaterials[0];
        }
        
        return null;
    }

    private Material[] GetDebrisMaterials()
    {
        if (!m_UseOriginalMaterial && m_DebrisMaterial != null)
        {
            return new Material[] { m_DebrisMaterial };
        }
        
        return m_OriginalMaterials ?? new Material[0];
    }

    private Bounds GetObjectBounds()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            return renderer.bounds;
            
        Collider collider = GetComponent<Collider>();
        if (collider != null)
            return collider.bounds;
            
        return new Bounds(transform.position, Vector3.one);
    }

    // 在Scene视图中显示调试信息
    private void OnDrawGizmosSelected()
    {
        Bounds bounds = GetObjectBounds();
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, m_ExplosionRadius);
        
        if (m_PreGeneratedDebris != null)
        {
            Gizmos.color = Color.green;
            foreach (GameObject debris in m_PreGeneratedDebris)
            {
                if (debris != null)
                {
                    Gizmos.DrawWireSphere(debris.transform.position, 0.1f);
                }
            }
        }
    }
}