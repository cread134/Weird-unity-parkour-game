using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private float damage;
    public float damageRadius = 0.1f;
    public float lifeTime = 3f;
    public LayerMask collisionMask;

    public float speedMultiplier = 10f;

    private Vector3 lastPosition;
    // Update is called once per frame
    void Update()
    {
        if (!spawned) return;

        //move
        transform.position = transform.position + (direction * speed * speedMultiplier * Time.deltaTime);

        //check col
        Vector3 rayDir = transform.position - lastPosition;
        float rayDistance = Vector3.Distance(transform.position, lastPosition);

        RaycastHit hit;
        if (Physics.SphereCast(lastPosition, damageRadius,rayDir, out hit, rayDistance + 0.1f, collisionMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.CompareTag("Player"))
            {
                HitTarget(hit.point, hit.transform.gameObject);
            }
            else
            {
                NullHit();
            }

        }
        else
        {
            if (Physics.Raycast(lastPosition, rayDir, rayDistance + 0.1f, 10))
            {
                 NullHit();
            }
        }
    }
    Vector3 direction;

    private bool spawned = false;
    private float speed;
    public void SpawnProjectile(float input_speed, float input_Damage, Vector3 input_direction)
    {
        damage = input_Damage;
        spawned = true;
        speed = input_speed;
        direction = -input_direction;
        Destroy(gameObject, lifeTime);
    }

    void NullHit()
    {
        Debug.Log("Null hit");
        Destroy(gameObject);
    }

    void HitTarget(Vector3 point,GameObject target)
    {
        target.GetComponent<I_DamageAble>().TakeDamage(damage, point, point - transform.position);
        Debug.Log("hit player");
        Destroy(gameObject);
    }
}
