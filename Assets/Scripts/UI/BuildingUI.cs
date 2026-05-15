using UnityEngine;
using UnityEngine.UI;

public class BuildingUI : MonoBehaviour
{
    [Header("UI References")]
    public Text m_BuildingCountText;               // 显示建筑物数量的文本
    public Slider m_DestructionSlider;             // 显示摧毁进度的滑块
    public Text m_DestructionPercentageText;       // 显示摧毁百分比的文本
    
    [Header("Settings")]
    public bool m_ShowBuildingCount = true;        // 是否显示建筑物数量
    public bool m_ShowDestructionProgress = true;  // 是否显示摧毁进度
    public string m_BuildingCountFormat = "建筑物: {0}/{1}";
    public string m_PercentageFormat = "{0:F1}%";
    
    private BuildingManager m_BuildingManager;

    private void Start()
    {
        // 查找BuildingManager
        m_BuildingManager = FindObjectOfType<BuildingManager>();
        
        if (m_BuildingManager == null)
        {
            Debug.LogWarning("BuildingUI: 未找到BuildingManager组件");
            enabled = false;
            return;
        }

        // 初始化UI
        InitializeUI();
    }

    private void Update()
    {
        if (m_BuildingManager == null) return;

        UpdateUI();
    }

    private void InitializeUI()
    {
        // 设置滑块初始值
        if (m_DestructionSlider != null)
        {
            m_DestructionSlider.minValue = 0f;
            m_DestructionSlider.maxValue = 1f;
            m_DestructionSlider.value = 0f;
        }
    }

    private void UpdateUI()
    {
        // 更新建筑物数量显示
        if (m_ShowBuildingCount && m_BuildingCountText != null)
        {
            int remaining = m_BuildingManager.GetRemainingBuildingsCount();
            int total = m_BuildingManager.m_TotalBuildings;
            m_BuildingCountText.text = string.Format(m_BuildingCountFormat, remaining, total);
        }

        // 更新摧毁进度显示
        if (m_ShowDestructionProgress)
        {
            float destructionPercentage = m_BuildingManager.GetDestructionPercentage();
            
            if (m_DestructionSlider != null)
            {
                m_DestructionSlider.value = destructionPercentage;
            }
            
            if (m_DestructionPercentageText != null)
            {
                m_DestructionPercentageText.text = string.Format(m_PercentageFormat, destructionPercentage * 100f);
            }
        }
    }

    // 公共方法：显示建筑物被摧毁的消息
    public void ShowBuildingDestroyedMessage(string buildingName)
    {
        Debug.Log($"建筑物被摧毁: {buildingName}");
        // 这里可以添加更复杂的UI提示，比如弹出消息等
    }

    // 公共方法：检查是否达成摧毁目标
    public void CheckDestructionGoal(float targetPercentage = 0.5f)
    {
        float currentPercentage = m_BuildingManager.GetDestructionPercentage();
        
        if (currentPercentage >= targetPercentage)
        {
            Debug.Log($"摧毁目标达成！已摧毁 {currentPercentage * 100f:F1}% 的建筑物");
            // 这里可以触发游戏胜利条件或其他事件
        }
    }
}