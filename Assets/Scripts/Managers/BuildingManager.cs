using UnityEngine;
using System.Collections.Generic;

public class BuildingManager : MonoBehaviour
{
    [Header("Building Management")]
    public BuildingHealth[] m_Buildings;           // 场景中所有建筑物的引用
    public LayerMask m_BuildingLayer = 1 << 8;     // 建筑物图层（默认第8层）
    
    [Header("Statistics")]
    public int m_TotalBuildings;                   // 总建筑数量
    public int m_DestroyedBuildings;               // 已摧毁建筑数量
    
    private List<BuildingHealth> m_ActiveBuildings;

    private void Start()
    {
        InitializeBuildings();
    }

    private void InitializeBuildings()
    {
        // 如果没有手动分配建筑物，自动查找场景中的建筑物
        if (m_Buildings == null || m_Buildings.Length == 0)
        {
            m_Buildings = FindObjectsOfType<BuildingHealth>();
        }

        m_ActiveBuildings = new List<BuildingHealth>();
        
        foreach (BuildingHealth building in m_Buildings)
        {
            if (building != null)
            {
                m_ActiveBuildings.Add(building);
                
                // 确保建筑物在正确的图层上
                building.gameObject.layer = GetLayerFromMask(m_BuildingLayer);
            }
        }

        m_TotalBuildings = m_ActiveBuildings.Count;
        m_DestroyedBuildings = 0;

        Debug.Log($"BuildingManager: 初始化了 {m_TotalBuildings} 个建筑物");
    }

    private void Update()
    {
        // 检查建筑物状态
        CheckBuildingStatus();
    }

    private void CheckBuildingStatus()
    {
        int destroyedCount = 0;
        
        foreach (BuildingHealth building in m_Buildings)
        {
            if (building == null || building.IsDestroyed())
            {
                destroyedCount++;
            }
        }

        m_DestroyedBuildings = destroyedCount;
    }

    // 获取剩余建筑物数量
    public int GetRemainingBuildingsCount()
    {
        return m_TotalBuildings - m_DestroyedBuildings;
    }

    // 获取建筑物摧毁百分比
    public float GetDestructionPercentage()
    {
        if (m_TotalBuildings == 0) return 0f;
        return (float)m_DestroyedBuildings / m_TotalBuildings;
    }

    // 检查是否所有建筑物都被摧毁
    public bool AreAllBuildingsDestroyed()
    {
        return m_DestroyedBuildings >= m_TotalBuildings;
    }

    // 重置所有建筑物（用于重新开始游戏）
    public void ResetBuildings()
    {
        // 这里可以重新加载场景或重新实例化建筑物
        Debug.Log("BuildingManager: 重置建筑物");
    }

    // 从LayerMask获取图层索引
    private int GetLayerFromMask(LayerMask mask)
    {
        int layerNumber = 0;
        int layer = mask.value;
        while (layer > 1)
        {
            layer = layer >> 1;
            layerNumber++;
        }
        return layerNumber;
    }

    // 在Inspector中显示统计信息
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            CheckBuildingStatus();
        }
    }
}