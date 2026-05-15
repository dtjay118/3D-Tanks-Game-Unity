using UnityEngine;
using System.Collections.Generic;

public static class SimpleFractureUtility
{
    /// <summary>
    /// 简单的网格分割工具，将一个网格分割成多个碎片
    /// </summary>
    public static List<Mesh> FractureMesh(Mesh originalMesh, int fragmentCount, float minSize = 0.1f)
    {
        List<Mesh> fragments = new List<Mesh>();
        
        if (originalMesh == null || fragmentCount <= 0)
            return fragments;

        Vector3[] vertices = originalMesh.vertices;
        int[] triangles = originalMesh.triangles;
        Vector3[] normals = originalMesh.normals;
        Vector2[] uvs = originalMesh.uv;

        // 计算网格边界
        Bounds bounds = originalMesh.bounds;
        
        // 生成随机分割平面
        List<Plane> cuttingPlanes = GenerateRandomCuttingPlanes(fragmentCount, bounds);
        
        // 为每个区域创建碎片
        for (int i = 0; i < fragmentCount; i++)
        {
            Mesh fragment = CreateFragmentMesh(vertices, triangles, normals, uvs, cuttingPlanes, i, bounds, minSize);
            if (fragment != null && fragment.vertexCount > 0)
            {
                fragments.Add(fragment);
            }
        }

        return fragments;
    }

    private static List<Plane> GenerateRandomCuttingPlanes(int count, Bounds bounds)
    {
        List<Plane> planes = new List<Plane>();
        
        for (int i = 0; i < count; i++)
        {
            // 生成随机的分割平面
            Vector3 randomPoint = bounds.center + new Vector3(
                Random.Range(-bounds.size.x * 0.5f, bounds.size.x * 0.5f),
                Random.Range(-bounds.size.y * 0.5f, bounds.size.y * 0.5f),
                Random.Range(-bounds.size.z * 0.5f, bounds.size.z * 0.5f)
            );
            
            Vector3 randomNormal = Random.onUnitSphere;
            planes.Add(new Plane(randomNormal, randomPoint));
        }
        
        return planes;
    }

    private static Mesh CreateFragmentMesh(Vector3[] vertices, int[] triangles, Vector3[] normals, Vector2[] uvs, 
                                         List<Plane> cuttingPlanes, int fragmentIndex, Bounds bounds, float minSize)
    {
        List<Vector3> fragmentVertices = new List<Vector3>();
        List<int> fragmentTriangles = new List<int>();
        List<Vector3> fragmentNormals = new List<Vector3>();
        List<Vector2> fragmentUVs = new List<Vector2>();

        // 选择属于这个碎片的三角形
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];
            
            Vector3 triangleCenter = (v1 + v2 + v3) / 3f;
            
            // 检查三角形是否属于当前碎片
            if (IsPointInFragment(triangleCenter, cuttingPlanes, fragmentIndex, bounds))
            {
                int baseIndex = fragmentVertices.Count;
                
                // 添加顶点
                fragmentVertices.Add(v1);
                fragmentVertices.Add(v2);
                fragmentVertices.Add(v3);
                
                // 添加三角形索引
                fragmentTriangles.Add(baseIndex);
                fragmentTriangles.Add(baseIndex + 1);
                fragmentTriangles.Add(baseIndex + 2);
                
                // 添加法线
                if (normals != null && normals.Length > triangles[i])
                {
                    fragmentNormals.Add(normals[triangles[i]]);
                    fragmentNormals.Add(normals[triangles[i + 1]]);
                    fragmentNormals.Add(normals[triangles[i + 2]]);
                }
                
                // 添加UV
                if (uvs != null && uvs.Length > triangles[i])
                {
                    fragmentUVs.Add(uvs[triangles[i]]);
                    fragmentUVs.Add(uvs[triangles[i + 1]]);
                    fragmentUVs.Add(uvs[triangles[i + 2]]);
                }
            }
        }

        // 检查碎片是否足够大
        if (fragmentVertices.Count < 3)
            return null;

        Bounds fragmentBounds = CalculateBounds(fragmentVertices);
        if (fragmentBounds.size.magnitude < minSize)
            return null;

        // 创建网格
        Mesh fragmentMesh = new Mesh();
        fragmentMesh.vertices = fragmentVertices.ToArray();
        fragmentMesh.triangles = fragmentTriangles.ToArray();
        
        if (fragmentNormals.Count == fragmentVertices.Count)
            fragmentMesh.normals = fragmentNormals.ToArray();
        else
            fragmentMesh.RecalculateNormals();
            
        if (fragmentUVs.Count == fragmentVertices.Count)
            fragmentMesh.uv = fragmentUVs.ToArray();

        fragmentMesh.RecalculateBounds();
        
        return fragmentMesh;
    }

    private static bool IsPointInFragment(Vector3 point, List<Plane> cuttingPlanes, int fragmentIndex, Bounds bounds)
    {
        // 简单的区域分配：基于点的位置和碎片索引
        Vector3 normalizedPoint = new Vector3(
            (point.x - bounds.min.x) / bounds.size.x,
            (point.y - bounds.min.y) / bounds.size.y,
            (point.z - bounds.min.z) / bounds.size.z
        );

        // 使用简单的网格分割
        int gridSize = Mathf.CeilToInt(Mathf.Pow(cuttingPlanes.Count, 1f/3f));
        int x = Mathf.FloorToInt(normalizedPoint.x * gridSize);
        int y = Mathf.FloorToInt(normalizedPoint.y * gridSize);
        int z = Mathf.FloorToInt(normalizedPoint.z * gridSize);
        
        x = Mathf.Clamp(x, 0, gridSize - 1);
        y = Mathf.Clamp(y, 0, gridSize - 1);
        z = Mathf.Clamp(z, 0, gridSize - 1);
        
        int cellIndex = x + y * gridSize + z * gridSize * gridSize;
        return cellIndex % cuttingPlanes.Count == fragmentIndex;
    }

    private static Bounds CalculateBounds(List<Vector3> vertices)
    {
        if (vertices.Count == 0)
            return new Bounds();

        Vector3 min = vertices[0];
        Vector3 max = vertices[0];

        foreach (Vector3 vertex in vertices)
        {
            min = Vector3.Min(min, vertex);
            max = Vector3.Max(max, vertex);
        }

        return new Bounds((min + max) * 0.5f, max - min);
    }

    /// <summary>
    /// 创建简单的几何碎片（立方体、球体等）
    /// </summary>
    public static List<GameObject> CreateSimpleDebris(Transform parent, int count, Material material, 
                                                    float minSize = 0.2f, float maxSize = 0.8f)
    {
        List<GameObject> debris = new List<GameObject>();
        Bounds parentBounds = GetObjectBounds(parent.gameObject);

        PrimitiveType[] primitiveTypes = { PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Capsule };

        for (int i = 0; i < count; i++)
        {
            // 随机选择几何体类型
            PrimitiveType type = primitiveTypes[Random.Range(0, primitiveTypes.Length)];
            GameObject debrisObj = GameObject.CreatePrimitive(type);
            
            // 设置位置和旋转
            Vector3 randomPos = parentBounds.center + new Vector3(
                Random.Range(-parentBounds.size.x * 0.5f, parentBounds.size.x * 0.5f),
                Random.Range(-parentBounds.size.y * 0.5f, parentBounds.size.y * 0.5f),
                Random.Range(-parentBounds.size.z * 0.5f, parentBounds.size.z * 0.5f)
            );
            
            debrisObj.transform.position = randomPos;
            debrisObj.transform.rotation = Random.rotation;
            
            // 随机缩放
            float scale = Random.Range(minSize, maxSize);
            debrisObj.transform.localScale = Vector3.one * scale;
            
            // 设置材质
            if (material != null)
            {
                debrisObj.GetComponent<Renderer>().material = material;
            }
            
            // 添加物理组件
            Rigidbody rb = debrisObj.AddComponent<Rigidbody>();
            rb.mass = scale * 0.5f;
            
            // 添加碎片脚本
            debrisObj.AddComponent<BuildingDebris>();
            
            debris.Add(debrisObj);
        }

        return debris;
    }

    private static Bounds GetObjectBounds(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
            return renderer.bounds;

        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
            return collider.bounds;

        return new Bounds(obj.transform.position, Vector3.one);
    }
}