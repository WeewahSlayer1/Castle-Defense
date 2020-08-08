using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit_Wagon : MonoBehaviour
{
    //==============  Struct - UnitWagonVars  =================================================//
    [System.Serializable]
    public struct UnitWagonVars
    {
        public Unit.Team team;

        public UnitWagonAudio wagonAudio;

        [System.NonSerialized]
        public LineRenderer lineRenderer;

        [System.NonSerialized]
        public Vector3 initialHeading;

        [System.NonSerialized]
        public Vector3 destinationHeading;

        public ParticleSystem[] particles;

        public WagonType wagonType;

        public WagonMeshes wagonMeshes;

        public SkinnedMeshRenderer wagonMeshRenderer;

        public Transform horseHinge;
    }

    //==============  Struct - WagonMeshes  =================================================//
    [System.Serializable]
    public struct WagonMeshes
    {
        public Mesh[] hayCart;
        public Mesh[] woodCart;
        public Mesh[] stoneCart;
    }

    //==============  enum - WagonType  =================================================//
    public enum WagonType {hay, wood, stone};

    //==============  Function - Update_Wagon()  =================================================//
    public static void Update_Wagon(Unit u)
    {
        if (u.wayPoints.Count > 0)
        {
            if (Vector3.Distance(u.transform.position, u.navMeshAgent.destination) < 0.25f)
            {
                u.navMeshAgent.SetDestination(u.wayPoints[0]);
                u.wayPoints.Remove(u.wayPoints[0]);

                int indexOffset = 0;
                if (u.wayPoints.Count > 10) indexOffset = 10;
                else if (u.wayPoints.Count > 1) indexOffset = u.wayPoints.Count - 1;
                else if (u.wayPoints.Count == 0)
                    return;

                float t = Mathf.Lerp(Time.deltaTime, Time.deltaTime * 5, 1 - indexOffset / 10);

                Vector3 point = u.wayPoints[indexOffset] + Vector3.up * 0.75f;

                Vector3 relativePos = point - u.wagonUnitVars.horseHinge.position;
                Quaternion toRotation = Quaternion.LookRotation(relativePos);
                u.wagonUnitVars.horseHinge.rotation = Quaternion.Lerp(u.wagonUnitVars.horseHinge.rotation, toRotation, t);
            }
        }
        else
            u.currentState = Unit.UnitState.available;
    }
}