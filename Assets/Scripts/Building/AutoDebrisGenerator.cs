using UnityEngine;
using System.Collections.Generic;

public class AutoDebrisGenerator : MonoBehaviour
{
    [Header("Auto Debris Generation")]
    public bool m_GenerateOnStart = true;           // 是否在开始时自动生成碎片
    public int m_DebrisCount = 8;                   // 碎片数量
    public Vector3 m_DebrisSize = Vector3.one * 0.5f; // 碎片大小
    public Material m_DebrisMaterial;               // 碎片材质
    
    [Header("Fracture Settings")]
    public bool m_UseVoronoiFracture = true;        // 使用Voronoi分割
    public int m_VoronoiSites = 6;                  // Voronoi点数量
    public float m_MinDebrisSize = 0.2f;            // 最小碎片大小
    public float m_MaxDebrisSize = 1.0f;            // 最大碎片大小
    
    [Header("Physics Settings")]
    public float m_DebrisMass = 1f;                 // 碎片质量
    public float m_ExplosionForce = 500f;           // 爆炸力
    public float m_DebrisLifetime = 10f;            // 碎片生命周期
    
    private List<GameObject> m_GeneratedDebris;
    private Mesh m_OriginalMesh;
    private Material[] m_OriginalMaterials;
    private Bounds m_OriginalBounds;

    private void Start()
    {
        if (m_GenerateOnStart)
        {
            GenerateDebris();
        }
    }

    public List<GameObject> GenerateDebris()
    {
        m_GeneratedDebris = new List<GameObject>();
        
        // 获取原始网格信息
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        
        if (meshFilter == null || meshFilter.mesh == null)
        {
            Debug.LogWarning("AutoDebrisGenerator: 未找到有效的网格");
            return m_GeneratedDebris;
        }

        m_OriginalMesh = meshFilter.mesh;
        m_OriginalMaterials = meshRenderer != null ? meshRenderer.materials : null;
        m_OriginalBounds = m_OriginalMesh.bounds;

        if (m_UseVoronoiFracture)
        {
            GenerateVoronoiDebris();
        }
        else
        {
            GenerateSimpleDebris();
        }

        return m_GeneratedDebris;
    }

    private void GenerateVoronoiDebris()
    {
        // 生成Voronoi点
        List<Vector3> voronoiSites = GenerateVoronoiSites();
        
        // 为每个Voronoi区域创建碎片
        for (int i = 0; i < voronoiSites.Count; i++)
        {
            GameObject debris = CreateDebrisFromVoronoiSite(voronoiSites[i], i);
            if (debris != null)
            {
                m_GeneratedDebris.Add(debris);
            }
        }
    }

    private List<Vector3> GenerateVoronoiSites()
    {
        List<Vector3> sites = new List<Vector3>();
        
        for (int i = 0; i < m_VoronoiSites; i++)
        {
            Vector3 randomPoint = new Vector3(
                Random.Range(m_OriginalBounds.min.x, m_OriginalBounds.max.x),
                Random.Range(m_OriginalBounds.min.y, m_OriginalBounds.max.y),
                Random.Range(m_OriginalBounds.min.z, m_OriginalBounds.max.z)
            );
            
            sites.Add(transform.TransformPoint(randomPoint));
        }
        
        return sites;
    }

    private GameObject CreateDebrisFromVoronoiSite(Vector3 site, int index)
    {
        // 创建基础碎片对象
        GameObject debris = CreateBasicDebris($"Debris_{index}", site);
        
        // 创建随机形状的网格
        Mesh debrisMesh = CreateRandomDebrisMesh();
        debris.GetComponent<MeshFilter>().mesh = debrisMesh;
        
        return debris;
    }

    private void GenerateSimpleDebris()
    {
        for (int i = 0; i < m_DebrisCount; i++)
        {
            // 在原始对象范围内随机生成位置
            Vector3 randomPosition = GetRandomPositionInBounds();
            GameObject debris = CreateBasicDebris($"SimpleDebris_{i}", randomPosition);
            
            // 使用简单的几何形状
            CreateSimpleDebrisMesh(debris);
            
            m_GeneratedDebris.Add(debris);
        }
    }

    private GameObject CreateBasicDebris(string name, Vector3 position)
    {
        GameObject debris = new GameObject(name);
        debris.transform.position = position;
        debris.transform.rotation = Random.rotation;
        
        // 添加基本组件
        debris.AddComponent<MeshFilter>();
        MeshRenderer renderer = debris.AddComponent<MeshRenderer>();
        Rigidbody rb = debris.AddComponent<Rigidbody>();
        debris.AddComponent<MeshCollider>().convex = true;
        
        // 设置材质
        if (m_DebrisMaterial != null)
        {
            renderer.material = m_DebrisMaterial;
        }
        else if (m_OriginalMaterials != null && m_OriginalMaterials.Length > 0)
        {
            renderer.material = m_OriginalMaterials[Random.Range(0, m_OriginalMaterials.Length)];
        }
        
        // 设置物理属性
        rb.mass = m_DebrisMass;
        
        // 添加碎片脚本
        BuildingDebris debrisScript = debris.AddComponent<BuildingDebris>();
        debrisScript.m_LifeTime = m_DebrisLifetime;
        
        return debris;
    }

    private Mesh CreateRandomDebrisMesh()
    {
        Mesh mesh = new Mesh();
        
        // 创建随机的顶点
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        
        // 生成随机的凸多面体
        int vertexCount = Random.Range(4, 8);
        for (int i = 0; i < vertexCount; i++)
        {
            Vector3 vertex = Random.insideUnitSphere * Random.Range(m_MinDebrisSize, m_MaxDebrisSize);
            vertices.Add(vertex);
        }
        
        // 创建简单的三角形（这里使用简化的方法）
        if (vertices.Count >= 4)
        {
            // 创建四面体
            triangles.AddRange(new int[] { 0, 1, 2 });
            triangles.AddRange(new int[] { 0, 2, 3 });
            triangles.AddRange(new int[] { 0, 3, 1 });
            triangles.AddRange(new int[] { 1, 3, 2 });
        }
        
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }

    private void CreateSimpleDebrisMesh(GameObject debris)
    {
        // 使用Unity的基本几何体
        PrimitiveType[] primitiveTypes = { PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Capsule };
        PrimitiveType selectedType = primitiveTypes[Random.Range(0, primitiveTypes.Length)];
        
        // 创建临时对象获取网格
        GameObject temp = GameObject.CreatePrimitive(selectedType);
        Mesh primitiveMesh = temp.GetComponent<MeshFilter>().mesh;
        
        // 复制网格并随机缩放
        Mesh debrisMesh = Instantiate(primitiveMesh);
        Vector3 randomScale = new Vector3(
            Random.Range(m_MinDebrisSize, m_MaxDebrisSize),
            Random.Range(m_MinDebrisSize, m_MaxDebrisSize),
            Random.Range(m_MinDebrisSize, m_MaxDebrisSize)
        );
        
        // 应用缩放到顶点
        Vector3[] vertices = debrisMesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = Vector3.Scale(vertices[i], randomScale);
        }
        debrisMesh.vertices = vertices;
        debrisMesh.RecalculateBounds();
        debrisMesh.RecalculateNormals();
        
        debris.GetComponent<MeshFilter>().mesh = debrisMesh;
        debris.GetComponent<MeshCollider>().sharedMesh = debrisMesh;
        
        DestroyImmediate(temp);
    }

    private Vector3 GetRandomPositionInBounds()
    {
        Vector3 localPosition = new Vector3(
            Random.Range(m_OriginalBounds.min.x, m_OriginalBounds.max.x),
            Random.Range(m_OriginalBounds.min.y, m_OriginalBounds.max.y),
            Random.Range(m_OriginalBounds.min.z, m_OriginalBounds.max.z)
        );
        
        return transform.TransformPoint(localPosition);
    }

    public void ExplodeDebris(Vector3 explosionCenter, float force = 0f)
    {
        if (m_GeneratedDebris == null) return;
        
        float actualForce = force > 0f ? force : m_ExplosionForce;
        
        foreach (GameObject debris in m_GeneratedDebris)
        {
            if (debris != null)
            {
                Rigidbody rb = debris.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 direction = (debris.transform.position - explosionCenter).normalized;
                    rb.AddForce(direction * actualForce + Vector3.up * actualForce * 0.3f);
                    rb.AddTorque(Random.insideUnitSphere * actualForce * 0.1f);
                }
            }
        }
    }

    // 在Scene视图中显示调试信息
    private void OnDrawGizmosSelected()
    {
        if (m_OriginalMesh != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + m_OriginalBounds.center, m_OriginalBounds.size);
        }
        
        if (m_GeneratedDebris != null)
        {
            Gizmos.color = Color.red;
            foreach (GameObject debris in m_GeneratedDebris)
            {
                if (debris != null)
                {
                    Gizmos.DrawWireSphere(debris.transform.position, 0.1f);
                }
            }
        }
    }
}