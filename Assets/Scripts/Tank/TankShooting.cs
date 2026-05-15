using UnityEngine;
using UnityEngine.UI;

public class TankShooting : MonoBehaviour
{
    [System.Serializable]
    public class WeaponConfig
    {
        public string m_WeaponName = "Default";
        public Rigidbody m_ShellPrefab;
        public float m_MinLaunchForce = 15f;
        public float m_MaxLaunchForce = 30f;
        public float m_MaxChargeTime = 0.75f;
        public float m_ReloadTime = 0.35f;
        public bool m_InfiniteAmmo = false;
        public int m_StartAmmo = 10;
        public int m_MaxAmmo = 30;
    }

    public int m_PlayerNumber = 1;              // Used to identify the different players.
    public Rigidbody m_Shell;                   // Legacy single-shell fallback.
    public Transform m_FireTransform;           // A child of the tank where the shells are spawned.
    public Slider m_AimSlider;                  // A child of the tank that displays the current launch force.
    public AudioSource m_ShootingAudio;         // Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.
    public AudioClip m_ChargingClip;            // Audio that plays when each shot is charging up.
    public AudioClip m_FireClip;                // Audio that plays when each shot is fired.
    public float m_MinLaunchForce = 15f;        // The force given to the shell if the fire button is not held.
    public float m_MaxLaunchForce = 30f;        // The force given to the shell if the fire button is held for the max charge time.
    public float m_MaxChargeTime = 0.75f;       // How long the shell can charge for before it is fired at max force.
    public WeaponConfig[] m_Weapons;            // Configured weapons that can be switched at runtime.
    public KeyCode m_PrevWeaponKey = KeyCode.Q;
    public KeyCode m_NextWeaponKey = KeyCode.E;

    [Header("Ammo (Shared)")]
    public bool m_InfiniteAmmo = false;
    public int m_StartAmmo = 30;
    public int m_MaxAmmo = 120;


    private string m_FireButton;                // The input axis that is used for launching shells.
    private float m_CurrentLaunchForce;         // The force that will be given to the shell when the fire button is released.
    private float m_ChargeSpeed;                // How fast the launch force increases, based on the max charge time.
    private bool m_Fired;                       // Whether or not the shell has been launched with this button press.
    private TankAI m_TankAI;                    // AI控制组件引用
    private bool m_AIFireButtonHeld;            // AI射击按钮持续按下状态
    private int m_CurrentWeaponIndex;
    private float m_NextFireTime;
    private int m_CurrentAmmo;

    public int CurrentWeaponIndex => m_CurrentWeaponIndex;
    public bool IsReloading => Time.time < m_NextFireTime;
    public float ReloadRemaining => Mathf.Max(0f, m_NextFireTime - Time.time);
    public float ReloadDuration => GetCurrentReloadTime();
    public string CurrentWeaponName
    {
        get
        {
            WeaponConfig weapon = GetCurrentWeapon();
            if (weapon == null)
            {
                return "Default";
            }

            return string.IsNullOrEmpty(weapon.m_WeaponName) ? "Weapon " + (m_CurrentWeaponIndex + 1) : weapon.m_WeaponName;
        }
    }
    public int CurrentWeaponAmmo => GetAmmoForWeapon(m_CurrentWeaponIndex);
    public WeaponConfig[] WeaponConfigs => m_Weapons;
    public int CurrentAmmo => m_InfiniteAmmo ? int.MaxValue : Mathf.Clamp(m_CurrentAmmo, 0, Mathf.Max(0, m_MaxAmmo));


    private void OnEnable()
    {
        // 获取AI组件引用
        m_TankAI = GetComponent<TankAI>();

        EnsureWeaponList();
        InitializeSharedAmmo();
        ApplyCurrentWeaponConfig();
        m_CurrentLaunchForce = m_MinLaunchForce;
        m_NextFireTime = 0f;

        if (m_AimSlider != null)
        {
            m_AimSlider.minValue = m_MinLaunchForce;
            m_AimSlider.maxValue = m_MaxLaunchForce;
            m_AimSlider.value = m_MinLaunchForce;
        }
    }


    private void Start ()
    {
        // The fire axis is based on the player number.
        m_FireButton = "Fire" + m_PlayerNumber;

        // The rate that the launch force charges up is the range of possible forces by the max charge time.
        m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / Mathf.Max(0.01f, m_MaxChargeTime);
    }


    private void Update ()
    {
        // 处理AI或玩家输入
        bool fireButtonDown, fireButton, fireButtonUp;
        
        bool isAI = m_TankAI != null && m_TankAI.m_IsAI;

        if (isAI)
        {
            // 使用AI输入
            bool currentAIFireInput = m_TankAI.GetFireInput();
            
            // 模拟按钮状态
            fireButtonDown = currentAIFireInput && !m_AIFireButtonHeld;
            fireButton = currentAIFireInput;
            fireButtonUp = !currentAIFireInput && m_AIFireButtonHeld;
            
            m_AIFireButtonHeld = currentAIFireInput;
        }
        else
        {
            HandleWeaponSwitchInput();

            // 使用玩家输入
            fireButtonDown = Input.GetButtonDown(m_FireButton);
            fireButton = Input.GetButton(m_FireButton);
            fireButtonUp = Input.GetButtonUp(m_FireButton);
        }

        if (isAI)
        {
            AutoSelectWeaponForAI();
        }
        
        // The slider should have a default value of the minimum launch force.
        if (m_AimSlider != null && m_Fired)
        {
            m_AimSlider.value = m_MinLaunchForce;
        }

        // If the max force has been exceeded and the shell hasn't yet been launched...
        if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
        {
            // ... use the max force and launch the shell.
            m_CurrentLaunchForce = m_MaxLaunchForce;
            Fire ();
        }
        // Otherwise, if the fire button has just started being pressed...
        else if (fireButtonDown)
        {
            if (Time.time < m_NextFireTime)
            {
                return;
            }

            // ... reset the fired flag and reset the launch force.
            m_Fired = false;
            m_CurrentLaunchForce = m_MinLaunchForce;

            // Change the clip to the charging clip and start it playing.
            m_ShootingAudio.clip = m_ChargingClip;
            m_ShootingAudio.Play ();
        }
        // Otherwise, if the fire button is being held and the shell hasn't been launched yet...
        else if (fireButton && !m_Fired)
        {
            // Increment the launch force and update the slider.
            m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;

            if (m_AimSlider != null)
            {
                m_AimSlider.value = m_CurrentLaunchForce;
            }
        }
        // Otherwise, if the fire button is released and the shell hasn't been launched yet...
        else if (fireButtonUp && !m_Fired)
        {
            // ... launch the shell.
            Fire ();
        }
    }


    private void Fire ()
    {
        if (Time.time < m_NextFireTime)
        {
            return;
        }

        // Set the fired flag so only Fire is only called once.
        m_Fired = true;

        // Create an instance of the shell and store a reference to it's rigidbody.
        Rigidbody shellPrefab = GetCurrentShellPrefab();
        if (shellPrefab == null)
        {
            return;
        }
        if (!CanFireCurrentWeapon())
        {
            return;
        }

        Rigidbody shellInstance =
            Instantiate (shellPrefab, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;

        // Set the shell's velocity to the launch force in the fire position's forward direction.
        shellInstance.velocity = m_CurrentLaunchForce * m_FireTransform.forward;

        // Change the clip to the firing clip and play it.
        m_ShootingAudio.clip = m_FireClip;
        m_ShootingAudio.Play ();

        // Reset the launch force.  This is a precaution in case of missing button events.
        m_CurrentLaunchForce = m_MinLaunchForce;
        ConsumeSharedAmmo();
        m_NextFireTime = Time.time + GetCurrentReloadTime();
    }

    private void HandleWeaponSwitchInput()
    {
        if (Input.GetKeyDown(m_PrevWeaponKey))
        {
            CycleWeapon(-1);
        }

        if (Input.GetKeyDown(m_NextWeaponKey))
        {
            CycleWeapon(1);
        }
    }

    private void CycleWeapon(int direction)
    {
        if (m_Weapons == null || m_Weapons.Length == 0)
        {
            return;
        }

        m_CurrentWeaponIndex += direction;
        if (m_CurrentWeaponIndex >= m_Weapons.Length)
        {
            m_CurrentWeaponIndex = 0;
        }
        else if (m_CurrentWeaponIndex < 0)
        {
            m_CurrentWeaponIndex = m_Weapons.Length - 1;
        }

        ApplyCurrentWeaponConfig();
    }

    private void AutoSelectWeaponForAI()
    {
        if (m_Weapons == null || m_Weapons.Length <= 1 || m_TankAI == null)
        {
            return;
        }

        float targetDistance = m_TankAI.GetTargetDistance();
        if (targetDistance == Mathf.Infinity)
        {
            return;
        }

        int bestWeaponIndex = m_CurrentWeaponIndex;
        float smallestDistanceGap = Mathf.Infinity;

        for (int i = 0; i < m_Weapons.Length; i++)
        {
            WeaponConfig weapon = m_Weapons[i];
            float idealDistance = (weapon.m_MinLaunchForce + weapon.m_MaxLaunchForce) * 0.5f;
            float distanceGap = Mathf.Abs(targetDistance - idealDistance);
            if (distanceGap < smallestDistanceGap)
            {
                smallestDistanceGap = distanceGap;
                bestWeaponIndex = i;
            }
        }

        if (bestWeaponIndex != m_CurrentWeaponIndex)
        {
            m_CurrentWeaponIndex = bestWeaponIndex;
            ApplyCurrentWeaponConfig();
        }
    }

    private void EnsureWeaponList()
    {
        WeaponConfig[] previousWeapons = m_Weapons;
        m_Weapons = BuildFixedWeaponConfigs(previousWeapons);
        m_CurrentWeaponIndex = 0;
    }

    private WeaponConfig[] BuildFixedWeaponConfigs(WeaponConfig[] previousWeapons)
    {
        return new WeaponConfig[]
        {
            CreateFixedWeapon("Cannon (Balanced)", 14f, 28f, 0.75f, 0.45f, GetShellByNameOrIndex(previousWeapons, "Cannon (Balanced)", 0)),
            CreateFixedWeapon("Howitzer (Heavy)", 18f, 36f, 1.10f, 1.20f, GetShellByNameOrIndex(previousWeapons, "Howitzer (Heavy)", 1)),
            CreateFixedWeapon("Mortar (Arc)", 10f, 22f, 0.90f, 0.80f, GetShellByNameOrIndex(previousWeapons, "Mortar (Arc)", 2)),
            CreateFixedWeapon("Quick Shot (Fast)", 12f, 20f, 0.35f, 0.20f, GetShellByNameOrIndex(previousWeapons, "Quick Shot (Fast)", 3)),
            CreateFixedWeapon("Sniper Shell (Long Range)", 24f, 42f, 1.00f, 1.60f, GetShellByNameOrIndex(previousWeapons, "Sniper Shell (Long Range)", 4))
        };
    }

    private WeaponConfig CreateFixedWeapon(string name, float minLaunchForce, float maxLaunchForce, float maxChargeTime, float reloadTime, Rigidbody shellPrefab)
    {
        return new WeaponConfig
        {
            m_WeaponName = name,
            m_ShellPrefab = shellPrefab != null ? shellPrefab : m_Shell,
            m_MinLaunchForce = minLaunchForce,
            m_MaxLaunchForce = maxLaunchForce,
            m_MaxChargeTime = maxChargeTime,
            m_ReloadTime = reloadTime,
            m_InfiniteAmmo = false,
            m_StartAmmo = 10,
            m_MaxAmmo = 30
        };
    }

    private Rigidbody GetShellByNameOrIndex(WeaponConfig[] previousWeapons, string weaponName, int fallbackIndex)
    {
        if (previousWeapons == null || previousWeapons.Length == 0)
        {
            return m_Shell;
        }

        for (int i = 0; i < previousWeapons.Length; i++)
        {
            WeaponConfig weapon = previousWeapons[i];
            if (weapon != null && weapon.m_WeaponName == weaponName && weapon.m_ShellPrefab != null)
            {
                return weapon.m_ShellPrefab;
            }
        }

        if (fallbackIndex >= 0 && fallbackIndex < previousWeapons.Length && previousWeapons[fallbackIndex] != null && previousWeapons[fallbackIndex].m_ShellPrefab != null)
        {
            return previousWeapons[fallbackIndex].m_ShellPrefab;
        }

        return m_Shell;
    }

    private void InitializeSharedAmmo()
    {
        if (m_InfiniteAmmo)
        {
            m_CurrentAmmo = int.MaxValue;
            return;
        }

        int maxAmmo = Mathf.Max(0, m_MaxAmmo);
        m_CurrentAmmo = Mathf.Clamp(m_StartAmmo, 0, maxAmmo);
    }

    private void ApplyCurrentWeaponConfig()
    {
        WeaponConfig weapon = GetCurrentWeapon();
        if (weapon == null)
        {
            return;
        }

        m_MinLaunchForce = weapon.m_MinLaunchForce;
        m_MaxLaunchForce = weapon.m_MaxLaunchForce;
        m_MaxChargeTime = weapon.m_MaxChargeTime;
        m_Shell = weapon.m_ShellPrefab;
        m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / Mathf.Max(0.01f, m_MaxChargeTime);
        m_CurrentLaunchForce = Mathf.Clamp(m_CurrentLaunchForce, m_MinLaunchForce, m_MaxLaunchForce);

        if (m_AimSlider != null)
        {
            m_AimSlider.minValue = m_MinLaunchForce;
            m_AimSlider.maxValue = m_MaxLaunchForce;
            m_AimSlider.value = m_CurrentLaunchForce;
        }
    }

    private WeaponConfig GetCurrentWeapon()
    {
        if (m_Weapons == null || m_Weapons.Length == 0)
        {
            return null;
        }

        return m_Weapons[m_CurrentWeaponIndex];
    }

    private bool CanFireCurrentWeapon()
    {
        if (m_InfiniteAmmo)
        {
            return true;
        }

        return m_CurrentAmmo > 0;
    }

    private void ConsumeSharedAmmo()
    {
        if (m_InfiniteAmmo)
        {
            return;
        }

        m_CurrentAmmo = Mathf.Max(0, m_CurrentAmmo - 1);
    }

    private Rigidbody GetCurrentShellPrefab()
    {
        WeaponConfig weapon = GetCurrentWeapon();
        if (weapon != null && weapon.m_ShellPrefab != null)
        {
            return weapon.m_ShellPrefab;
        }

        return m_Shell;
    }

    private float GetCurrentReloadTime()
    {
        WeaponConfig weapon = GetCurrentWeapon();
        if (weapon == null)
        {
            return 0.35f;
        }

        return Mathf.Max(0f, weapon.m_ReloadTime);
    }

    public int GetAmmoForWeapon(int weaponIndex)
    {
        return CurrentAmmo;
    }

    public bool AddAmmo(int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        if (m_InfiniteAmmo)
        {
            return false;
        }

        int maxAmmo = Mathf.Max(0, m_MaxAmmo);
        int beforeAmmo = m_CurrentAmmo;
        m_CurrentAmmo = Mathf.Clamp(beforeAmmo + amount, 0, maxAmmo);
        return m_CurrentAmmo > beforeAmmo;
    }

    // Backward-compatible: resource points may still call this.
    public bool AddAmmoForShell(Rigidbody shellPrefab, int amount)
    {
        return AddAmmo(amount);
    }
}