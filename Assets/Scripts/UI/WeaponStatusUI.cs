using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponStatusUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text m_WeaponStatusTMPText;
    public TMP_Text m_WeaponStatusLegacyText;

    [Header("Target Tank")]
    public TankShooting m_TargetShooting;
    public TankHealth m_TargetHealth;
    public int m_TargetPlayerNumber = 1;
    public bool m_AutoFindTarget = true;
    public float m_FpsSmoothing = 0.1f;

    private float m_Fps;
    private float m_FpsVelocity;

    private void Start()
    {
        TryFindTargetShooting();
        TryFindTargetHealth();
        UpdateWeaponStatusText();
    }

    private void Update()
    {
        if (m_TargetShooting == null && m_AutoFindTarget)
        {
            TryFindTargetShooting();
        }
        if (m_TargetHealth == null && m_AutoFindTarget)
        {
            TryFindTargetHealth();
        }

        UpdateFps();

        UpdateWeaponStatusText();
    }

    private void TryFindTargetShooting()
    {
        if (m_TargetShooting != null)
        {
            return;
        }

        TankShooting[] allShooting = FindObjectsOfType<TankShooting>();
        for (int i = 0; i < allShooting.Length; i++)
        {
            if (allShooting[i].m_PlayerNumber == m_TargetPlayerNumber)
            {
                m_TargetShooting = allShooting[i];
                break;
            }
        }
    }

    private void TryFindTargetHealth()
    {
        if (m_TargetHealth != null)
        {
            return;
        }

        if (m_TargetShooting != null)
        {
            m_TargetHealth = m_TargetShooting.GetComponent<TankHealth>();
            if (m_TargetHealth != null)
            {
                return;
            }
        }

        TankHealth[] allHealth = FindObjectsOfType<TankHealth>();
        for (int i = 0; i < allHealth.Length; i++)
        {
            TankShooting shooting = allHealth[i].GetComponent<TankShooting>();
            if (shooting != null && shooting.m_PlayerNumber == m_TargetPlayerNumber)
            {
                m_TargetHealth = allHealth[i];
                break;
            }
        }
    }

    private void UpdateFps()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f)
        {
            return;
        }

        float instantFps = 1f / dt;
        float smoothTime = Mathf.Max(0.01f, m_FpsSmoothing);
        m_Fps = Mathf.SmoothDamp(m_Fps, instantFps, ref m_FpsVelocity, smoothTime);
    }

    private void UpdateWeaponStatusText()
    {
        if (m_WeaponStatusTMPText == null && m_WeaponStatusLegacyText == null)
        {
            return;
        }

        if (m_TargetShooting == null)
        {
            SetStatusText("Weapon: N/A\nAmmo: N/A\nHP: N/A\nFPS: " + m_Fps.ToString("F0") + "\nStatus: No Tank");
            return;
        }

        string weaponName = m_TargetShooting.CurrentWeaponName;
        string ammoText = m_TargetShooting.CurrentAmmo == int.MaxValue
            ? "∞"
            : m_TargetShooting.CurrentAmmo.ToString();

        string hpText = "N/A";
        if (m_TargetHealth != null)
        {
            hpText = m_TargetHealth.CurrentHealth.ToString("F0") + "/" + m_TargetHealth.MaxHealth.ToString("F0");
        }
        string statusText;

        if (m_TargetShooting.IsReloading)
        {
            statusText = "Reloading " + m_TargetShooting.ReloadRemaining.ToString("F1") + "s";
        }
        else
        {
            statusText = "Ready";
        }

        SetStatusText(
            "Weapon: " + weaponName +
            "\nAmmo: " + ammoText +
            "\nHP: " + hpText +
            "\nFPS: " + m_Fps.ToString("F0") +
            "\nStatus: " + statusText
        );
    }

    private void SetStatusText(string value)
    {
        if (m_WeaponStatusTMPText != null)
        {
            m_WeaponStatusTMPText.text = value;
        }

        if (m_WeaponStatusLegacyText != null)
        {
            m_WeaponStatusLegacyText.text = value;
        }
    }
}
