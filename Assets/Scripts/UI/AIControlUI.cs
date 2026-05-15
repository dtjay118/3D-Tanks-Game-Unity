using UnityEngine;
using UnityEngine.UI;

public class AIControlUI : MonoBehaviour
{
    [Header("UI References")]
    public Button m_TogglePlayer1AIButton;
    public Button m_TogglePlayer2AIButton;
    public Slider m_DifficultySlider;
    public Text m_DifficultyText;
    public Text m_Player1StatusText;
    public Text m_Player2StatusText;
    
    private AIController m_AIController;
    
    private void Start()
    {
        m_AIController = FindObjectOfType<AIController>();
        
        if (m_AIController == null)
        {
            Debug.LogError("AIController not found! Please add AIController to the scene.");
            return;
        }
        
        // 设置按钮事件
        if (m_TogglePlayer1AIButton != null)
        {
            m_TogglePlayer1AIButton.onClick.AddListener(() => TogglePlayerAI(0));
        }
        
        if (m_TogglePlayer2AIButton != null)
        {
            m_TogglePlayer2AIButton.onClick.AddListener(() => TogglePlayerAI(1));
        }
        
        // 设置滑块事件
        if (m_DifficultySlider != null)
        {
            m_DifficultySlider.onValueChanged.AddListener(OnDifficultyChanged);
            m_DifficultySlider.value = m_AIController.m_AIDifficulty;
        }
        
        // 初始化UI显示
        UpdateUI();
    }
    
    private void TogglePlayerAI(int playerIndex)
    {
        if (m_AIController != null)
        {
            m_AIController.ToggleAI(playerIndex);
            UpdateUI();
        }
    }
    
    private void OnDifficultyChanged(float value)
    {
        if (m_AIController != null)
        {
            m_AIController.SetAIDifficulty(value);
            UpdateDifficultyText();
        }
    }
    
    private void UpdateUI()
    {
        if (m_AIController == null) return;
        
        // 更新玩家状态文本
        if (m_Player1StatusText != null)
        {
            m_Player1StatusText.text = m_AIController.m_UseAIForPlayer[0] ? "AI" : "Human";
        }
        
        if (m_Player2StatusText != null)
        {
            m_Player2StatusText.text = m_AIController.m_UseAIForPlayer[1] ? "AI" : "Human";
        }
        
        UpdateDifficultyText();
    }
    
    private void UpdateDifficultyText()
    {
        if (m_DifficultyText != null && m_AIController != null)
        {
            string difficultyLevel;
            float difficulty = m_AIController.m_AIDifficulty;
            
            if (difficulty < 0.3f)
                difficultyLevel = "Easy";
            else if (difficulty < 0.7f)
                difficultyLevel = "Medium";
            else
                difficultyLevel = "Hard";
            
            m_DifficultyText.text = $"AI Difficulty: {difficultyLevel} ({difficulty:F1})";
        }
    }
    
    private void Update()
    {
        // 键盘快捷键
        if (Input.GetKeyDown(KeyCode.F1))
        {
            TogglePlayerAI(0);
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            TogglePlayerAI(1);
        }
    }
}