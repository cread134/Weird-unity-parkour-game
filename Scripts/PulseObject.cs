using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PulseObject : MonoBehaviour
{
    [Header("Values")]
    public GameObject model;
    public ParticleSystem hitParticles;
    public float speed = 10f;
    public float damage = 25f;
    public LayerMask collisionMask;
    private ConnectionManager sentFrom;
    [Space]
    public AudioSource a_source;
    public AudioClip[] hitSounds;


    private bool initialised = false;

    private Vector3 lastPosition;

    private void Update()
    {
        if (initialised)
        {
            //lerp
            //in case enemy dies before getting tehre
            if(targetObj.transform == null)
            {
                Destroy(gameObject);
            }

            transform.position = Vector3.Lerp(transform.position, targetObj.connectPoint.position, speed * Time.deltaTime);

            //check col
            Vector3 rayDir = transform.position - lastPosition;
            float rayDistance = Vector3.Distance(transform.position, lastPosition);

            RaycastHit hit;
            if (Physics.Raycast(lastPosition, rayDir, out hit, rayDistance + 0.1f, collisionMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.gameObject == targetObj.transform.gameObject)
                {
                    HitAtPoint(hit.point, hit.transform.gameObject);
                }
            }
        }
    }
    private CastReceiver targetObj;
    public void SetInitialValues(GameObject target, ConnectionManager fromConnection)
    {
        sentFrom = fromConnection;
        targetObj = target.GetComponent<CastReceiver>();
        lastPosition = transform.position;


        initialised = true;

        StartCoroutine(LifeIenumerator());
    }

    public float lifeTime = 5f;

    void HitAtPoint(Vector3 point, GameObject hitObject)
    {
        StopAllCoroutines();

        AudioClip targClip = hitSounds[Random.Range(0, hitSounds.Length)];
        a_source.PlayOneShot(targClip);

        Debug.Log("hit enemy " + hitObject);

        initialised = false;

        EnemyBrain e_Brain = hitObject.GetComponent<EnemyBrain>();

        Vector3 dir = point - transform.position;

        e_Brain.DisruptAttack();
        e_Brain.TakeDamage(damage, point, dir);

        sentFrom.DoHitMarker();
        hitParticles.Play();
        StartCoroutine(EndLife());

    }

    IEnumerator EndLife()
    {

        hitParticles.Play();
        model.SetActive(false);
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }
    IEnumerator LifeIenumerator()
    {
        yield return new WaitForSeconds(lifeTime);
        FissleOut();
    }
    void FissleOut()
    {
        Destroy(gameObject);
    }
}
