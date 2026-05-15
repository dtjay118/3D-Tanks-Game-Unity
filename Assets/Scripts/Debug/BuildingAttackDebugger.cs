using UnityEngine;

public class BuildingAttackDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool m_EnableDebugLogs = true;
    public bool m_ShowGizmos = true;
    public Color m_ExplosionGizmoColor = Color.red;
    public Color m_BuildingGizmoColor = Color.yellow;
    
    private ShellExplosion m_ShellExplosion;
    
    private void Start()
    {
        m_ShellExplosion = GetComponent<ShellExplosion>();
        
        if (m_ShellExplosion == null)
        {
            Debug.LogError("BuildingAttackDebugger: 没有找到ShellExplosion组件！");
            return;
        }
        
        // 检查Shell配置
        CheckShellConfiguration();
        
        // 检查场景中的建筑物
        CheckBuildingsInScene();
    }
    
    private void CheckShellConfiguration()
    {
        if (!m_EnableDebugLogs) return;
        
        Debug.Log("=== Shell配置检查 ===");
        Debug.Log($"Tank Mask: {m_ShellExplosion.m_TankMask.value}");
        Debug.Log($"Building Mask: {m_ShellExplosion.m_BuildingMask.value}");
        Debug.Log($"Building Damage: {m_ShellExplosion.m_BuildingDamage}");
        Debug.Log($"Explosion Radius: {m_ShellExplosion.m_ExplosionRadius}");
        
        // 检查Building Mask是否设置
        if (m_ShellExplosion.m_BuildingMask.value == 0)
        {
            Debug.LogWarning("⚠️ Building Mask未设置！建筑物不会受到伤害。");
        }
        
        // 检查Building Damage是否大于0
        if (m_ShellExplosion.m_BuildingDamage <= 0)
        {
            Debug.LogWarning("⚠️ Building Damage为0或负数！建筑物不会受到伤害。");
        }
    }
    
    private void CheckBuildingsInScene()
    {
        if (!m_EnableDebugLogs) return;
        
        BuildingHealth[] buildings = FindObjectsOfType<BuildingHealth>();
        Debug.Log($"=== 场景中的建筑物检查 ===");
        Debug.Log($"找到 {buildings.Length} 个BuildingHealth组件");
        
        for (int i = 0; i < buildings.Length; i++)
        {
            BuildingHealth building = buildings[i];
            GameObject buildingObj = building.gameObject;
            
            Debug.Log($"建筑物 {i + 1}: {buildingObj.name}");
            Debug.Log($"  - 图层: {buildingObj.layer} ({LayerMask.LayerToName(buildingObj.layer)})");
            Debug.Log($"  - 生命值: {building.m_StartingHealth}");
            Debug.Log($"  - 有Collider: {buildingObj.GetComponent<Collider>() != null}");
            
            // 检查图层是否匹配Building Mask
            int buildingLayer = buildingObj.layer;
            bool layerMatches = (m_ShellExplosion.m_BuildingMask.value & (1 << buildingLayer)) != 0;
            
            if (!layerMatches)
            {
                Debug.LogWarning($"⚠️ 建筑物 {buildingObj.name} 的图层 {buildingLayer} 不在Building Mask中！");
                Debug.LogWarning($"   Building Mask值: {m_ShellExplosion.m_BuildingMask.value}");
                Debug.LogWarning($"   建议将建筑物设置到正确的图层，或修改Shell的Building Mask");
            }
            else
            {
                Debug.Log($"✅ 建筑物 {buildingObj.name} 图层配置正确");
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!m_EnableDebugLogs) return;
        
        Debug.Log($"=== 爆炸触发调试 ===");
        Debug.Log($"触发对象: {other.name}");
        Debug.Log($"触发对象图层: {other.gameObject.layer}");
        
        // 检查爆炸范围内的建筑物
        Collider[] buildingColliders = Physics.OverlapSphere(transform.position, m_ShellExplosion.m_ExplosionRadius, m_ShellExplosion.m_BuildingMask);
        Debug.Log($"爆炸范围内找到 {buildingColliders.Length} 个建筑物");
        
        for (int i = 0; i < buildingColliders.Length; i++)
        {
            BuildingHealth buildingHealth = buildingColliders[i].GetComponent<BuildingHealth>();
            float distance = Vector3.Distance(transform.position, buildingColliders[i].transform.position);
            
            Debug.Log($"建筑物 {i + 1}: {buildingColliders[i].name}");
            Debug.Log($"  - 距离: {distance:F2}");
            Debug.Log($"  - 有BuildingHealth: {buildingHealth != null}");
            
            if (buildingHealth != null)
            {
                float damageMultiplier = Mathf.Clamp01((m_ShellExplosion.m_ExplosionRadius - distance) / m_ShellExplosion.m_ExplosionRadius);
                float buildingDamage = m_ShellExplosion.m_BuildingDamage * damageMultiplier;
                Debug.Log($"  - 计算伤害: {buildingDamage:F2}");
                Debug.Log($"  - 当前生命值: {buildingHealth.m_CurrentHealth:F2}");
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!m_ShowGizmos || m_ShellExplosion == null) return;
        
        // 绘制爆炸范围
        Gizmos.color = m_ExplosionGizmoColor;
        Gizmos.DrawWireSphere(transform.position, m_ShellExplosion.m_ExplosionRadius);
        
        // 绘制范围内的建筑物
        Collider[] buildingColliders = Physics.OverlapSphere(transform.position, m_ShellExplosion.m_ExplosionRadius, m_ShellExplosion.m_BuildingMask);
        
        Gizmos.color = m_BuildingGizmoColor;
        foreach (Collider building in buildingColliders)
        {
            if (building != null)
            {
                Gizmos.DrawLine(transform.position, building.transform.position);
                Gizmos.DrawWireCube(building.transform.position, building.bounds.size);
            }
        }
    }
}