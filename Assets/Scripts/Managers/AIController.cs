using UnityEngine;
using System.Collections;

public class AIController : MonoBehaviour
{
    [Header("Manual Setup (Optional)")]
    [Tooltip("如果自动查找失败，可以手动拖拽GameManager到这里")]
    public GameManager m_ManualGameManager;
    
    [Header("AI Configuration")]
    [Tooltip("选择哪些玩家使用AI控制")]
    public bool[] m_UseAIForPlayer = new bool[2] { false, true }; // 默认玩家2使用AI
    
    [Header("AI Difficulty Settings")]
    [Range(0.1f, 1.0f)]
    public float m_AIDifficulty = 0.7f;
    
    private GameManager m_GameManager;
    private bool m_IsInitialized = false;
    
    private void Start()
    {
        // 延迟初始化，确保GameManager有时间完成自己的初始化
        Invoke("InitializeAI", 0.5f);
    }
    
    private void InitializeAI()
    {
        // 首先检查是否手动分配了GameManager
        if (m_ManualGameManager != null)
        {
            m_GameManager = m_ManualGameManager;
            Debug.Log("Using manually assigned GameManager");
        }
        else
        {
            // 尝试查找GameManager
            m_GameManager = FindObjectOfType<GameManager>();
        }
        
        if (m_GameManager != null)
        {
            Debug.Log("GameManager found successfully!");
            StartCoroutine(SetupAICoroutine());
            m_IsInitialized = true;
        }
        else
        {
            Debug.LogError("GameManager not found! Please make sure GameManager exists in the scene.");
            // 重试机制
            if (!m_IsInitialized)
            {
                InvokeRepeating("RetryInitialization", 1f, 1f);
            }
        }
    }
    
    private int retryCount = 0;
    private void RetryInitialization()
    {
        retryCount++;
        Debug.Log($"Retrying GameManager initialization... Attempt {retryCount}");
        
        m_GameManager = FindObjectOfType<GameManager>();
        if (m_GameManager != null)
        {
            Debug.Log("GameManager found on retry!");
            StartCoroutine(SetupAICoroutine());
            m_IsInitialized = true;
            CancelInvoke("RetryInitialization");
        }
        else if (retryCount >= 5)
        {
            Debug.LogError("Failed to find GameManager after 5 attempts. Please check your scene setup.");
            CancelInvoke("RetryInitialization");
        }
    }
    
    private IEnumerator SetupAICoroutine()
    {
        // 等待坦克完全实例化
        yield return new WaitForSeconds(1f);
        
        if (m_GameManager.m_Tanks == null)
        {
            Debug.LogError("GameManager.m_Tanks is null!");
            yield break;
        }
        
        for (int i = 0; i < m_GameManager.m_Tanks.Length && i < m_UseAIForPlayer.Length; i++)
        {
            if (m_UseAIForPlayer[i])
            {
                if (m_GameManager.m_Tanks[i] == null)
                {
                    Debug.LogError($"Tank {i} is null!");
                    continue;
                }
                
                GameObject tankInstance = m_GameManager.m_Tanks[i].m_Instance;
                
                if (tankInstance != null)
                {
                    // 添加AI组件
                    TankAI aiComponent = tankInstance.GetComponent<TankAI>();
                    if (aiComponent == null)
                    {
                        aiComponent = tankInstance.AddComponent<TankAI>();
                    }
                    
                    // 等待一帧确保组件正确添加
                    yield return null;
                    
                    // 配置AI设置
                    aiComponent.m_IsAI = true;
                    aiComponent.m_Aggressiveness = m_AIDifficulty;
                    aiComponent.m_Accuracy = m_AIDifficulty * 0.9f;
                    aiComponent.m_ReactionTime = (1f - m_AIDifficulty) * 0.5f + 0.1f;
                    
                    Debug.Log($"Player {i + 1} is now controlled by AI");
                }
                else
                {
                    Debug.LogError($"Tank instance for Player {i + 1} is null!");
                }
            }
        }
    }
    
    // 运行时切换AI控制
    public void ToggleAI(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= m_UseAIForPlayer.Length) return;
        
        m_UseAIForPlayer[playerIndex] = !m_UseAIForPlayer[playerIndex];
        
        if (m_GameManager != null && playerIndex < m_GameManager.m_Tanks.Length)
        {
            GameObject tankInstance = m_GameManager.m_Tanks[playerIndex].m_Instance;
            
            if (tankInstance != null)
            {
                TankAI aiComponent = tankInstance.GetComponent<TankAI>();
                
                if (m_UseAIForPlayer[playerIndex])
                {
                    // 启用AI
                    if (aiComponent == null)
                    {
                        aiComponent = tankInstance.AddComponent<TankAI>();
                    }
                    aiComponent.m_IsAI = true;
                    Debug.Log($"Player {playerIndex + 1} AI enabled");
                }
                else
                {
                    // 禁用AI
                    if (aiComponent != null)
                    {
                        aiComponent.m_IsAI = false;
                    }
                    Debug.Log($"Player {playerIndex + 1} AI disabled");
                }
            }
        }
    }
    
    // 设置AI难度
    public void SetAIDifficulty(float difficulty)
    {
        m_AIDifficulty = Mathf.Clamp01(difficulty);
        
        // 更新所有AI坦克的难度
        if (m_GameManager != null)
        {
            for (int i = 0; i < m_GameManager.m_Tanks.Length && i < m_UseAIForPlayer.Length; i++)
            {
                if (m_UseAIForPlayer[i])
                {
                    GameObject tankInstance = m_GameManager.m_Tanks[i].m_Instance;
                    TankAI aiComponent = tankInstance?.GetComponent<TankAI>();
                    
                    if (aiComponent != null)
                    {
                        aiComponent.m_Aggressiveness = m_AIDifficulty;
                        aiComponent.m_Accuracy = m_AIDifficulty * 0.9f;
                        aiComponent.m_ReactionTime = (1f - m_AIDifficulty) * 0.5f + 0.1f;
                    }
                }
            }
        }
    }
}