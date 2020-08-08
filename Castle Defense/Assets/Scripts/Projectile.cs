using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    //=============================  Variables  =====================================================//
    public Rigidbody rb;
    public bool arcing;
    float speed;
    LineRenderer line;

    float timeTillNextUpdate;
    const float timeBetweenUpdates = 0.2f;

    public Transform    target;
    public float        timeToTarget;

    //=============================  Function - Initialise()  =======================================//
    public void Initialise()
    {
        gameObject.layer = LayerMask.NameToLayer("Projectile");
        rb = gameObject.GetComponent<Rigidbody>();
        arcing = true;

        line = gameObject.AddComponent<LineRenderer>();
        line.widthMultiplier = 0.1f;
        line.material = FindObjectOfType<World_GenericVars>().materials.lineAirTrail;
        line.positionCount = 10;
        for (int i = 0; i < line.positionCount; i++)
            line.SetPosition(i, this.transform.position);
    }

    //=============================  Function - Update()  ===========================================//
    void Update()
    {
        timeTillNextUpdate -= Time.deltaTime;

        transform.LookAt(this.transform.position + rb.velocity);
        
        if (timeTillNextUpdate <= 0) {
            for (int i = 0; i < line.positionCount - 1; i++)
                line.SetPosition(i, line.GetPosition(i + 1));

            line.SetPosition(line.positionCount - 1, this.transform.position);
            timeTillNextUpdate = timeBetweenUpdates;
        }

        if (target != null)
        {
            timeToTarget -= Time.deltaTime;

            if (timeToTarget <= 0)
                HitTarget(target.gameObject, 0.25f);
        }
    }

    //=============================  Function - CalculateLaunchSpeed()  =============================//
    public static float CalculateLaunchSpeed(Vector3 dir, Vector3 initialPos, Vector3 targetPos, Vector3 targetVelocity, bool adjustForMovement)
    {
        float deltaX = Vector3.Distance(new Vector3(initialPos.x, 0, initialPos.z), new Vector3(targetPos.x, 0, targetPos.z));
        float deltaY = targetPos.y - initialPos.y;
        float cosTheta = new Vector3(dir.x, 0, dir.z).magnitude / dir.magnitude;
        float sinTheta = dir.y / dir.magnitude;
        
        float sqrt = 2 * (deltaY - ((sinTheta * deltaX) / cosTheta)) / -Physics.gravity.magnitude;
        sqrt *= Mathf.Sign(sqrt);
        float oldSpeed = deltaX / (cosTheta * Mathf.Sqrt(sqrt));

        float timeToTarget = (dir.y * oldSpeed + Mathf.Sqrt(Mathf.Pow(dir.y * oldSpeed, 2) - 2 * Physics.gravity.magnitude * deltaY)) / Physics.gravity.magnitude;

        Vector3 predictedPos = targetPos + targetVelocity * timeToTarget;

        if (Vector3.Angle(targetPos - initialPos, predictedPos - initialPos) < 90)
        {
            deltaX = Vector3.Distance(new Vector3(initialPos.x, 0, initialPos.z), new Vector3(predictedPos.x, 0, predictedPos.z));

            sqrt = 2 * (deltaY - ((sinTheta * deltaX) / cosTheta)) / -Physics.gravity.magnitude;
            sqrt *= Mathf.Sign(sqrt);
            float newSpeed = deltaX / (cosTheta * Mathf.Sqrt(sqrt));

            float final = Mathf.Lerp(oldSpeed, newSpeed, 0.9f);
            if (float.IsNaN(final))
                Debug.Log(
                    "dir " + dir +
                    "oldspeed: " + oldSpeed +
                    "newSpeed: " + newSpeed
                    );

            return final;
        }
        else
            return oldSpeed;
    }

    //=============================  OnCollisionEnter()  ============================================//
    private void OnCollisionEnter (Collision collision)
    {
        HitTarget(collision.gameObject, 5f);
    }

    //=============================  Function - HitTarget()  ========================================//
    void HitTarget(GameObject obj, float forceMultiplier)
    {
        arcing = false;

        if (obj.GetComponent<Rigidbody>() != null) {
            transform.parent = obj.transform;
            this.name = "Arrow: direct";
            transform.position = VectorMath.Vec2closestPoint(transform.position - rb.velocity, rb.velocity, obj.transform.position);
            //Debug.Log("Distance: " + Vector3.Distance(transform.position, obj.transform.position));
            Unit u = obj.GetComponentInParent<Unit>();

            if (u != null && u.currentState != Unit.UnitState.dying) {
                if (obj.tag != "Shield")
                    u.StartCoroutine(Unit_Combat_Types.CoRoutine_Die_Archer(u, this.transform.parent));
            }

            obj.GetComponent<Rigidbody>().velocity = rb.velocity * forceMultiplier;
        }

        Destroy(line);

        Destroy(rb);

        Destroy(this);
    }

}