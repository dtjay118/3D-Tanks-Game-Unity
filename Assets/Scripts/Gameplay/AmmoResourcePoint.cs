using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class AmmoResourcePoint : MonoBehaviour
{
    [Header("Resource Config")]
    public string m_PointName = "Ammo Point";
    public Rigidbody m_ShellPrefab;
    public int m_AmmoAmount = 5;
    public float m_RequiredStayTime = 2f;
    public float m_RefillCooldown = 3f;

    [Header("Debug")]
    public bool m_ShowDebugLog = false;
    public bool m_ShowRuntimeNameLabel = false;
    public float m_LabelHeight = 2f;
    public Color m_LabelColor = Color.white;
    public Vector2 m_LabelSize = new Vector2(6f, 2f);
    public float m_LabelCanvasScale = 0.03f;
    public bool m_ShowProgressRing = true;
    public Color m_RingColor = new Color(0.2f, 1f, 0.4f, 0.95f);
    public Color m_RingBackgroundColor = new Color(0f, 0f, 0f, 0.45f);
    public float m_RingSize = 160f;

    private readonly Dictionary<TankShooting, float> m_StayTimeByTank = new Dictionary<TankShooting, float>();
    private readonly Dictionary<TankShooting, float> m_NextRefillTimeByTank = new Dictionary<TankShooting, float>();
    private TextMeshProUGUI m_RuntimeNameLabel;
    private Image m_ProgressRingFill;
    private Image m_ProgressRingBackground;
    private Transform m_LabelRoot;
    private Transform m_TextBillboardRoot;
    private static Sprite s_ProgressRingSprite;

    private void Start()
    {
        EnsureRuntimeProgressRing();
        RefreshNameLabel();
    }

    private void Update()
    {
        UpdateProgressRing();
    }

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        TankShooting tankShooting = other.GetComponentInParent<TankShooting>();
        if (tankShooting == null)
        {
            return;
        }

        if (m_NextRefillTimeByTank.TryGetValue(tankShooting, out float nextRefillTime) && Time.time < nextRefillTime)
        {
            return;
        }

        float stayTime = 0f;
        m_StayTimeByTank.TryGetValue(tankShooting, out stayTime);
        stayTime += Time.deltaTime;
        m_StayTimeByTank[tankShooting] = stayTime;

        if (stayTime < m_RequiredStayTime)
        {
            return;
        }

        bool hasAddedAmmo = tankShooting.AddAmmo(m_AmmoAmount);
        m_StayTimeByTank[tankShooting] = 0f;
        m_NextRefillTimeByTank[tankShooting] = Time.time + Mathf.Max(0f, m_RefillCooldown);

        if (m_ShowDebugLog && hasAddedAmmo)
        {
            Debug.Log($"[{m_PointName}] Refilled {m_AmmoAmount} ammo for {tankShooting.name}.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        TankShooting tankShooting = other.GetComponentInParent<TankShooting>();
        if (tankShooting == null)
        {
            return;
        }

        m_StayTimeByTank.Remove(tankShooting);
    }

    private void LateUpdate()
    {
        if (m_LabelRoot != null)
        {
            // Progress ring stays flat: rotate around X 90 degrees.
            m_LabelRoot.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        if (m_TextBillboardRoot == null)
        {
            return;
        }

        // Text faces camera.
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 dir = mainCamera.transform.position - m_TextBillboardRoot.position;
            if (dir.sqrMagnitude > 0.001f)
            {
                m_TextBillboardRoot.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }
    }

    public void RefreshNameLabel()
    {
        if (!m_ShowRuntimeNameLabel)
        {
            if (m_RuntimeNameLabel != null)
            {
                m_RuntimeNameLabel.gameObject.SetActive(false);
            }
            if (m_TextBillboardRoot != null)
            {
                m_TextBillboardRoot.gameObject.SetActive(false);
            }
            return;
        }

        EnsureRuntimeNameLabel();
        if (m_RuntimeNameLabel == null)
        {
            return;
        }

        m_RuntimeNameLabel.gameObject.SetActive(true);
        if (m_TextBillboardRoot != null)
        {
            m_TextBillboardRoot.gameObject.SetActive(true);
        }
        m_RuntimeNameLabel.text = string.IsNullOrEmpty(m_PointName) ? "Ammo Point" : m_PointName;
        m_RuntimeNameLabel.color = m_LabelColor;
        if (m_LabelRoot != null)
        {
            m_LabelRoot.localPosition = new Vector3(0f, m_LabelHeight, 0f);
            m_LabelRoot.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
        if (m_TextBillboardRoot != null)
        {
            m_TextBillboardRoot.localPosition = new Vector3(0f, m_LabelHeight + 0.35f, 0f);
        }
        UpdateProgressRing();
    }

    private void EnsureRuntimeProgressRing()
    {
        if (m_ProgressRingFill != null && m_ProgressRingBackground != null)
        {
            return;
        }

        Transform existingRing = transform.Find("RuntimeNameLabel");
        if (existingRing != null)
        {
            m_LabelRoot = existingRing;
            m_ProgressRingFill = existingRing.GetComponentInChildren<Image>(true);
            Image[] images = existingRing.GetComponentsInChildren<Image>(true);
            if (images != null && images.Length >= 2)
            {
                m_ProgressRingBackground = images[0];
                m_ProgressRingFill = images[images.Length - 1];
            }
            return;
        }

        GameObject ringRoot = new GameObject("RuntimeNameLabel");
        ringRoot.transform.SetParent(transform);
        ringRoot.transform.localPosition = new Vector3(0f, m_LabelHeight, 0f);
        ringRoot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ringRoot.transform.localScale = Vector3.one;
        m_LabelRoot = ringRoot.transform;

        Canvas ringCanvas = ringRoot.AddComponent<Canvas>();
        ringCanvas.renderMode = RenderMode.WorldSpace;
        ringCanvas.worldCamera = Camera.main;
        ringCanvas.sortingOrder = 20;

        CanvasScaler ringScaler = ringRoot.AddComponent<CanvasScaler>();
        ringScaler.dynamicPixelsPerUnit = 30f;

        ringRoot.AddComponent<GraphicRaycaster>();

        RectTransform ringCanvasRect = ringRoot.GetComponent<RectTransform>();
        ringCanvasRect.sizeDelta = m_LabelSize * 100f;
        ringRoot.transform.localScale = Vector3.one * m_LabelCanvasScale;

        Sprite ringSprite = GetOrCreateProgressRingSprite();

        GameObject ringBackgroundObject = new GameObject("ProgressRingBackground");
        ringBackgroundObject.transform.SetParent(ringRoot.transform, false);
        RectTransform ringBackgroundRect = ringBackgroundObject.AddComponent<RectTransform>();
        ringBackgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        ringBackgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        ringBackgroundRect.pivot = new Vector2(0.5f, 0.5f);
        ringBackgroundRect.sizeDelta = new Vector2(m_RingSize, m_RingSize);
        ringBackgroundRect.anchoredPosition = Vector2.zero;

        m_ProgressRingBackground = ringBackgroundObject.AddComponent<Image>();
        m_ProgressRingBackground.sprite = ringSprite;
        m_ProgressRingBackground.type = Image.Type.Filled;
        m_ProgressRingBackground.fillMethod = Image.FillMethod.Radial360;
        m_ProgressRingBackground.fillAmount = 1f;
        m_ProgressRingBackground.color = m_RingBackgroundColor;

        GameObject ringFillObject = new GameObject("ProgressRingFill");
        ringFillObject.transform.SetParent(ringBackgroundObject.transform, false);
        RectTransform ringFillRect = ringFillObject.AddComponent<RectTransform>();
        ringFillRect.anchorMin = new Vector2(0.5f, 0.5f);
        ringFillRect.anchorMax = new Vector2(0.5f, 0.5f);
        ringFillRect.pivot = new Vector2(0.5f, 0.5f);
        ringFillRect.sizeDelta = new Vector2(m_RingSize, m_RingSize);
        ringFillRect.anchoredPosition = Vector2.zero;

        m_ProgressRingFill = ringFillObject.AddComponent<Image>();
        m_ProgressRingFill.sprite = ringSprite;
        m_ProgressRingFill.type = Image.Type.Filled;
        m_ProgressRingFill.fillMethod = Image.FillMethod.Radial360;
        m_ProgressRingFill.fillOrigin = (int)Image.Origin360.Top;
        m_ProgressRingFill.fillClockwise = true;
        m_ProgressRingFill.fillAmount = 0f;
        m_ProgressRingFill.color = m_RingColor;
    }

    private void EnsureRuntimeNameLabel()
    {
        EnsureRuntimeProgressRing();

        if (m_RuntimeNameLabel != null)
        {
            return;
        }

        GameObject textRoot = new GameObject("RuntimeNameLabel_TextBillboard");
        textRoot.transform.SetParent(transform);
        textRoot.transform.localPosition = new Vector3(0f, m_LabelHeight + 0.35f, 0f);
        textRoot.transform.localRotation = Quaternion.identity;
        textRoot.transform.localScale = Vector3.one * m_LabelCanvasScale;
        m_TextBillboardRoot = textRoot.transform;

        Canvas textCanvas = textRoot.AddComponent<Canvas>();
        textCanvas.renderMode = RenderMode.WorldSpace;
        textCanvas.worldCamera = Camera.main;
        textCanvas.sortingOrder = 21;

        CanvasScaler textScaler = textRoot.AddComponent<CanvasScaler>();
        textScaler.dynamicPixelsPerUnit = 30f;

        textRoot.AddComponent<GraphicRaycaster>();

        RectTransform textCanvasRect = textRoot.GetComponent<RectTransform>();
        textCanvasRect.sizeDelta = m_LabelSize * 100f;

        GameObject textObject = new GameObject("LabelText");
        textObject.transform.SetParent(textRoot.transform, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        m_RuntimeNameLabel = textObject.AddComponent<TextMeshProUGUI>();
        m_RuntimeNameLabel.alignment = TextAlignmentOptions.Center;
        m_RuntimeNameLabel.fontSize = 72f;
        m_RuntimeNameLabel.enableAutoSizing = true;
        m_RuntimeNameLabel.fontSizeMin = 36f;
        m_RuntimeNameLabel.fontSizeMax = 96f;
        m_RuntimeNameLabel.enableWordWrapping = false;
        m_RuntimeNameLabel.color = m_LabelColor;
    }

    private void UpdateProgressRing()
    {
        if (m_ProgressRingFill == null || m_ProgressRingBackground == null)
        {
            return;
        }

        if (!m_ShowProgressRing || m_RequiredStayTime <= 0f)
        {
            m_ProgressRingBackground.gameObject.SetActive(false);
            m_ProgressRingFill.gameObject.SetActive(false);
            return;
        }

        float maxProgress = 0f;
        foreach (KeyValuePair<TankShooting, float> entry in m_StayTimeByTank)
        {
            float progress = Mathf.Clamp01(entry.Value / m_RequiredStayTime);
            if (progress > maxProgress)
            {
                maxProgress = progress;
            }
        }

        bool shouldShow = maxProgress > 0f;
        m_ProgressRingBackground.gameObject.SetActive(shouldShow);
        m_ProgressRingFill.gameObject.SetActive(shouldShow);
        if (shouldShow)
        {
            m_ProgressRingFill.fillAmount = maxProgress;
            m_ProgressRingBackground.color = m_RingBackgroundColor;
            m_ProgressRingFill.color = m_RingColor;
        }
    }

    private Sprite GetOrCreateProgressRingSprite()
    {
        if (s_ProgressRingSprite != null)
        {
            return s_ProgressRingSprite;
        }

        const int textureSize = 256;
        const float outerRadius = 0.48f;
        const float innerRadius = 0.30f;

        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.name = "Runtime_ProgressRing";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        float maxRadius = textureSize * 0.5f;
        float outer = maxRadius * outerRadius;
        float inner = maxRadius * innerRadius;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                bool inRing = distance <= outer && distance >= inner;
                texture.SetPixel(x, y, inRing ? Color.white : Color.clear);
            }
        }

        texture.Apply();

        s_ProgressRingSprite = Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
        return s_ProgressRingSprite;
    }
}
