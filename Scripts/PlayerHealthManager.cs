using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using EZCameraShake;
using TMPro;

public class PlayerHealthManager : MonoBehaviour, I_DamageAble
{
    public GameObject gameUi;

    PlayerAudioScript p_audio;
    private ConnectionManager p_Connection;

    private GameHandler gameHandler;

    private float curHealth;
    public float maxHealth = 100f;

    [Space]
    public Slider healthSlider;
    public Slider healthFollowSlider;
    public TextMeshProUGUI healthText;
    public GameObject deathPanel;
    public Animator armsAnimator;
    [Space]
    public AudioClip[] takeDamageSounds;
    public AudioClip[] tankedDamageSounds;
    public AudioClip[] healthAddSounds;
    public AudioClip deathStinger;
    [Space]
    public GameObject takeDamageImage;
    public GameObject tankedDamageScreen;
    public ParticleSystem healthAddParticles;

    public void SetDefaultValue()
    {
        stopDamage = false;
        isDead = false;

        curHealth = maxHealth;

        healthSlider.maxValue = maxHealth;
        healthSlider.value = maxHealth;

        healthText.text = curHealth.ToString();

        healthFollowSlider.value = curHealth;
        healthFollowSlider.value = maxHealth;
    }

    private bool stopDamage;
    public void BlockDamageAllowed()
    {
        stopDamage = true;
    }

    // Start is called before the first frame update
    void Awake()
    {
        p_audio = GetComponent<PlayerAudioScript>();
        p_Connection = GetComponent<ConnectionManager>();

        gameHandler = GameObject.Find("GameManager").GetComponent<GameHandler>();

        deathPanel.SetActive(false);
    }

    public ObjectPooler GetObjectPooler()
    {
        return gameHandler.gameObject.GetComponent<ObjectPooler>();
    }

   public GameObject GetPlayerUi()
    {
        return gameUi;
    }

    public bool IsPaused()
    {
        return gameHandler.IsPaused();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(10f, Vector3.zero, Vector3.zero);
        }

        if(Time.time > lastScreen)
        {
            takeDamageImage.SetActive(false);
            tankedDamageScreen.SetActive(false);
        }

        healthFollowSlider.value = Mathf.Lerp(healthFollowSlider.value, curHealth, 5f * Time.time);  
    }
    private bool isDead = false;
    public float connectionDamageDampening = 0.5f;
    public void TakeDamage(float amount, Vector3 point, Vector3 direction)
    {
        if (isDead || stopDamage || IsPaused()) return;

        //check for connection
        bool didConnect = false;
        if(p_Connection.ConnectedToEnemy() == true)
        {
            p_Connection.DamageAllReceivers(amount);
            didConnect = true;
            amount *= 0.5f;
        }

        //take damage
        curHealth = Mathf.Clamp(curHealth - amount, 0f, maxHealth);
        
        if(curHealth <= 0)
        {
            Die();
        }

        healthSlider.value = curHealth;
        healthText.text = curHealth.ToString();

        //av
        if (didConnect != true)
        {
            AudioClip targetClip = takeDamageSounds[Random.Range(0, takeDamageSounds.Length)];
            p_audio.PlayPlayerSound(targetClip);

            //show image
            takeDamageImage.SetActive(true);
            lastScreen = Time.time + 0.1f;
        }
        else
        {
            AudioClip targetClip = tankedDamageSounds[Random.Range(0, tankedDamageSounds.Length)];
            p_audio.PlayPlayerSound(targetClip);

            tankedDamageScreen.SetActive(true);
            lastScreen = Time.time + 0.1f;
        }

        //shake
        EZCameraShake.CameraShaker.Instance.ShakeOnce(1.5f, 0.9f, 0.1f, 1.5f);
    }

    private float lastScreen;
    
    public bool IsDead()
    {
        return isDead;
    }

    public void Die()
    {
        isDead = true;
        armsAnimator.Play("Anim_Arms_Die", 0, 0f);
        Debug.Log("Player died");
        deathPanel.SetActive(true);
        p_audio.PlayPlayerSound(deathStinger);

        gameHandler.PlayerDie();

        //disable everything
        GetComponent<ConnectionManager>().enabled = false;
        GetComponent<WeaponSway>().enabled = false;
        GetComponent<MouseLook>().enabled = false;
        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<KickManager>().enabled = false;

        StartCoroutine(DeathIenumerator());
    }

    public float deathTime = 3f;
    IEnumerator DeathIenumerator()
    {
        yield return new WaitForSeconds(deathTime);
        gameHandler.EndGame();
    }
    public void AddHealth(float amount)
    {
        curHealth = Mathf.Clamp(curHealth + amount, 0f, maxHealth);

        healthSlider.value = curHealth;
        healthText.text = curHealth.ToString();

        EZCameraShake.CameraShaker.Instance.ShakeOnce(0.9f, 0.9f, 0.1f, 1.5f);

        //do particles
        healthAddParticles.Play();

        //do sounds
        AudioClip targetClip = healthAddSounds[Random.Range(0, healthAddSounds.Length)];
        p_audio.PlayPlayerSound(targetClip);
    }

    public void SpawnPlayer()
    {        //enable everything
        deathPanel.SetActive(false);
        StartCoroutine(SpawnPlayerCoroutine());
    }


    public float spawnTime = 3f;
    IEnumerator SpawnPlayerCoroutine()
    {
        GetComponent<ConnectionManager>().enabled = true;
        GetComponent<WeaponSway>().enabled = true;
        GetComponent<MouseLook>().enabled = true;
        GetComponent<PlayerMovement>().enabled = true;
        GetComponent<KickManager>().enabled = true;

        isDead = true;

        yield return new WaitForSeconds(spawnTime);

        isDead = false;

        stopDamage = false;
    }
}
