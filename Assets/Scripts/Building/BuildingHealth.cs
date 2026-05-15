using UnityEngine;
using System.Collections.Generic;

public class BuildingHealth : MonoBehaviour
{
    [Header("Building Health Settings")]
    public float m_StartingHealth = 50f;           // 建筑物初始生命值
    public float m_CurrentHealth;                  // 当前生命值
    
    [Header("Destruction Effects")]
    public GameObject m_DestructionPrefab;         // 破坏效果预制体
    public GameObject[] m_DebrisPrefabs;           // 碎片预制体数组（手动设置）
    public AudioClip m_DestructionSound;           // 破坏音效
    
    [Header("Destruction Settings")]
    public bool m_UseAutoDebrisGeneration = true;  // 使用自动碎片生成
    public float m_DebrisForce = 500f;             // 碎片飞散力度
    public float m_DebrisLifetime = 5f;            // 碎片存在时间
    public int m_DebrisCount = 5;                  // 碎片数量（手动模式）
    
    private bool m_IsDestroyed = false;
    private AudioSource m_AudioSource;
    private Renderer m_Renderer;
    private Collider m_Collider;
    private AutoDebrisGenerator m_DebrisGenerator;
    private BuildingCrackEffect m_CrackEffect;
    private BuildingDamageEffects m_DamageEffects;

    private void Awake()
    {
        m_AudioSource = GetComponent<AudioSource>();
        if (m_AudioSource == null)
        {
            m_AudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        m_Renderer = GetComponent<Renderer>();
        m_Collider = GetComponent<Collider>();
        
        // 获取或添加裂缝效果组件
        m_CrackEffect = GetComponent<BuildingCrackEffect>();
        if (m_CrackEffect == null)
        {
            m_CrackEffect = gameObject.AddComponent<BuildingCrackEffect>();
        }
        
        // 获取或添加受损效果组件
        m_DamageEffects = GetComponent<BuildingDamageEffects>();
        if (m_DamageEffects == null)
        {
            m_DamageEffects = gameObject.AddComponent<BuildingDamageEffects>();
        }
        
        // 获取或添加自动碎片生成器
        if (m_UseAutoDebrisGeneration)
        {
            m_DebrisGenerator = GetComponent<AutoDebrisGenerator>();
            if (m_DebrisGenerator == null)
            {
                m_DebrisGenerator = gameObject.AddComponent<AutoDebrisGenerator>();
                // 配置自动碎片生成器
                m_DebrisGenerator.m_GenerateOnStart = false; // 不在开始时生成，而是在摧毁时生成
                m_DebrisGenerator.m_DebrisCount = m_DebrisCount;
                m_DebrisGenerator.m_ExplosionForce = m_DebrisForce;
                m_DebrisGenerator.m_DebrisLifetime = m_DebrisLifetime;
            }
        }
    }

    private void Start()
    {
        m_CurrentHealth = m_StartingHealth;
    }

    public void TakeDamage(float damage)
    {
        TakeDamageAtPoint(damage, transform.position);
    }

    public void TakeDamageAtPoint(float damage, Vector3 impactPoint)
    {
        if (m_IsDestroyed) return;

        float previousHealth = m_CurrentHealth;
        m_CurrentHealth -= damage;
        
        // 播放受损效果
        if (m_DamageEffects != null)
        {
            m_DamageEffects.PlayDamageEffects(impactPoint, damage);
        }
        
        // 显示裂缝效果
        if (m_CrackEffect != null)
        {
            float healthPercentage = GetHealthPercentage();
            m_CrackEffect.ShowCrackEffect(healthPercentage, impactPoint);
        }
        
        // 显示受损效果
        ShowDamageEffect();

        if (m_CurrentHealth <= 0f)
        {
            DestroyBuilding();
        }
    }

    private void ShowDamageEffect()
    {
        // 裂缝效果已经在TakeDamageAtPoint中处理
        // 这里可以添加其他受损效果，比如烟雾、火花等
        
        if (m_Renderer != null)
        {
            // 计算损伤程度
            float damageRatio = 1f - (m_CurrentHealth / m_StartingHealth);
            
            // 根据损伤程度调整材质颜色（变暗）- 这个效果现在由CrackEffect处理
            // Color originalColor = m_Renderer.material.color;
            // Color damagedColor = Color.Lerp(originalColor, Color.gray, damageRatio * 0.5f);
            // m_Renderer.material.color = damagedColor;
        }
    }

    private void DestroyBuilding()
    {
        if (m_IsDestroyed) return;
        m_IsDestroyed = true;

        // 播放破坏效果
        if (m_DamageEffects != null)
        {
            m_DamageEffects.PlayDestructionEffects(transform.position);
        }

        // 播放破坏音效
        if (m_DestructionSound != null && m_AudioSource != null)
        {
            m_AudioSource.PlayOneShot(m_DestructionSound);
        }

        // 生成破坏效果
        if (m_DestructionPrefab != null)
        {
            GameObject explosion = Instantiate(m_DestructionPrefab, transform.position, transform.rotation);
            Destroy(explosion, 3f); // 3秒后销毁效果
        }

        // 生成碎片
        CreateDebris();

        // 禁用碰撞体和渲染器
        if (m_Collider != null) m_Collider.enabled = false;
        if (m_Renderer != null) m_Renderer.enabled = false;

        // 延迟销毁建筑物本体（等音效播放完）
        Destroy(gameObject, 2f);
    }

    private void CreateDebris()
    {
        if (m_UseAutoDebrisGeneration && m_DebrisGenerator != null)
        {
            // 使用自动碎片生成器
            List<GameObject> generatedDebris = m_DebrisGenerator.GenerateDebris();
            m_DebrisGenerator.ExplodeDebris(transform.position, m_DebrisForce);
        }
        else
        {
            // 使用手动设置的碎片预制体
            CreateManualDebris();
        }
    }

    private void CreateManualDebris()
    {
        if (m_DebrisPrefabs == null || m_DebrisPrefabs.Length == 0) return;

        for (int i = 0; i < m_DebrisCount; i++)
        {
            // 随机选择碎片预制体
            GameObject debrisPrefab = m_DebrisPrefabs[Random.Range(0, m_DebrisPrefabs.Length)];
            
            // 在建筑物周围随机位置生成碎片
            Vector3 spawnPosition = transform.position + Random.insideUnitSphere * 2f;
            spawnPosition.y = transform.position.y + Random.Range(0f, 2f);
            
            GameObject debris = Instantiate(debrisPrefab, spawnPosition, Random.rotation);
            
            // 添加物理效果
            Rigidbody debrisRb = debris.GetComponent<Rigidbody>();
            if (debrisRb == null)
            {
                debrisRb = debris.AddComponent<Rigidbody>();
            }
            
            // 添加随机力让碎片飞散
            Vector3 forceDirection = (spawnPosition - transform.position).normalized + Vector3.up * 0.5f;
            debrisRb.AddForce(forceDirection * m_DebrisForce);
            debrisRb.AddTorque(Random.insideUnitSphere * m_DebrisForce * 0.1f);
            
            // 设置碎片生命周期
            Destroy(debris, m_DebrisLifetime);
        }
    }

    // 获取当前生命值百分比（用于UI显示等）
    public float GetHealthPercentage()
    {
        return m_CurrentHealth / m_StartingHealth;
    }

    // 检查建筑是否已被摧毁
    public bool IsDestroyed()
    {
        return m_IsDestroyed;
    }
}