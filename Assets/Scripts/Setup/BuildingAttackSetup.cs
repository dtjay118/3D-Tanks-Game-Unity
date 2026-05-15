using UnityEngine;

[System.Serializable]
public class BuildingSetupData
{
    public GameObject buildingObject;
    public float health = 50f;
    public int layer = 8; // 默认建筑物图层
}

public class BuildingAttackSetup : MonoBehaviour
{
    [Header("自动设置")]
    public bool m_AutoSetupOnStart = true;
    public int m_BuildingLayer = 8; // 建筑物图层（默认第8层）
    public float m_DefaultBuildingHealth = 50f;
    public float m_DefaultBuildingDamage = 25f;
    
    [Header("手动设置建筑物")]
    public BuildingSetupData[] m_BuildingsToSetup;
    
    [Header("Shell预制体设置")]
    public GameObject m_ShellPrefab;
    
    private void Start()
    {
        if (m_AutoSetupOnStart)
        {
            SetupBuildingAttackSystem();
        }
    }
    
    [ContextMenu("设置建筑物攻击系统")]
    public void SetupBuildingAttackSystem()
    {
        Debug.Log("=== 开始设置建筑物攻击系统 ===");
        
        // 1. 设置建筑物图层
        SetupBuildingLayers();
        
        // 2. 为建筑物添加BuildingHealth组件
        SetupBuildingHealth();
        
        // 3. 配置Shell预制体
        SetupShellPrefab();
        
        // 4. 验证设置
        ValidateSetup();
        
        Debug.Log("✅ 建筑物攻击系统设置完成！");
    }
    
    private void SetupBuildingLayers()
    {
        Debug.Log("--- 设置建筑物图层 ---");
        
        // 查找所有可能的建筑物对象
        string[] buildingNames = { "Building", "House", "Wall", "Structure", "Tower", "Factory" };
        
        foreach (string buildingName in buildingNames)
        {
            GameObject[] buildings = GameObject.FindGameObjectsWithTag("Untagged");
            foreach (GameObject obj in buildings)
            {
                if (obj.name.ToLower().Contains(buildingName.ToLower()))
                {
                    obj.layer = m_BuildingLayer;
                    Debug.Log($"设置 {obj.name} 到图层 {m_BuildingLayer}");
                }
            }
        }
        
        // 处理手动指定的建筑物
        if (m_BuildingsToSetup != null)
        {
            foreach (BuildingSetupData buildingData in m_BuildingsToSetup)
            {
                if (buildingData.buildingObject != null)
                {
                    buildingData.buildingObject.layer = buildingData.layer;
                    Debug.Log($"设置 {buildingData.buildingObject.name} 到图层 {buildingData.layer}");
                }
            }
        }
    }
    
    private void SetupBuildingHealth()
    {
        Debug.Log("--- 添加BuildingHealth组件 ---");
        
        // 查找所有在建筑物图层的对象
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == m_BuildingLayer)
            {
                // 检查是否已经有BuildingHealth组件
                BuildingHealth existingHealth = obj.GetComponent<BuildingHealth>();
                if (existingHealth == null)
                {
                    // 添加BuildingHealth组件
                    BuildingHealth buildingHealth = obj.AddComponent<BuildingHealth>();
                    buildingHealth.m_StartingHealth = m_DefaultBuildingHealth;
                    
                    // 确保有Collider
                    if (obj.GetComponent<Collider>() == null)
                    {
                        obj.AddComponent<BoxCollider>();
                        Debug.Log($"为 {obj.name} 添加了BoxCollider");
                    }
                    
                    Debug.Log($"为 {obj.name} 添加了BuildingHealth组件");
                }
                else
                {
                    Debug.Log($"{obj.name} 已经有BuildingHealth组件");
                }
            }
        }
        
        // 处理手动指定的建筑物
        if (m_BuildingsToSetup != null)
        {
            foreach (BuildingSetupData buildingData in m_BuildingsToSetup)
            {
                if (buildingData.buildingObject != null)
                {
                    BuildingHealth buildingHealth = buildingData.buildingObject.GetComponent<BuildingHealth>();
                    if (buildingHealth == null)
                    {
                        buildingHealth = buildingData.buildingObject.AddComponent<BuildingHealth>();
                    }
                    buildingHealth.m_StartingHealth = buildingData.health;
                    
                    Debug.Log($"设置 {buildingData.buildingObject.name} 生命值为 {buildingData.health}");
                }
            }
        }
    }
    
    private void SetupShellPrefab()
    {
        Debug.Log("--- 配置Shell预制体 ---");
        
        if (m_ShellPrefab == null)
        {
            // 尝试查找Shell预制体
            m_ShellPrefab = Resources.Load<GameObject>("Shell");
            if (m_ShellPrefab == null)
            {
                Debug.LogWarning("未找到Shell预制体，请手动设置");
                return;
            }
        }
        
        ShellExplosion shellExplosion = m_ShellPrefab.GetComponent<ShellExplosion>();
        if (shellExplosion != null)
        {
            // 设置建筑物图层遮罩
            shellExplosion.m_BuildingMask = 1 << m_BuildingLayer;
            shellExplosion.m_BuildingDamage = m_DefaultBuildingDamage;
            
            Debug.Log($"设置Shell的Building Mask为图层 {m_BuildingLayer}");
            Debug.Log($"设置Shell的Building Damage为 {m_DefaultBuildingDamage}");
        }
        else
        {
            Debug.LogWarning("Shell预制体没有ShellExplosion组件！");
        }
    }
    
    private void ValidateSetup()
    {
        Debug.Log("--- 验证设置 ---");
        
        // 检查建筑物
        BuildingHealth[] buildings = FindObjectsOfType<BuildingHealth>();
        Debug.Log($"找到 {buildings.Length} 个建筑物");
        
        // 检查Shell配置
        if (m_ShellPrefab != null)
        {
            ShellExplosion shellExplosion = m_ShellPrefab.GetComponent<ShellExplosion>();
            if (shellExplosion != null)
            {
                bool buildingMaskSet = shellExplosion.m_BuildingMask.value != 0;
                bool buildingDamageSet = shellExplosion.m_BuildingDamage > 0;
                
                Debug.Log($"Shell Building Mask设置: {(buildingMaskSet ? "✅" : "❌")}");
                Debug.Log($"Shell Building Damage设置: {(buildingDamageSet ? "✅" : "❌")}");
            }
        }
        
        // 检查图层设置
        string layerName = LayerMask.LayerToName(m_BuildingLayer);
        if (string.IsNullOrEmpty(layerName))
        {
            Debug.LogWarning($"⚠️ 图层 {m_BuildingLayer} 没有名称，建议在Project Settings > Tags and Layers中设置");
        }
        else
        {
            Debug.Log($"✅ 使用图层: {m_BuildingLayer} ({layerName})");
        }
    }
    
    [ContextMenu("添加调试组件到Shell")]
    public void AddDebuggerToShell()
    {
        if (m_ShellPrefab != null)
        {
            BuildingAttackDebugger debugger = m_ShellPrefab.GetComponent<BuildingAttackDebugger>();
            if (debugger == null)
            {
                m_ShellPrefab.AddComponent<BuildingAttackDebugger>();
                Debug.Log("已添加BuildingAttackDebugger到Shell预制体");
            }
            else
            {
                Debug.Log("Shell预制体已经有BuildingAttackDebugger组件");
            }
        }
        else
        {
            Debug.LogWarning("请先设置Shell预制体");
        }
    }
    
    [ContextMenu("创建测试建筑物")]
    public void CreateTestBuilding()
    {
        GameObject testBuilding = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testBuilding.name = "TestBuilding";
        testBuilding.transform.position = Vector3.zero;
        testBuilding.transform.localScale = new Vector3(2, 3, 2);
        testBuilding.layer = m_BuildingLayer;
        
        BuildingHealth buildingHealth = testBuilding.AddComponent<BuildingHealth>();
        buildingHealth.m_StartingHealth = m_DefaultBuildingHealth;
        
        // 设置材质颜色
        Renderer renderer = testBuilding.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.gray;
        }
        
        Debug.Log("创建了测试建筑物");
    }
}