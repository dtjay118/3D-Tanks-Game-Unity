using UnityEngine;

public class BuildingDebris : MonoBehaviour
{
    [Header("Debris Settings")]
    public float m_LifeTime = 5f;                  // 碎片存在时间
    public float m_FadeStartTime = 3f;             // 开始淡出的时间
    public bool m_FadeOut = true;                  // 是否淡出消失
    
    private Renderer m_Renderer;
    private Material m_Material;
    private Color m_OriginalColor;
    private float m_Timer = 0f;

    private void Start()
    {
        m_Renderer = GetComponent<Renderer>();
        if (m_Renderer != null)
        {
            // 创建材质实例以避免影响其他对象
            m_Material = new Material(m_Renderer.material);
            m_Renderer.material = m_Material;
            m_OriginalColor = m_Material.color;
        }

        // 设置销毁时间
        Destroy(gameObject, m_LifeTime);
    }

    private void Update()
    {
        if (!m_FadeOut || m_Material == null) return;

        m_Timer += Time.deltaTime;

        // 开始淡出效果
        if (m_Timer >= m_FadeStartTime)
        {
            float fadeProgress = (m_Timer - m_FadeStartTime) / (m_LifeTime - m_FadeStartTime);
            fadeProgress = Mathf.Clamp01(fadeProgress);
            
            Color currentColor = m_OriginalColor;
            currentColor.a = Mathf.Lerp(m_OriginalColor.a, 0f, fadeProgress);
            m_Material.color = currentColor;
        }
    }

    private void OnDestroy()
    {
        // 清理材质实例
        if (m_Material != null)
        {
            DestroyImmediate(m_Material);
        }
    }
}