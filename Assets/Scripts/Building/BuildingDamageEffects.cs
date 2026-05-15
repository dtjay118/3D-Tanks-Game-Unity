using UnityEngine;
using System.Collections;

public class BuildingDamageEffects : MonoBehaviour
{
    [Header("受损效果设置")]
    public bool m_EnableDamageEffects = true;
    public bool m_EnableScreenShake = true;
    public bool m_EnableSoundEffects = true;
    public bool m_EnableParticleEffects = true;
    
    [Header("屏幕震动")]
    public float m_ShakeIntensity = 0.1f;
    public float m_ShakeDuration = 0.2f;
    
    [Header("音效")]
    public AudioClip[] m_HitSounds;              // 受击音效
    public AudioClip[] m_CrackSounds;            // 裂缝音效
    public float m_SoundVolume = 0.7f;
    
    [Header("粒子效果")]
    public GameObject m_DustEffectPrefab;        // 灰尘效果
    public GameObject m_SparkEffectPrefab;       // 火花效果
    public GameObject m_SmokeEffectPrefab;       // 烟雾效果
    
    [Header("材质效果")]
    public bool m_EnableMaterialPulse = true;    // 材质脉冲效果
    public Color m_DamageFlashColor = Color.red; // 受损闪烁颜色
    public float m_FlashDuration = 0.1f;         // 闪烁持续时间
    
    private AudioSource m_AudioSource;
    private Renderer m_Renderer;
    private Material m_OriginalMaterial;
    private Camera m_MainCamera;
    private BuildingHealth m_BuildingHealth;
    
    private void Awake()
    {
        m_AudioSource = GetComponent<AudioSource>();
        if (m_AudioSource == null)
        {
            m_AudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        m_Renderer = GetComponent<Renderer>();
        if (m_Renderer != null)
        {
            m_OriginalMaterial = m_Renderer.material;
        }
        
        m_MainCamera = Camera.main;
        m_BuildingHealth = GetComponent<BuildingHealth>();
    }
    
    public void PlayDamageEffects(Vector3 impactPoint, float damage)
    {
        if (!m_EnableDamageEffects) return;
        
        // 播放受击音效
        if (m_EnableSoundEffects)
        {
            PlayHitSound();
        }
        
        // 播放粒子效果
        if (m_EnableParticleEffects)
        {
            PlayParticleEffects(impactPoint, damage);
        }
        
        // 材质闪烁效果
        if (m_EnableMaterialPulse)
        {
            StartCoroutine(MaterialFlashEffect());
        }
        
        // 屏幕震动
        if (m_EnableScreenShake && m_MainCamera != null)
        {
            StartCoroutine(ScreenShakeEffect());
        }
    }
    
    public void PlayCrackEffect(Vector3 crackPosition)
    {
        // 播放裂缝音效
        if (m_EnableSoundEffects && m_CrackSounds != null && m_CrackSounds.Length > 0)
        {
            AudioClip crackSound = m_CrackSounds[Random.Range(0, m_CrackSounds.Length)];
            m_AudioSource.PlayOneShot(crackSound, m_SoundVolume * 0.5f);
        }
        
        // 播放小型灰尘效果
        if (m_EnableParticleEffects && m_DustEffectPrefab != null)
        {
            GameObject dustEffect = Instantiate(m_DustEffectPrefab, crackPosition, Quaternion.identity);
            
            // 缩小效果
            dustEffect.transform.localScale *= 0.3f;
            
            Destroy(dustEffect, 2f);
        }
    }
    
    private void PlayHitSound()
    {
        if (m_HitSounds == null || m_HitSounds.Length == 0) return;
        
        AudioClip hitSound = m_HitSounds[Random.Range(0, m_HitSounds.Length)];
        m_AudioSource.PlayOneShot(hitSound, m_SoundVolume);
    }
    
    private void PlayParticleEffects(Vector3 impactPoint, float damage)
    {
        // 根据伤害程度选择效果
        float damageRatio = damage / (m_BuildingHealth != null ? m_BuildingHealth.m_StartingHealth : 50f);
        
        // 灰尘效果
        if (m_DustEffectPrefab != null)
        {
            GameObject dustEffect = Instantiate(m_DustEffectPrefab, impactPoint, Quaternion.identity);
            
            // 根据伤害调整效果大小
            float scale = Mathf.Lerp(0.5f, 1.5f, damageRatio);
            dustEffect.transform.localScale *= scale;
            
            Destroy(dustEffect, 3f);
        }
        
        // 火花效果（高伤害时）
        if (damageRatio > 0.3f && m_SparkEffectPrefab != null)
        {
            GameObject sparkEffect = Instantiate(m_SparkEffectPrefab, impactPoint, Quaternion.identity);
            Destroy(sparkEffect, 2f);
        }
        
        // 烟雾效果（严重受损时）
        if (damageRatio > 0.5f && m_SmokeEffectPrefab != null)
        {
            GameObject smokeEffect = Instantiate(m_SmokeEffectPrefab, impactPoint, Quaternion.identity);
            Destroy(smokeEffect, 5f);
        }
    }
    
    private IEnumerator MaterialFlashEffect()
    {
        if (m_Renderer == null || m_OriginalMaterial == null) yield break;
        
        // 创建临时材质
        Material flashMaterial = new Material(m_OriginalMaterial);
        flashMaterial.color = m_DamageFlashColor;
        
        // 应用闪烁材质
        m_Renderer.material = flashMaterial;
        
        yield return new WaitForSeconds(m_FlashDuration);
        
        // 恢复原始材质
        m_Renderer.material = m_OriginalMaterial;
        
        // 清理临时材质
        DestroyImmediate(flashMaterial);
    }
    
    private IEnumerator ScreenShakeEffect()
    {
        if (m_MainCamera == null) yield break;
        
        Vector3 originalPosition = m_MainCamera.transform.position;
        float elapsed = 0f;
        
        while (elapsed < m_ShakeDuration)
        {
            float x = Random.Range(-1f, 1f) * m_ShakeIntensity;
            float y = Random.Range(-1f, 1f) * m_ShakeIntensity;
            
            m_MainCamera.transform.position = originalPosition + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        m_MainCamera.transform.position = originalPosition;
    }
    
    public void PlayDestructionEffects(Vector3 position)
    {
        // 播放所有破坏效果
        if (m_EnableParticleEffects)
        {
            if (m_DustEffectPrefab != null)
            {
                GameObject dustEffect = Instantiate(m_DustEffectPrefab, position, Quaternion.identity);
                dustEffect.transform.localScale *= 2f; // 更大的效果
                Destroy(dustEffect, 5f);
            }
            
            if (m_SmokeEffectPrefab != null)
            {
                GameObject smokeEffect = Instantiate(m_SmokeEffectPrefab, position, Quaternion.identity);
                smokeEffect.transform.localScale *= 1.5f;
                Destroy(smokeEffect, 8f);
            }
        }
        
        // 更强的屏幕震动
        if (m_EnableScreenShake)
        {
            StartCoroutine(DestructionShakeEffect());
        }
    }
    
    private IEnumerator DestructionShakeEffect()
    {
        if (m_MainCamera == null) yield break;
        
        Vector3 originalPosition = m_MainCamera.transform.position;
        float elapsed = 0f;
        float duration = m_ShakeDuration * 2f; // 更长的震动
        float intensity = m_ShakeIntensity * 2f; // 更强的震动
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            
            m_MainCamera.transform.position = originalPosition + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        m_MainCamera.transform.position = originalPosition;
    }
}