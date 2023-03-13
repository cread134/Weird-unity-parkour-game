using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DamageZone : MonoBehaviour
{
    public bool useComplexValues = false;
    public bool blockMultipleUse = false;
    [Space]
    public UnityEvent activated;
    public UnityEvent performed;

    public LayerMask damageMask;
    public float cooldown;
    public float damageDelay;
    public float damage;

    private void Start()
    {
        if (activated == null)
        {
            activated = new UnityEvent();
        }
        if (performed == null)
        {
            performed = new UnityEvent();
        }
    }

    private List<Collider> triggerColliders = new List<Collider>();
    private void OnTriggerEnter(Collider other)
    {
        triggerColliders.Add(other);
        if (other.GetComponent<I_DamageAble>() != null)
        {
            if (blockMultipleUse == true && inUse == true)
            {
                return;
            }
            //do the evenys
            activated.Invoke();
            if (useComplexValues)
            {
                StartCoroutine(UseCouroutine());
            }
            else
            {
                other.GetComponent<I_DamageAble>().TakeDamage(damage, other.ClosestPoint(transform.position), other.transform.position + transform.position);
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (triggerColliders.Contains(other))
        {
            triggerColliders.Remove(other);
        }
    }


    private bool inUse;
    IEnumerator UseCouroutine()
    {
        inUse = true;
        yield return new WaitForSeconds(damageDelay);

        //calculate damage
        performed.Invoke();
        foreach (Collider other in triggerColliders)
        {
            if (other != null)
            {
                if (other.GetComponent<I_DamageAble>() != null)
                {
                    other.GetComponent<I_DamageAble>().TakeDamage(damage, other.ClosestPoint(transform.position), other.transform.position + transform.position);
                }
            }
        }


        yield return new WaitForSeconds(cooldown);
        inUse = false;
    }
}

