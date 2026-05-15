using UnityEngine;

public class TankAI : MonoBehaviour
{
    [Header("AI Settings")]
    public bool m_IsAI = false;                    // 是否启用AI控制
    public float m_DetectionRange = 20f;           // AI检测敌人的范围
    public float m_AttackRange = 15f;              // AI攻击范围
    public float m_SafeDistance = 8f;              // AI保持的安全距离
    public float m_UpdateInterval = 0.1f;          // AI决策更新间隔
    
    [Header("Obstacle Avoidance")]
    public float m_ObstacleDetectionDistance = 5f; // 障碍物检测距离
    public float m_AvoidanceForce = 1.5f;          // 避障力度（降低以减少抽搐）
    public LayerMask m_ObstacleLayerMask = -1;     // 障碍物图层遮罩
    public float m_AvoidanceSmoothing = 2f;        // 避障平滑度
    public float m_MinAvoidanceTime = 0.5f;        // 最小避障时间
    
    [Header("AI Behavior")]
    public float m_Aggressiveness = 0.7f;          // 攻击性 (0-1)
    public float m_Accuracy = 0.8f;                // 射击精度 (0-1)
    public float m_ReactionTime = 0.3f;            // 反应时间
    
    private TankMovement m_Movement;
    private TankShooting m_Shooting;
    private Transform m_Target;                    // 当前目标
    private Vector3 m_LastKnownTargetPos;          // 目标最后已知位置
    private float m_NextUpdateTime;
    private float m_NextShootTime;
    private AIState m_CurrentState;
    private Vector3 m_AvoidanceDirection;          // 避障方向
    private bool m_IsAvoiding;                     // 是否正在避障
    private float m_AvoidanceStartTime;            // 避障开始时间
    private Vector3 m_SmoothedAvoidanceDirection;  // 平滑的避障方向
    private float m_LastDirectionChangeTime;       // 上次方向改变时间
    
    // AI状态枚举
    private enum AIState
    {
        Patrol,      // 巡逻
        Chase,       // 追击
        Attack,      // 攻击
        Retreat,     // 撤退
        Search       // 搜索
    }
    
    // 模拟输入值
    [HideInInspector] public float m_AIMovementInput;
    [HideInInspector] public float m_AITurnInput;
    [HideInInspector] public bool m_AIFireInput;
    
    private void Awake()
    {
        m_Movement = GetComponent<TankMovement>();
        m_Shooting = GetComponent<TankShooting>();
    }
    
    private void Start()
    {
        m_CurrentState = AIState.Patrol;
        m_NextUpdateTime = Time.time + m_ReactionTime;
    }
    
    private void Update()
    {
        if (!m_IsAI) return;
        
        // AI决策更新
        if (Time.time >= m_NextUpdateTime)
        {
            UpdateAI();
            m_NextUpdateTime = Time.time + m_UpdateInterval;
        }
        
        // 执行AI行为
        ExecuteAIBehavior();
    }
    
    private void UpdateAI()
    {
        // 检测障碍物
        DetectObstacles();
        
        // 寻找最近的敌人
        FindNearestEnemy();
        
        // 根据当前状态和环境更新AI状态
        UpdateAIState();
    }
    
    private void FindNearestEnemy()
    {
        // 首先尝试通过标签查找
        GameObject[] tanks = GameObject.FindGameObjectsWithTag("Player");
        
        // 如果没有找到带"Player"标签的对象，尝试查找所有TankMovement组件
        if (tanks.Length == 0)
        {
            TankMovement[] allTanks = FindObjectsOfType<TankMovement>();
            tanks = new GameObject[allTanks.Length];
            for (int i = 0; i < allTanks.Length; i++)
            {
                tanks[i] = allTanks[i].gameObject;
            }
        }
        
        float nearestDistance = Mathf.Infinity;
        Transform nearestEnemy = null;
        
        foreach (GameObject tank in tanks)
        {
            if (tank == gameObject) continue; // 跳过自己
            
            float distance = Vector3.Distance(transform.position, tank.transform.position);
            if (distance < nearestDistance && distance <= m_DetectionRange)
            {
                // 检查是否在视线内
                if (CanSeeTarget(tank.transform))
                {
                    nearestDistance = distance;
                    nearestEnemy = tank.transform;
                }
            }
        }
        
        if (nearestEnemy != null)
        {
            m_Target = nearestEnemy;
            m_LastKnownTargetPos = nearestEnemy.position;
        }
    }
    
    private bool CanSeeTarget(Transform target)
    {
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        RaycastHit hit;
        
        if (Physics.Raycast(transform.position + Vector3.up, directionToTarget, out hit, m_DetectionRange))
        {
            return hit.transform == target;
        }
        
        return false;
    }
    
    private void DetectObstacles()
    {
        Vector3 forward = transform.forward;
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        bool obstacleDetected = false;
        Vector3 newAvoidanceDirection = Vector3.zero;
        
        // 前方检测
        RaycastHit hit;
        if (Physics.Raycast(rayStart, forward, out hit, m_ObstacleDetectionDistance, m_ObstacleLayerMask))
        {
            if (hit.transform != transform && !IsEnemyTank(hit.transform))
            {
                obstacleDetected = true;
                
                // 计算避障方向
                Vector3 avoidDirection = Vector3.Cross(forward, Vector3.up);
                
                // 检查左右哪边更安全
                bool rightClear = !Physics.Raycast(rayStart, avoidDirection, m_ObstacleDetectionDistance * 0.8f, m_ObstacleLayerMask);
                bool leftClear = !Physics.Raycast(rayStart, -avoidDirection, m_ObstacleDetectionDistance * 0.8f, m_ObstacleLayerMask);
                
                if (rightClear && !leftClear)
                {
                    newAvoidanceDirection = avoidDirection;
                }
                else if (leftClear && !rightClear)
                {
                    newAvoidanceDirection = -avoidDirection;
                }
                else if (rightClear && leftClear)
                {
                    // 两边都安全，保持当前避障方向或选择距离障碍物更远的一边
                    if (m_IsAvoiding && Vector3.Dot(m_AvoidanceDirection, avoidDirection) != 0)
                    {
                        newAvoidanceDirection = m_AvoidanceDirection; // 保持当前方向
                    }
                    else
                    {
                        // 选择距离障碍物法线更远的一边
                        Vector3 hitNormal = hit.normal;
                        newAvoidanceDirection = Vector3.Dot(avoidDirection, hitNormal) > 0 ? avoidDirection : -avoidDirection;
                    }
                }
                else
                {
                    // 两边都不安全，后退
                    newAvoidanceDirection = -forward;
                }
            }
        }
        
        // 侧面检测（减少敏感度）
        if (!obstacleDetected)
        {
            Vector3 rightDirection = Vector3.Cross(forward, Vector3.up);
            
            // 检测右侧
            if (Physics.Raycast(rayStart, rightDirection, m_ObstacleDetectionDistance * 0.6f, m_ObstacleLayerMask))
            {
                obstacleDetected = true;
                newAvoidanceDirection += -rightDirection;
            }
            
            // 检测左侧
            if (Physics.Raycast(rayStart, -rightDirection, m_ObstacleDetectionDistance * 0.6f, m_ObstacleLayerMask))
            {
                obstacleDetected = true;
                newAvoidanceDirection += rightDirection;
            }
            
            newAvoidanceDirection = newAvoidanceDirection.normalized;
        }
        
        // 更新避障状态
        if (obstacleDetected)
        {
            if (!m_IsAvoiding)
            {
                m_IsAvoiding = true;
                m_AvoidanceStartTime = Time.time;
                m_AvoidanceDirection = newAvoidanceDirection;
                m_SmoothedAvoidanceDirection = newAvoidanceDirection;
            }
            else
            {
                // 平滑避障方向变化，防止抽搐
                if (Vector3.Dot(newAvoidanceDirection, m_AvoidanceDirection) > 0.5f || 
                    Time.time - m_LastDirectionChangeTime > 0.5f)
                {
                    m_AvoidanceDirection = newAvoidanceDirection;
                    m_LastDirectionChangeTime = Time.time;
                }
            }
        }
        else
        {
            // 保持避障状态一段时间，防止频繁切换
            if (m_IsAvoiding && Time.time - m_AvoidanceStartTime > m_MinAvoidanceTime)
            {
                m_IsAvoiding = false;
                m_AvoidanceDirection = Vector3.zero;
            }
        }
        
        // 平滑避障方向
        if (m_IsAvoiding)
        {
            m_SmoothedAvoidanceDirection = Vector3.Slerp(m_SmoothedAvoidanceDirection, m_AvoidanceDirection, 
                Time.deltaTime * m_AvoidanceSmoothing);
        }
    }
    
    private bool IsEnemyTank(Transform target)
    {
        return target.GetComponent<TankMovement>() != null;
    }
    
    private void UpdateAIState()
    {
        if (m_Target == null)
        {
            m_CurrentState = AIState.Patrol;
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, m_Target.position);
        
        // 根据距离和血量决定状态
        TankHealth health = GetComponent<TankHealth>();
        float healthPercentage = 1f; // 默认满血
        
        if (health != null)
        {
            healthPercentage = health.HealthPercentage;
        }
        
        if (distanceToTarget <= m_AttackRange)
        {
            if (healthPercentage > 0.3f || m_Aggressiveness > 0.8f)
            {
                m_CurrentState = AIState.Attack;
            }
            else
            {
                m_CurrentState = AIState.Retreat;
            }
        }
        else if (distanceToTarget <= m_DetectionRange)
        {
            if (distanceToTarget > m_SafeDistance || healthPercentage > 0.5f)
            {
                m_CurrentState = AIState.Chase;
            }
            else
            {
                m_CurrentState = AIState.Retreat;
            }
        }
        else
        {
            m_CurrentState = AIState.Search;
        }
    }
    
    private void ExecuteAIBehavior()
    {
        // 重置输入
        m_AIMovementInput = 0f;
        m_AITurnInput = 0f;
        m_AIFireInput = false;
        
        switch (m_CurrentState)
        {
            case AIState.Patrol:
                PatrolBehavior();
                break;
            case AIState.Chase:
                ChaseBehavior();
                break;
            case AIState.Attack:
                AttackBehavior();
                break;
            case AIState.Retreat:
                RetreatBehavior();
                break;
            case AIState.Search:
                SearchBehavior();
                break;
        }
    }
    
    private void PatrolBehavior()
    {
        if (m_IsAvoiding)
        {
            // 避障优先
            ApplyAvoidance();
        }
        else
        {
            // 简单的巡逻：缓慢移动和平滑转向
            m_AIMovementInput = 0.4f;
            m_AITurnInput = Mathf.Sin(Time.time * 0.2f) * 0.6f; // 降低频率和幅度
        }
    }
    
    private void ChaseBehavior()
    {
        if (m_Target == null) return;
        
        if (m_IsAvoiding)
        {
            // 避障时减速并调整方向
            ApplyAvoidance();
            m_AIMovementInput *= 0.6f; // 减速
        }
        else
        {
            Vector3 directionToTarget = (m_Target.position - transform.position).normalized;
            float angleToTarget = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);
            
            // 平滑转向目标
            m_AITurnInput = Mathf.Clamp(angleToTarget / 60f, -0.8f, 0.8f); // 减少转向敏感度
            
            // 如果大致面向目标，则前进
            if (Mathf.Abs(angleToTarget) < 45f)
            {
                m_AIMovementInput = 0.8f;
            }
            else
            {
                m_AIMovementInput = 0.3f; // 转向时减速
            }
        }
    }
    
    private void AttackBehavior()
    {
        if (m_Target == null) return;
        
        Vector3 directionToTarget = (m_Target.position - transform.position).normalized;
        float angleToTarget = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);
        float distanceToTarget = Vector3.Distance(transform.position, m_Target.position);
        
        if (m_IsAvoiding)
        {
            // 避障时优先避障，但仍尝试瞄准
            ApplyAvoidance();
            m_AIMovementInput *= 0.4f; // 大幅减速
        }
        else
        {
            // 平滑瞄准目标
            m_AITurnInput = Mathf.Clamp(angleToTarget / 45f, -0.7f, 0.7f); // 减少转向敏感度
            
            // 保持适当距离
            if (distanceToTarget > m_SafeDistance * 1.2f)
            {
                m_AIMovementInput = 0.5f;
            }
            else if (distanceToTarget < m_SafeDistance * 0.8f)
            {
                m_AIMovementInput = -0.4f; // 后退
            }
            else
            {
                m_AIMovementInput = 0f; // 保持位置
            }
        }
        
        // 射击逻辑
        if (Mathf.Abs(angleToTarget) < 20f && Time.time >= m_NextShootTime && !m_IsAvoiding)
        {
            // 添加一些随机性来模拟不完美的瞄准
            float aimError = (1f - m_Accuracy) * 25f;
            if (Mathf.Abs(angleToTarget) < aimError)
            {
                m_AIFireInput = true;
                m_NextShootTime = Time.time + Random.Range(1.2f, 2.5f);
            }
        }
    }
    
    private void RetreatBehavior()
    {
        if (m_Target == null) return;
        
        if (m_IsAvoiding)
        {
            // 避障优先，但尽量远离敌人
            Vector3 combinedDirection = (m_AvoidanceDirection - (m_Target.position - transform.position).normalized).normalized;
            float angleToRetreat = Vector3.SignedAngle(transform.forward, combinedDirection, Vector3.up);
            m_AITurnInput = Mathf.Clamp(angleToRetreat / 45f, -1f, 1f);
            m_AIMovementInput = 0.7f;
        }
        else
        {
            Vector3 directionAwayFromTarget = (transform.position - m_Target.position).normalized;
            float angleToRetreat = Vector3.SignedAngle(transform.forward, directionAwayFromTarget, Vector3.up);
            
            // 转向远离目标的方向
            m_AITurnInput = Mathf.Clamp(angleToRetreat / 45f, -1f, 1f);
            
            // 如果大致面向撤退方向，则前进
            if (Mathf.Abs(angleToRetreat) < 45f)
            {
                m_AIMovementInput = 1f;
            }
        }
    }
    
    private void SearchBehavior()
    {
        if (m_IsAvoiding)
        {
            ApplyAvoidance();
        }
        else
        {
            // 搜索最后已知位置
            Vector3 directionToLastKnown = (m_LastKnownTargetPos - transform.position).normalized;
            float angleToLastKnown = Vector3.SignedAngle(transform.forward, directionToLastKnown, Vector3.up);
            
            m_AITurnInput = Mathf.Clamp(angleToLastKnown / 45f, -1f, 1f);
            
            if (Mathf.Abs(angleToLastKnown) < 30f)
            {
                m_AIMovementInput = 0.7f;
            }
        }
    }
    
    private void ApplyAvoidance()
    {
        if (m_SmoothedAvoidanceDirection == Vector3.zero) return;
        
        float angleToAvoid = Vector3.SignedAngle(transform.forward, m_SmoothedAvoidanceDirection, Vector3.up);
        
        // 限制转向速度，防止抽搐
        float maxTurnInput = 0.8f;
        m_AITurnInput = Mathf.Clamp(angleToAvoid / 45f, -maxTurnInput, maxTurnInput);
        
        // 根据避障方向调整移动
        if (Vector3.Dot(m_SmoothedAvoidanceDirection, transform.forward) < -0.5f)
        {
            // 需要后退
            m_AIMovementInput = -0.3f;
        }
        else
        {
            // 前进但减速
            m_AIMovementInput = 0.4f;
        }
    }
    
    // 供外部脚本调用的方法
    public float GetMovementInput()
    {
        return m_IsAI ? m_AIMovementInput : 0f;
    }
    
    public float GetTurnInput()
    {
        return m_IsAI ? m_AITurnInput : 0f;
    }
    
    public bool GetFireInput()
    {
        return m_IsAI ? m_AIFireInput : false;
    }

    public float GetTargetDistance()
    {
        if (!m_IsAI || m_Target == null)
        {
            return Mathf.Infinity;
        }

        return Vector3.Distance(transform.position, m_Target.position);
    }
    
    // 调试绘制
    private void OnDrawGizmosSelected()
    {
        // 绘制检测范围
        Gizmos.color = Color.yellow;
        DrawWireCircle(transform.position, m_DetectionRange);
        
        // 绘制攻击范围
        Gizmos.color = Color.red;
        DrawWireCircle(transform.position, m_AttackRange);
        
        // 绘制安全距离
        Gizmos.color = Color.green;
        DrawWireCircle(transform.position, m_SafeDistance);
        
        // 绘制障碍物检测射线
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        Gizmos.color = m_IsAvoiding ? Color.red : Color.blue;
        Gizmos.DrawRay(rayStart, transform.forward * m_ObstacleDetectionDistance);
        
        // 绘制侧面检测射线
        Vector3 rightDirection = Vector3.Cross(transform.forward, Vector3.up);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(rayStart, rightDirection * m_ObstacleDetectionDistance * 0.5f);
        Gizmos.DrawRay(rayStart, -rightDirection * m_ObstacleDetectionDistance * 0.5f);
        
        // 绘制避障方向
        if (m_IsAvoiding)
        {
            if (m_AvoidanceDirection != Vector3.zero)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, m_AvoidanceDirection * 3f);
            }
            
            if (m_SmoothedAvoidanceDirection != Vector3.zero)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(transform.position, m_SmoothedAvoidanceDirection * 3f);
            }
        }
        
        // 绘制到目标的线
        if (m_Target != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, m_Target.position);
        }
    }
    
    // 自定义绘制圆圈方法
    private void DrawWireCircle(Vector3 center, float radius)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}