using UnityEngine;
using System.Collections.Generic;

public class BuildingCrackEffect : MonoBehaviour
{
    [Header("裂缝效果设置")]
    public CrackEffectMethod m_EffectMethod = CrackEffectMethod.DecalProjector;
    public bool m_EnableCrackEffect = true;
    
    [Header("裂缝贴图")]
    public Texture2D[] m_CrackTextures;           // 裂缝贴图数组
    public Material m_CrackMaterial;              // 裂缝材质
    public Color m_CrackColor = Color.black;      // 裂缝颜色
    
    [Header("裂缝生成设置")]
    public int m_MaxCracks = 5;                   // 最大裂缝数量
    public float m_CrackSize = 1f;                // 裂缝大小
    public float m_CrackDepth = 0.01f;            // 裂缝深度
    public bool m_RandomRotation = true;          // 随机旋转裂缝
    
    [Header("渐进式裂缝")]
    public bool m_ProgressiveCracks = true;       // 渐进式裂缝（根据伤害程度显示）
    public float[] m_CrackThresholds = { 0.8f, 0.6f, 0.4f, 0.2f }; // 裂缝出现的生命值阈值
    
    [Header("材质混合")]
    public bool m_UseTextureBlending = true;      // 使用贴图混合
    public float m_BlendStrength = 0.5f;          // 混合强度
    
    private List<GameObject> m_ActiveCracks;      // 当前活跃的裂缝
    private BuildingHealth m_BuildingHealth;      // 建筑物生命值组件
    private Renderer m_BuildingRenderer;          // 建筑物渲染器
    private Material m_OriginalMaterial;          // 原始材质
    private Material m_DamagedMaterial;           // 受损材质
    private int m_CurrentCrackLevel = 0;          // 当前裂缝等级

    public enum CrackEffectMethod
    {
        DecalProjector,    // 使用贴花投影器
        TextureBlending,   // 贴图混合
        GeometryOverlay,   // 几何体覆盖
        ParticleEffect     // 粒子效果
    }

    private void Awake()
    {
        m_ActiveCracks = new List<GameObject>();
        m_BuildingHealth = GetComponent<BuildingHealth>();
        m_BuildingRenderer = GetComponent<Renderer>();
        
        if (m_BuildingRenderer != null)
        {
            m_OriginalMaterial = m_BuildingRenderer.material;
            CreateDamagedMaterial();
        }
    }

    private void Start()
    {
        // 订阅建筑物受损事件
        if (m_BuildingHealth != null)
        {
            // 我们需要修改BuildingHealth来支持事件
        }
    }

    public void ShowCrackEffect(float healthPercentage, Vector3 impactPoint)
    {
        if (!m_EnableCrackEffect) return;

        // 根据生命值百分比决定是否显示新裂缝
        int targetCrackLevel = CalculateCrackLevel(healthPercentage);
        
        if (targetCrackLevel > m_CurrentCrackLevel)
        {
            // 添加新裂缝
            for (int i = m_CurrentCrackLevel; i < targetCrackLevel; i++)
            {
                CreateCrack(impactPoint, i);
            }
            m_CurrentCrackLevel = targetCrackLevel;
        }

        // 更新材质效果
        UpdateMaterialEffect(healthPercentage);
    }

    private int CalculateCrackLevel(float healthPercentage)
    {
        if (!m_ProgressiveCracks) return 1;

        for (int i = 0; i < m_CrackThresholds.Length; i++)
        {
            if (healthPercentage <= m_CrackThresholds[i])
            {
                return i + 1;
            }
        }
        return 0;
    }

    private void CreateCrack(Vector3 impactPoint, int crackIndex)
    {
        switch (m_EffectMethod)
        {
            case CrackEffectMethod.DecalProjector:
                CreateDecalCrack(impactPoint, crackIndex);
                break;
            case CrackEffectMethod.TextureBlending:
                UpdateTextureBlending(crackIndex);
                break;
            case CrackEffectMethod.GeometryOverlay:
                CreateGeometryCrack(impactPoint, crackIndex);
                break;
            case CrackEffectMethod.ParticleEffect:
                CreateParticleCrack(impactPoint, crackIndex);
                break;
        }
    }

    private void CreateDecalCrack(Vector3 impactPoint, int crackIndex)
    {
        if (m_CrackTextures == null || m_CrackTextures.Length == 0) return;

        GameObject crackDecal = new GameObject($"Crack_{crackIndex}");
        crackDecal.transform.SetParent(transform);

        // 计算裂缝位置（在建筑物表面）
        Vector3 crackPosition = CalculateSurfacePosition(impactPoint);
        crackDecal.transform.position = crackPosition;

        // 随机旋转
        if (m_RandomRotation)
        {
            crackDecal.transform.rotation = Quaternion.Euler(
                Random.Range(0, 360),
                Random.Range(0, 360),
                Random.Range(0, 360)
            );
        }

        // 创建贴花
        CreateDecalQuad(crackDecal, crackIndex);
        
        m_ActiveCracks.Add(crackDecal);
    }

    private void CreateDecalQuad(GameObject parent, int crackIndex)
    {
        // 创建四边形网格
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.SetParent(parent.transform);
        quad.transform.localPosition = Vector3.zero;
        quad.transform.localScale = Vector3.one * m_CrackSize;

        // 移除碰撞体
        Collider quadCollider = quad.GetComponent<Collider>();
        if (quadCollider != null)
        {
            DestroyImmediate(quadCollider);
        }

        // 设置材质
        Renderer quadRenderer = quad.GetComponent<Renderer>();
        if (quadRenderer != null)
        {
            Material crackMat = CreateCrackMaterial(crackIndex);
            quadRenderer.material = crackMat;
            quadRenderer.sortingOrder = 1; // 确保在建筑物表面之上
        }

        // 稍微向外偏移避免Z-fighting
        quad.transform.localPosition = Vector3.forward * m_CrackDepth;
    }

    private Material CreateCrackMaterial(int crackIndex)
    {
        Material crackMat;
        
        if (m_CrackMaterial != null)
        {
            crackMat = new Material(m_CrackMaterial);
        }
        else
        {
            // 创建默认裂缝材质
            crackMat = new Material(Shader.Find("Standard"));
            crackMat.SetFloat("_Mode", 3); // 设置为透明模式
            crackMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            crackMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            crackMat.SetInt("_ZWrite", 0);
            crackMat.DisableKeyword("_ALPHATEST_ON");
            crackMat.EnableKeyword("_ALPHABLEND_ON");
            crackMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            crackMat.renderQueue = 3000;
        }

        // 设置裂缝贴图
        if (m_CrackTextures != null && m_CrackTextures.Length > 0)
        {
            Texture2D crackTexture = m_CrackTextures[crackIndex % m_CrackTextures.Length];
            crackMat.mainTexture = crackTexture;
        }

        crackMat.color = m_CrackColor;
        
        return crackMat;
    }

    private void UpdateTextureBlending(int crackIndex)
    {
        if (m_DamagedMaterial == null) return;

        // 更新材质的裂缝强度
        float crackStrength = (float)(crackIndex + 1) / m_MaxCracks;
        m_DamagedMaterial.SetFloat("_CrackStrength", crackStrength * m_BlendStrength);
        
        if (m_BuildingRenderer != null)
        {
            m_BuildingRenderer.material = m_DamagedMaterial;
        }
    }

    private void CreateGeometryCrack(Vector3 impactPoint, int crackIndex)
    {
        // 创建简单的几何裂缝（线条）
        GameObject crackLine = new GameObject($"CrackLine_{crackIndex}");
        crackLine.transform.SetParent(transform);

        LineRenderer lineRenderer = crackLine.AddComponent<LineRenderer>();
        lineRenderer.material = CreateCrackMaterial(crackIndex);
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.02f;
        lineRenderer.positionCount = Random.Range(3, 6);

        // 生成随机裂缝路径
        Vector3[] crackPath = GenerateRandomCrackPath(impactPoint, lineRenderer.positionCount);
        lineRenderer.SetPositions(crackPath);

        m_ActiveCracks.Add(crackLine);
    }

    private Vector3[] GenerateRandomCrackPath(Vector3 startPoint, int pointCount)
    {
        Vector3[] path = new Vector3[pointCount];
        path[0] = startPoint;

        Vector3 direction = Random.onUnitSphere;
        direction.y = Mathf.Abs(direction.y) * 0.3f; // 限制Y方向

        for (int i = 1; i < pointCount; i++)
        {
            // 添加一些随机性
            direction += Random.insideUnitSphere * 0.3f;
            direction = direction.normalized;

            float segmentLength = Random.Range(0.5f, 1.5f) * m_CrackSize;
            path[i] = path[i - 1] + direction * segmentLength;
        }

        return path;
    }

    private void CreateParticleCrack(Vector3 impactPoint, int crackIndex)
    {
        GameObject crackParticles = new GameObject($"CrackParticles_{crackIndex}");
        crackParticles.transform.SetParent(transform);
        crackParticles.transform.position = impactPoint;

        ParticleSystem particles = crackParticles.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = 2f;
        main.startSpeed = 0.1f;
        main.startSize = 0.1f;
        main.startColor = m_CrackColor;
        main.maxParticles = 20;

        var emission = particles.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, 20)
        });

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = m_CrackSize * 0.5f;

        m_ActiveCracks.Add(crackParticles);
    }

    private Vector3 CalculateSurfacePosition(Vector3 impactPoint)
    {
        // 简单实现：使用建筑物的边界
        Bounds bounds = GetComponent<Renderer>().bounds;
        
        // 找到最近的表面点
        Vector3 closestPoint = bounds.ClosestPoint(impactPoint);
        
        // 稍微向外偏移
        Vector3 direction = (closestPoint - bounds.center).normalized;
        return closestPoint + direction * 0.01f;
    }

    private void CreateDamagedMaterial()
    {
        if (m_OriginalMaterial == null) return;

        m_DamagedMaterial = new Material(m_OriginalMaterial);
        m_DamagedMaterial.name = m_OriginalMaterial.name + "_Damaged";

        // 如果有裂缝贴图，可以在这里设置
        if (m_UseTextureBlending && m_CrackTextures != null && m_CrackTextures.Length > 0)
        {
            // 这里需要一个支持贴图混合的Shader
            // 暂时使用颜色变化来表示受损
        }
    }

    private void UpdateMaterialEffect(float healthPercentage)
    {
        if (m_BuildingRenderer == null || m_DamagedMaterial == null) return;

        // 根据生命值调整材质颜色（变暗表示受损）
        float damageRatio = 1f - healthPercentage;
        Color damagedColor = Color.Lerp(Color.white, Color.gray, damageRatio * 0.7f);
        m_DamagedMaterial.color = damagedColor;

        m_BuildingRenderer.material = m_DamagedMaterial;
    }

    public void ClearAllCracks()
    {
        foreach (GameObject crack in m_ActiveCracks)
        {
            if (crack != null)
            {
                DestroyImmediate(crack);
            }
        }
        m_ActiveCracks.Clear();
        m_CurrentCrackLevel = 0;

        // 恢复原始材质
        if (m_BuildingRenderer != null && m_OriginalMaterial != null)
        {
            m_BuildingRenderer.material = m_OriginalMaterial;
        }
    }

    public void ResetCracks()
    {
        ClearAllCracks();
    }

    private void OnDestroy()
    {
        // 清理材质实例
        if (m_DamagedMaterial != null)
        {
            DestroyImmediate(m_DamagedMaterial);
        }
    }

    // 在Scene视图中显示调试信息
    private void OnDrawGizmosSelected()
    {
        if (m_ActiveCracks != null)
        {
            Gizmos.color = Color.red;
            foreach (GameObject crack in m_ActiveCracks)
            {
                if (crack != null)
                {
                    Gizmos.DrawWireSphere(crack.transform.position, 0.1f);
                }
            }
        }
    }
}