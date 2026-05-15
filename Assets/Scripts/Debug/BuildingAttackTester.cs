using UnityEngine;

public class BuildingAttackTester : MonoBehaviour
{
    [Header("测试设置")]
    public KeyCode m_TestKey = KeyCode.T;
    public float m_TestDamage = 25f;
    public float m_TestRadius = 5f;
    
    [Header("可视化")]
    public bool m_ShowTestRadius = true;
    public Color m_TestRadiusColor = Color.green;
    
    private void Update()
    {
        if (Input.GetKeyDown(m_TestKey))
        {
            TestBuildingAttack();
        }
    }
    
    [ContextMenu("测试建筑物攻击")]
    public void TestBuildingAttack()
    {
        Debug.Log("=== 测试建筑物攻击 ===");
        
        // 查找范围内的建筑物
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_TestRadius);
        int buildingsFound = 0;
        int buildingsDamaged = 0;
        
        foreach (Collider collider in colliders)
        {
            BuildingHealth buildingHealth = collider.GetComponent<BuildingHealth>();
            if (buildingHealth != null)
            {
                buildingsFound++;
                
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                float damageMultiplier = Mathf.Clamp01((m_TestRadius - distance) / m_TestRadius);
                float actualDamage = m_TestDamage * damageMultiplier;
                
                Debug.Log($"攻击建筑物: {collider.name}");
                Debug.Log($"  距离: {distance:F2}");
                Debug.Log($"  伤害倍数: {damageMultiplier:F2}");
                Debug.Log($"  实际伤害: {actualDamage:F2}");
                Debug.Log($"  攻击前生命值: {buildingHealth.m_CurrentHealth:F2}");
                
                buildingHealth.TakeDamage(actualDamage);
                buildingsDamaged++;
                
                Debug.Log($"  攻击后生命值: {buildingHealth.m_CurrentHealth:F2}");
            }
        }
        
        Debug.Log($"测试完成 - 找到 {buildingsFound} 个建筑物，攻击了 {buildingsDamaged} 个");
        
        if (buildingsFound == 0)
        {
            Debug.LogWarning("⚠️ 测试范围内没有找到建筑物！");
            Debug.LogWarning("请检查：");
            Debug.LogWarning("1. 建筑物是否有BuildingHealth组件");
            Debug.LogWarning("2. 建筑物是否有Collider组件");
            Debug.LogWarning("3. 测试范围是否足够大");
        }
    }
    
    [ContextMenu("列出附近的建筑物")]
    public void ListNearbyBuildings()
    {
        Debug.Log("=== 附近的建筑物 ===");
        
        BuildingHealth[] allBuildings = FindObjectsOfType<BuildingHealth>();
        int nearbyCount = 0;
        
        foreach (BuildingHealth building in allBuildings)
        {
            float distance = Vector3.Distance(transform.position, building.transform.position);
            if (distance <= m_TestRadius * 2f) // 扩大搜索范围
            {
                nearbyCount++;
                Debug.Log($"建筑物: {building.name}");
                Debug.Log($"  位置: {building.transform.position}");
                Debug.Log($"  距离: {distance:F2}");
                Debug.Log($"  生命值: {building.m_CurrentHealth:F2}/{building.m_StartingHealth:F2}");
                Debug.Log($"  图层: {building.gameObject.layer}");
                Debug.Log($"  有Collider: {building.GetComponent<Collider>() != null}");
            }
        }
        
        Debug.Log($"在范围内找到 {nearbyCount} 个建筑物");
        
        if (nearbyCount == 0)
        {
            Debug.LogWarning("附近没有建筑物！请使用BuildingAttackSetup创建测试建筑物");
        }
    }
    
    [ContextMenu("重置附近建筑物生命值")]
    public void ResetNearbyBuildingsHealth()
    {
        BuildingHealth[] allBuildings = FindObjectsOfType<BuildingHealth>();
        int resetCount = 0;
        
        foreach (BuildingHealth building in allBuildings)
        {
            float distance = Vector3.Distance(transform.position, building.transform.position);
            if (distance <= m_TestRadius * 2f)
            {
                building.m_CurrentHealth = building.m_StartingHealth;
                resetCount++;
                
                // 重新启用渲染器和碰撞体（如果被禁用了）
                Renderer renderer = building.GetComponent<Renderer>();
                if (renderer != null) renderer.enabled = true;
                
                Collider collider = building.GetComponent<Collider>();
                if (collider != null) collider.enabled = true;
                
                // 重置材质颜色
                if (renderer != null && renderer.material != null)
                {
                    Color originalColor = renderer.material.color;
                    originalColor.r = originalColor.g = originalColor.b = 1f; // 移除变暗效果
                    renderer.material.color = originalColor;
                }
            }
        }
        
        Debug.Log($"重置了 {resetCount} 个建筑物的生命值");
    }
    
    private void OnDrawGizmos()
    {
        if (m_ShowTestRadius)
        {
            Gizmos.color = m_TestRadiusColor;
            Gizmos.DrawWireSphere(transform.position, m_TestRadius);
            
            // 绘制到附近建筑物的连线
            BuildingHealth[] allBuildings = FindObjectsOfType<BuildingHealth>();
            foreach (BuildingHealth building in allBuildings)
            {
                float distance = Vector3.Distance(transform.position, building.transform.position);
                if (distance <= m_TestRadius)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position, building.transform.position);
                }
            }
        }
    }
}