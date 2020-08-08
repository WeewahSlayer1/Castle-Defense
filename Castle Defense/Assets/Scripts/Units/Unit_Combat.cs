using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// If frontLineVars.frontMost.unitState == unavailable, speed = 0;

public static class Unit_Combat
{
    const float timeBetweenUpdates          = 1.0f;
    public const float humanJoggingSpeed    = 3.5f;
    public const float humanWalkingSpeed    = 2.0f;

    //=============  Struct UnitCombatVars  ====================================//
    [System.Serializable]
    public struct UnitCombatVars
    {
        public Unit_Combat_Types.Type   type;

        [System.NonSerialized]
        public Unit                     enemyTargetedUnit;
        [System.NonSerialized]
        public Unit                     enemyTargetingUs;

        public float                    rangeWeapon;
        public float                    rangeScanning;

        public UnitCombatAudio          combatAudio;

        [System.NonSerialized]
        public Unit_Squad               squad;
    
        [System.NonSerialized]
        public float                    timeTillNextUpdate;

        public SkinnedMeshRenderer      projectileRenderer;
        public Mesh                     projectileMesh;
        public Transform                projectileBone;

        public FrontLineVars            frontLineVars;
    }

    //=============  Update_Combat()  ===========================================================================================================//
    public static void Update_Combat(Unit u)
    {
        switch (u.currentState) {
            //--------------------------  Case - AVAILABLE  ------------------------------------//
            case Unit.UnitState.available:
                Update_Available(u);
                break;

            //--------------------------  Case - UNAVAILABLE  ------------------------------------//
            case Unit.UnitState.unavailable:
                Update_Unavailable(u);
                break;

            //--------------------------  Case - SPAWNING  ------------------------------------//
            case Unit.UnitState.spawning:
                Update_Spawning(u);
                break;

            //--------------------------  Case - RETREATING  ------------------------------------//
            case Unit.UnitState.retreating:
                Update_Retreating(u);
                break;
        }

        //--------------------------  Update countdown  ------------------------------------//
        if (u.combatUnitVars.timeTillNextUpdate <= 0) u.combatUnitVars.timeTillNextUpdate = timeBetweenUpdates;
        u.combatUnitVars.timeTillNextUpdate -= Time.deltaTime;
    }

    //============= Function - Update_Available()  =============================================================================//
    static void Update_Available(Unit u)
    {
        //--------------  Acquiring enemies, attacking, etc  ----------------------------------------------------------------//
        switch (u.combatUnitVars.type)
        {
            case Unit_Combat_Types.Type.pikeman:
                Update_Available_Pikeman(u);
                break;

            case Unit_Combat_Types.Type.shield:
                Update_Available_Pikeman(u);
                break;

            case Unit_Combat_Types.Type.archer:
                //------------------------  Every SECOND  ----------------------------//    Update nearestEnemy, break formation if within range
                if (u.combatUnitVars.timeTillNextUpdate <= 0)
                {
                    u.combatUnitVars.enemyTargetedUnit = NearestEnemyBot(u, false);

                    float distance2enemy = float.MaxValue;

                    if (u.combatUnitVars.enemyTargetedUnit != null)
                    {
                        distance2enemy = Vector3.Distance(u.transform.position, u.combatUnitVars.enemyTargetedUnit.transform.position);

                        if (u.combatUnitVars.frontLineVars.inFront == null || distance2enemy > Unit_Combat_Types.archerMinimumRange) {
                            if (distance2enemy < u.combatUnitVars.rangeWeapon)
                                Attack(u);
                        }
                        else {
                            u.AssignObjective(u.transform.position - u.combatUnitVars.squad.squadTransform.forward * (Unit_Combat_Types.archerMinimumRange + 10));
                            u.currentState = Unit.UnitState.retreating;
                            u.navMeshAgent.speed = humanJoggingSpeed;
                        }
                    }
                }
                break;
        }
    }

    //============= Function - Update_Available_Pikeman()  ===============================//
    static void Update_Available_Pikeman(Unit u)
    {
        if (u.combatUnitVars.frontLineVars.inFront == null)
        {
            //------------------------  EVERY frame  -----------------------------//    Update distance, if we are in range, ATTACK
            float distance2enemy = float.MaxValue;

            if (u.combatUnitVars.enemyTargetedUnit != null)
            {
                if (u.combatUnitVars.enemyTargetedUnit.currentState == Unit.UnitState.dying)
                    u.navMeshAgent.SetDestination(u.destination);
                else
                {
                    distance2enemy = Vector3.Distance(u.transform.position, u.combatUnitVars.enemyTargetedUnit.transform.position);

                    if (distance2enemy < u.combatUnitVars.rangeWeapon)
                        Attack(u);
                }
            }

            //------------------------  Every SECOND  ----------------------------//    Update nearestEnemy, break formation if within range
            if (u.combatUnitVars.timeTillNextUpdate <= 0) {
                u.combatUnitVars.enemyTargetedUnit = NearestEnemyBot(u, true);

                if (u.combatUnitVars.enemyTargetedUnit != null) {
                    distance2enemy = Vector3.Distance(u.transform.position, u.combatUnitVars.enemyTargetedUnit.transform.position);
                    if (u.combatUnitVars.enemyTargetedUnit.currentState != Unit.UnitState.dying)
                        if (distance2enemy > u.combatUnitVars.rangeWeapon) {
                            if (distance2enemy < Unit_Combat_Types.breakFormationRange || !u.combatUnitVars.frontLineVars.inFormation)
                                u.navMeshAgent.SetDestination(u.combatUnitVars.enemyTargetedUnit.transform.position);
                        }
                        else
                            Attack(u);
                }
            }
        }
        else if (u.combatUnitVars.frontLineVars.queued)
            if (u.combatUnitVars.timeTillNextUpdate <= 0)
            {
                bool farEnough = Vector3.Distance(u.transform.position, u.combatUnitVars.frontLineVars.inFront.transform.position) > 0.5f;
                bool weAreBehind = Vector3.Angle(u.transform.forward, u.combatUnitVars.frontLineVars.inFront.transform.position - u.transform.position) < 90;

                if (farEnough && weAreBehind)
                    u.navMeshAgent.speed = humanJoggingSpeed;
                else
                    u.navMeshAgent.speed = 0;
            }
    }

    //============= Function - Update_Unavailable()  ===============================//
    static void Update_Unavailable(Unit u)
    {
        u.lerpData.actionTimer += Time.deltaTime;
        float t = Mathf.Clamp(u.lerpData.actionTimer / u.lerpData.actionDuration, 0, 1);

        u.transform.position = Vector3.Lerp(u.lerpData.originalPos, u.lerpData.targetPos, t);
        u.transform.LookAt(Vector3.Lerp(u.lerpData.originalLookAt, u.lerpData.targetLookAt, t));
    }

    //============= Function - Update_Spawning()  ===============================//
    static void Update_Spawning(Unit u)
    {
        if (u.wayPoints.Count > 0)
        {
            if (Vector3.Distance(u.transform.position, u.navMeshAgent.destination) < 0.25f)
            {
                u.wayPoints.Remove(u.wayPoints[0]);

                if (u.wayPoints.Count > 0)
                    u.navMeshAgent.SetDestination(u.wayPoints[0]);
            }
        }
        else
            u.currentState = Unit.UnitState.available;
    }

    //============= Function - Update_Retreating()  ===============================//
    static void Update_Retreating(Unit u)
    {
        if (u.navMeshAgent.remainingDistance < 0.5f) {
            u.currentState = Unit.UnitState.available;
            Update_Available(u);
        }
    }

    //============= Function - Attack()  ===============================//
    static void Attack(Unit u)
    {
        u.navMeshAgent.speed = 0;
        u.currentState = Unit.UnitState.unavailable;
        u.audioSrc.Stop();

        Unit behind = u.combatUnitVars.frontLineVars.behind;
        while (behind != null) {
            if (behind.currentState != Unit.UnitState.retreating) {
                behind.combatUnitVars.frontLineVars.queued = true;
                behind.navMeshAgent.speed = 0;
            }
            behind = behind.combatUnitVars.frontLineVars.behind;
        }

        switch (u.combatUnitVars.type) {
            case Unit_Combat_Types.Type.pikeman:
                u.combatUnitVars.frontLineVars.inFormation = false;
                u.StartCoroutine(Unit_Combat_Types.CoRoutine_Attack_Pike(u));
                break;

            case Unit_Combat_Types.Type.shield:
                u.combatUnitVars.frontLineVars.inFormation = false;
                u.StartCoroutine(Unit_Combat_Types.CoRoutine_Attack_Pike(u));
                break;

            case Unit_Combat_Types.Type.archer:
                u.StartCoroutine(Unit_Combat_Types.CoRoutine_Attack_Archer(u));
                break;

            default:
                break;
        }
    }

    //============= Function - GetEnemyUnit()  ===============================//
    public static Unit NearestEnemyBot(Unit u, bool allowDoubleTeaming)
    {
        Unit closestEnemy = null;
        float shortestDistance = float.MaxValue;

        float range = u.combatUnitVars.rangeScanning;

        Unit[] possibleUnits = Object.FindObjectsOfType<Unit>();
        
        for (int i = 0; i < possibleUnits.Length; i++)
            if (possibleUnits[i].type == Unit.UnitType.SOLDIER && possibleUnits[i].combatUnitVars.squad.team != u.combatUnitVars.squad.team && possibleUnits[i].currentState != Unit.UnitState.dying) {
                //-------------------  If one of our allies has already called dibs  --------------------//
                bool alreadyAcquired = false;

                if (!allowDoubleTeaming)
                    if (possibleUnits[i].combatUnitVars.enemyTargetingUs != null)
                        if (!(u.combatUnitVars.type == Unit_Combat_Types.Type.archer && possibleUnits[i].combatUnitVars.enemyTargetingUs.combatUnitVars.type != Unit_Combat_Types.Type.archer))
                        {
                            float ourDistance = Vector3.Distance(u.transform.position, possibleUnits[i].transform.position);
                            float competingDistance = Vector3.Distance(possibleUnits[i].combatUnitVars.enemyTargetingUs.transform.position, possibleUnits[i].transform.position);

                            if (ourDistance > competingDistance)
                                alreadyAcquired = true;
                        }

                if (!alreadyAcquired) {
                    //-------------------  If this enemy is nearer than our previous choice  --------------------//
                    float distance = Vector3.Distance(u.transform.position, possibleUnits[i].transform.position);

                    if (distance < shortestDistance && distance < range) {
                        shortestDistance = distance;
                        closestEnemy = possibleUnits[i];
                    }
                }
            }

        if (closestEnemy != null) {
            if (closestEnemy.combatUnitVars.enemyTargetingUs != null)
                closestEnemy.combatUnitVars.enemyTargetingUs.combatUnitVars.enemyTargetedUnit = null;

            closestEnemy.combatUnitVars.enemyTargetingUs = u;
        }

        return closestEnemy;
    }

    //============= Struct - FrontLineVars  ===============================//
    public struct FrontLineVars
    {
        public Unit behind;
        public Unit inFront;
        public bool queued;
        public bool inFormation;
    }
}