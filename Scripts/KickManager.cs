using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class KickManager : MonoBehaviour
{
    private PlayerMovement p_Movement;
    private PlayerAudioScript p_Audio;
    private ConnectionManager p_Connection;
    private PlayerHealthManager p_Health;

    private bool kicking = false;

    private float lastKick;
    [Header("Settings")]
    public Transform camTransform;
    public float kickForce;
    public LayerMask kickMask;
    public LayerMask checkMask;
    public float kickTime = 0.8f;
    public float kickCooldown = 2.5f;
    [Space]
    public float kickRadius = 0.5f;
    public float kickDistance = 1.5f;
    [Space]
    public GameObject kickObject;
    public Animator kickAnimator;
    [Space]
    public AnimationClip[] defaultKickAnimations;
    [Space]
    public AudioClip[] hitSounds;
    public AudioClip[] kickSounds;
    [Space]
    public Animator bodyRepAnimator;
    public void SetDefaultValues()
    {

    }
    public void KickInput(InputAction.CallbackContext context)
    {
        if (p_Health.IsPaused()) return;
        if (context.performed)
        {
            if(!kicking && Time.time > lastKick)
            {
                StartCoroutine(DoKick());
            }
        }
    }

    IEnumerator DoKick()
    {
        kicking = true;

        if (p_Movement.IsSliding())
        {
            bodyRepAnimator.SetTrigger("Kick");
        }
        else
        {
            kickObject.SetActive(true);
            AnimationClip targetKick = defaultKickAnimations[Random.Range(0, defaultKickAnimations.Length)];
            kickAnimator.Play(targetKick.name, 0, 0f);
        }

        yield return new WaitForSeconds(kickTime);

        kickObject.SetActive(false);
        kicking = false;
        lastKick = Time.time + kickCooldown;

    }

    public void DoKickSound()
    {
        AudioClip targClip = kickSounds[Random.Range(0, kickSounds.Length)];
        p_Audio.PlayPlayerSound(targClip);

    }

    public void DoKickCalculation()
    {
        Debug.Log("Doing calculation");

        EZCameraShake.CameraShaker.Instance.ShakeOnce(2f, 1.5f, 0f, 0.2f);

        Collider[] colls = Physics.OverlapSphere(camTransform.position + (camTransform.forward * kickDistance), kickRadius, kickMask);

        if(colls.Length > 0)
        {
            Debug.Log("Col over length");
            foreach (Collider col in colls)
            {
                Vector3 useVec = col.ClosestPoint(transform.position);
                Vector3 dir = useVec - camTransform.position;
                float dist = Vector3.Distance(useVec, camTransform.position) + 0.1f;

                Debug.DrawLine(useVec, camTransform.position, Color.red, 3f);

                RaycastHit hit;
                if (Physics.Raycast(camTransform.position, camTransform.forward, out hit, dist, checkMask))
                {
                        if (hit.transform.gameObject.GetComponent<I_Knockback>() != null)
                        {
                            hit.transform.gameObject.GetComponent<I_Knockback>().KnockBack(kickForce, hit.point, camTransform.forward);
                            Debug.Log("GaveKnockBack");

                        //audio
                        AudioClip targClip = hitSounds[Random.Range(0, hitSounds.Length)];
                        p_Audio.PlayPlayerSound(targClip);

                        //hitmarker
                        p_Connection.DoHitMarker();

                            return;
                        }                   
                }
            }        
        }
    }

    public void KickCam()
    {
        EZCameraShake.CameraShaker.Instance.ShakeOnce(7f, 0.8f, 0.4f, 1.5f);
    }

    // Start is called before the first frame update
    void Start()
    {
        p_Audio = GetComponent<PlayerAudioScript>();
        p_Movement = GetComponent<PlayerMovement>();
        p_Connection = GetComponent<ConnectionManager>();
        p_Health = GetComponent<PlayerHealthManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(camTransform.position + (camTransform.forward * kickDistance), kickRadius);      
    }
}
