using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CastObject : MonoBehaviour
{

    [Header("Values")]
    public AudioClip hitSounds;
    public AudioSource a_Source;
    public float radius = 0.2f;
    public GameObject model;
    public ParticleSystem hitParticles;
    public float speed = 10f;
    public LayerMask collisionMask;
    private ConnectionManager sentFrom;

    private bool initialised = false;

    private Vector3 lastPosition;

    bool hasHit = false;
    private void Update()
    {
        if (initialised)
        {
            Vector3 rayDir = transform.position - lastPosition;
            float rayDistance = Vector3.Distance(transform.position, lastPosition);

            transform.rotation = Quaternion.LookRotation(rayDir);

            if (!hasHit)
            {
                RaycastHit hit;
                if (Physics.SphereCast(lastPosition, radius, rayDir, out hit, rayDistance, collisionMask, QueryTriggerInteraction.Ignore))
                {
                    hasHit = true;
                    HitAtPoint(hit.point, hit.transform.gameObject);
                }
                else
                {
                    float dirBetween = Vector3.Distance(transform.position, initialPosition);
                    if (dirBetween >= targetRange - 0.5f)
                    {
                        hasHit = true;
                        NoHit();
                    }
                }
            }
        }
    }
    private Coroutine moveCoroutine;
    private Vector3 initialPosition;
    private float targetRange;
    public void SetInitialValues(Vector3 target, ConnectionManager fromConnection, float range)
    {
        model.SetActive(true);
        sentFrom = fromConnection;
        targetRange = range;
        lastPosition = transform.position;

        float distance = Vector3.Distance(transform.position, target);
        float divisor = distance / 100f;

        float timeToReach = speed * divisor;
        initialPosition = fromConnection.transform.position;
        moveCoroutine = StartCoroutine(MoveToPosition(transform, target, timeToReach));

        lifeTimeCoroutine = StartCoroutine(StartLifetime());

        initialised = true;
    }

    private Coroutine lifeTimeCoroutine;

    IEnumerator StartLifetime()
    {
        yield return new WaitForSeconds(3f);
        NoHit();
    }

    void HitAtPoint(Vector3 point, GameObject hitObject)
    {
        initialised = false;

        if(moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        if(lifeTimeCoroutine != null)
        {
            StopCoroutine(lifeTimeCoroutine);
        }

        a_Source.PlayOneShot(hitSounds);

        sentFrom.CastHitSomething(point, hitObject);
        StartCoroutine(EndLife());       
    }

    void NoHit()
    {
        if (lifeTimeCoroutine != null)
        {
            StopCoroutine(lifeTimeCoroutine);
        }
        initialised = false;
        sentFrom.NoCastHit(transform.position);
        // Debug.Log("targetRange " + targetRange + " dist" + Vector3.Distance(transform.position, initialPosition));
        Destroy(gameObject);
    }

    IEnumerator EndLife()
    {
        
        hitParticles.Play();
        model.SetActive(false);
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }

    public IEnumerator MoveToPosition(Transform transform, Vector3 position, float timeTo)
    {
        var currentPos = transform.position;
        var t = 0f;
        while (t < 1)
        {
            t += Time.deltaTime / timeTo;
            transform.position = Vector3.Lerp(currentPos, position, t);
            yield return null;
        }
    }

}
