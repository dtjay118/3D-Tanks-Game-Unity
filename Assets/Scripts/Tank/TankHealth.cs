using UnityEngine;
using UnityEngine.UI;

public class TankHealth : MonoBehaviour
{
    public float m_StartingHealth = 100f;          
    public Slider m_Slider;                        
    public Image m_FillImage;                      
    public Color m_FullHealthColor = Color.green;  
    public Color m_ZeroHealthColor = Color.red;    
    public GameObject m_ExplosionPrefab;
    
    private AudioSource m_ExplosionAudio;          
    private ParticleSystem m_ExplosionParticles;   
    private float m_CurrentHealth;  
    private bool m_Dead;            

    public float CurrentHealth => m_CurrentHealth;
    public float MaxHealth => m_StartingHealth;
    public float HealthPercentage => m_StartingHealth > 0f ? m_CurrentHealth / m_StartingHealth : 0f;
    public bool IsDead => m_Dead;


    private void Awake()
    {
        if (m_Slider == null)
        {
            m_Slider = GetComponentInChildren<Slider>(true);
        }

        if (m_FillImage == null && m_Slider != null)
        {
            m_FillImage = m_Slider.fillRect != null ? m_Slider.fillRect.GetComponent<Image>() : null;
        }

        if (m_ExplosionPrefab != null)
        {
            m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();
            if (m_ExplosionParticles != null)
            {
                m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();
                m_ExplosionParticles.gameObject.SetActive(false);
            }
        }
    }


    private void OnEnable()
    {
        m_CurrentHealth = m_StartingHealth;
        m_Dead = false;
        if (m_Slider != null)
        {
            m_Slider.minValue = 0f;
            m_Slider.maxValue = m_StartingHealth;
        }

        SetHealthUI();
    }
    

    public void TakeDamage(float amount)
    {
        // Adjust the tank's current health, update the UI based on the new health and check whether or not the tank is dead.
        m_CurrentHealth -= Mathf.Abs(amount);
        m_CurrentHealth = Mathf.Clamp(m_CurrentHealth, 0f, m_StartingHealth);
        SetHealthUI();

        if(m_CurrentHealth <=0f && !m_Dead){
            OnDeath();
        }
    }


    private void SetHealthUI()
    {
        // Adjust the value and colour of the slider.
        if (m_Slider == null)
        {
            return;
        }

        m_Slider.value = m_CurrentHealth;
        if (m_FillImage != null)
        {
            m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);
        }
    }


    private void OnDeath()
    {
        // Play the effects for the death of the tank and deactivate it.
        m_Dead = true;
        if (m_ExplosionParticles != null)
        {
            m_ExplosionParticles.transform.position = transform.position;
            m_ExplosionParticles.gameObject.SetActive(true);
            m_ExplosionParticles.Play();
        }
        if (m_ExplosionAudio != null)
        {
            m_ExplosionAudio.Play();
        }

        gameObject.SetActive(false);
    }
}