using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthPickup : MonoBehaviour
{


    void Update()
    {
        if (initialised)
        {
            Vector3 targPos = goTarget.transform.position;
            targPos.y += 1.5f;
            transform.position = Vector3.MoveTowards(transform.position, targPos, speed * Time.deltaTime);

            float distance = Vector3.Distance(transform.position, targPos);
            if(distance <= 0.5f)
            {
                Arrived();
            }
        }
    }
    public float healthAmount;
    public float speed;
    private bool doTheDamage;
    private GameObject goTarget;
    private bool initialised = false;
    public void Initialise(GameObject target, bool doDamage)
    {
        doTheDamage = doDamage;
        goTarget = target;
        initialised = true;
    }

    void Arrived()
    {

        initialised = false;

        if (doTheDamage)
        {
            goTarget.GetComponent<I_DamageAble>().TakeDamage(healthAmount * 3f, transform.position, goTarget.transform.position - transform.position);
        }
        else
        {
            PlayerHealthManager hManage = goTarget.GetComponent<PlayerHealthManager>();
            hManage.AddHealth(healthAmount);
        }
        Destroy(gameObject);
    }
}
