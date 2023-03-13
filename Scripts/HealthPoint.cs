using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthPoint : MonoBehaviour
{
    public GameObject healthPickup;


    public void SetInitialValues( ConnectionManager fromConnection, bool doDamage, GameObject target)
    {
        GameObject pickupInstance = Instantiate(healthPickup, transform.position, transform.rotation);
        pickupInstance.GetComponent<HealthPickup>().Initialise(target, doDamage);
        Destroy(gameObject);
    }
}
