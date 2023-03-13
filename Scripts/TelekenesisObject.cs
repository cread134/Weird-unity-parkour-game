using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TelekenesisObject : MonoBehaviour, I_Knockback
{

    private Rigidbody rb;

    public UnityEvent onCollision;

    public float velocityThreshold = 1.5f;

    public float collisionInbetweenTime = 0.5f;

    private float lastCollision;

    public bool singleCollisionOnly = true;

    private bool collided = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if(onCollision == null)
        {
            onCollision = new UnityEvent();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SendTowardsTarget(float force, Vector3 target)
    {
        Vector3 between = (target - transform.position).normalized;
        rb.AddForce(between * force, ForceMode.Impulse);
    }

    public void KnockBack(float amount, Vector3 point, Vector3 direction)
    {
        rb.AddForce(direction.normalized * amount, ForceMode.Impulse);
    }

    public void OnCollisionEnter(Collision collision)
    {
        if(rb.velocity.magnitude > velocityThreshold && Time.time > lastCollision)
        {
            if (singleCollisionOnly && !collided)
            {
                Collided();
            }
            else
            {
                Collided();
            }
        }
    }

    void Collided()
    {
        Debug.Log("Ob col " + gameObject);
        collided = true;
        onCollision.Invoke();
        lastCollision = Time.time + collisionInbetweenTime;
    }
}
