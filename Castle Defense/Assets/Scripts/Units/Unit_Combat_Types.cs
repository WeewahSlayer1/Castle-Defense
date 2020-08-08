using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Unit_Combat_Types
{
    public const float archerMinimumRange   = 30.0f;

    public const float pikemanSpacing       = 2.0f;
    public const float archerSpacing        = 2.0f;
    public const float shieldSpacing        = 1.33f;
    
    public const float breakFormationRange  = 5.0f;

    //=============  Enum - Type  ==========================//
    public enum Type { pikeman, archer, shield}

    //=============  CoRoutine - CoRoutine_Attack_Pike()  ==========================//
    public static IEnumerator CoRoutine_Attack_Pike(Unit u)
    {
        for (int i = 0; i < u.anims.Length; i++) {
            u.anims[0].SetTrigger("Attack - Standard");
            u.anims[0].SetFloat("Speed", 0);
            u.anims[0].SetBool("Available", false);
        }

        u.lerpData.originalPos = u.transform.position;
        u.lerpData.originalLookAt = u.transform.position + u.transform.forward * 3;
        u.lerpData.targetPos = u.combatUnitVars.enemyTargetedUnit.transform.position + (u.transform.position - u.combatUnitVars.enemyTargetedUnit.transform.position) * (3 / (u.combatUnitVars.enemyTargetedUnit.transform.position - u.transform.position).magnitude);

        Quaternion savedRot = u.combatUnitVars.enemyTargetedUnit.transform.rotation;
        u.combatUnitVars.enemyTargetedUnit.transform.LookAt(u.transform.position);
        u.lerpData.targetLookAt = u.combatUnitVars.enemyTargetedUnit.transform.position + u.combatUnitVars.enemyTargetedUnit.transform.right * 0.3f;
        u.combatUnitVars.enemyTargetedUnit.transform.rotation = savedRot;

        u.lerpData.actionDuration = 0.15f;
        u.lerpData.actionTimer = 0;

        u.combatUnitVars.enemyTargetedUnit.StartCoroutine(Unit_Combat_Types.CoRoutine_Die_Pike(u.transform.position, u.combatUnitVars.enemyTargetedUnit));
        u.combatUnitVars.enemyTargetedUnit = null;

        //------------  WaitForSeconds(0.1)  ---------------//
        yield return new WaitForSeconds(0.1f);
        if (u.currentState != Unit.UnitState.dying) {
            u.audioSrc.loop = false;
            u.audioSrc.clip = u.combatUnitVars.combatAudio.audioClips.clip_Vocal_Attack;
            u.audioSrc.Play();

            u.transform.position = u.lerpData.targetPos;
            u.transform.LookAt(u.lerpData.targetLookAt);
        }

        //------------  WaitForSeconds(1.25)  ---------------//
        yield return new WaitForSeconds(1.0f);
        if (u.currentState != Unit.UnitState.dying)
            ReEnableUnit(u);
    }

    //=============  CoRoutine - CoRoutine_Attack_Archer()  ==========================//
    public static IEnumerator CoRoutine_Attack_Archer(Unit u)
    {
        // yield return new WaitForSeconds(Random.Range(0, 0.3f));

        Unit enemyUnit = u.combatUnitVars.enemyTargetedUnit;

        float distance = Vector3.Distance(enemyUnit.transform.position, u.transform.position);
        
        for (int i = 0; i < u.anims.Length; i++) {
            u.anims[0].SetFloat("Speed", 0);
            if (distance < archerMinimumRange)
                u.anims[0].SetTrigger("Attack - #2");
            else
                u.anims[0].SetTrigger("Attack - Standard");
        }

        u.lerpData.originalLookAt = u.transform.position + u.transform.forward * u.combatUnitVars.rangeWeapon;

        u.lerpData.originalPos = u.transform.position;

        if (distance < 2)
            u.lerpData.targetPos = u.transform.position + (u.transform.position - enemyUnit.transform.position).normalized * -0.5f;
        else
            u.lerpData.targetPos = u.transform.position;

        Quaternion savedRot = enemyUnit.transform.rotation;
        enemyUnit.transform.LookAt(u.transform.position);
        u.lerpData.targetLookAt = enemyUnit.transform.position + enemyUnit.transform.right * 0.3f;
        enemyUnit.transform.rotation = savedRot;

        u.lerpData.actionDuration = 0.15f;
        u.lerpData.actionTimer = 0;

        //------------  WaitForSeconds()  ---------------//
        yield return new WaitForSeconds(1.0f);

        GameObject arrow = new GameObject();

        if (u.currentState != Unit.UnitState.dying && enemyUnit != null)
        {
            u.audioSrc.loop = false;
            u.audioSrc.clip = u.combatUnitVars.combatAudio.audioClips.clip_Vocal_Attack;
            u.audioSrc.Play();

            u.transform.LookAt(u.lerpData.targetLookAt);

            u.combatUnitVars.projectileRenderer.enabled = false;

            arrow.transform.position = u.combatUnitVars.projectileBone.position;
            arrow.transform.LookAt(u.combatUnitVars.projectileBone.position + u.combatUnitVars.projectileBone.up);

            arrow.AddComponent<MeshFilter>().mesh = u.combatUnitVars.projectileMesh;
            arrow.AddComponent<MeshRenderer>().sharedMaterial = u.combatUnitVars.projectileRenderer.sharedMaterial;
            arrow.name = "Arrow";

            Rigidbody rb = arrow.AddComponent<Rigidbody>();
            
            Projectile proj = arrow.AddComponent<Projectile>();
            proj.Initialise();
            arrow.transform.parent = GameObject.Find("Hierarchy - Scrap").transform;

            if (distance > archerMinimumRange)
                rb.velocity = arrow.transform.forward * Projectile.CalculateLaunchSpeed(arrow.transform.forward, arrow.transform.position, enemyUnit.transform.position + Vector3.up * 1.2f, enemyUnit.navMeshAgent.velocity, true);
            else
            {
                Collider[] colliders = enemyUnit.GetComponentsInChildren<Collider>();
                Transform chestpiece = null;
                Transform shield = null;
                for (int i = 0; i < colliders.Length; i++) {
                    if (colliders[i].gameObject.tag == "ChestPiece")
                        chestpiece = colliders[i].transform;
                    if (colliders[i].gameObject.tag == "Shield")
                        shield = colliders[i].transform;
                }

                if (shield != null && Vector3.Angle(enemyUnit.transform.forward, arrow.transform.forward) > 90) {
                    proj.target = shield;
                    rb.velocity = (shield.transform.position - arrow.transform.position).normalized * 85f;
                }
                else {
                    rb.velocity = (chestpiece.transform.position - arrow.transform.position).normalized * 85f;
                    proj.target = chestpiece;
                }
                
                proj.timeToTarget = Vector3.Distance(arrow.transform.position, proj.target.position) / rb.velocity.magnitude;
            }


            //------------  WaitForSeconds()  ---------------//
            yield return new WaitForSeconds(0.5f);

            if (arrow != null) {
                arrow.AddComponent<BoxCollider>().size = new Vector3(0.1f, 0.1f, 0.8f);
                arrow.GetComponent<BoxCollider>().center = new Vector3(0, 0, -0.275f);
            }
            else
                Debug.Log("GameObject 'arrow' has been destroyed for some reason");

            if (enemyUnit != null) {
                Rigidbody[] rbArr = enemyUnit.GetComponentsInChildren<Rigidbody>();
                for (int i = 0; i < rbArr.Length; i++)

                if (u.currentState != Unit.UnitState.dying)
                    u.combatUnitVars.projectileRenderer.enabled = true;
            }
        }
        else {
            Debug.Log("Cancelling archery");
            Object.Destroy(arrow);
        }

        //------------  WaitForSeconds()  ---------------//
        yield return new WaitForSeconds(1.0f);
        if (u.currentState != Unit.UnitState.dying)
            ReEnableUnit(u);
    }

    //=============  Function - ReEnableUnit()  ==========================//
    static void ReEnableUnit(Unit u)
    {
        u.currentState = Unit.UnitState.available;
        u.navMeshAgent.speed = Unit_Combat.humanJoggingSpeed;
        u.combatUnitVars.enemyTargetedUnit = Unit_Combat.NearestEnemyBot(u, u.combatUnitVars.frontLineVars.inFront == null);

        //-----------  Allow all units behind us to move again  ------------//
        Unit behind = u.combatUnitVars.frontLineVars.behind;
        while (behind != null) {
            behind.combatUnitVars.frontLineVars.queued = false;
            behind.navMeshAgent.speed = Unit_Combat.humanJoggingSpeed;
            behind = behind.combatUnitVars.frontLineVars.behind;
        }
    }
    
    //=============  Coroutine - CoRoutine_Die_Pike()  =============================//
    public static IEnumerator CoRoutine_Die_Pike(Vector3 attackerPos, Unit u)
    {
        u.currentState = Unit.UnitState.dying;
        u.navMeshAgent.enabled = false;
        u.transform.LookAt(attackerPos);

        if (u.type == Unit.UnitType.SOLDIER)
            u.combatUnitVars.squad.unitList.Remove(u);

        for (int i = 0; i < u.anims.Length; i++) {
            u.anims[i].SetFloat("Speed", 0);
            u.anims[i].SetTrigger("Death - Standard");
        }

        u.audioSrc.loop = false;
        u.audioSrc.clip = u.combatUnitVars.combatAudio.audioClips.clip_Vocal_Die;
        u.audioSrc.Play();

        yield return new WaitForSeconds(0.5f);
        Object.Instantiate(u.humanUnitVars.deathFX, u.transform.position + Vector3.up * 1.25f, u.transform.rotation, null).transform.parent = u.transform;

        yield return new WaitForSeconds(0.5f);
        ActuallyDie(u);

        yield return new WaitForSeconds(3.0f);
        Unit_Human.DeathCleanUp(u);
    }
    
    //=============  Coroutine - CoRoutine_Die_Archer()  =============================//
    public static IEnumerator CoRoutine_Die_Archer(Unit u, Transform fxParent)
    {
        u.currentState = Unit.UnitState.dying;

        u.navMeshAgent.enabled = false;

        if (u.type == Unit.UnitType.SOLDIER)
            u.combatUnitVars.squad.unitList.Remove(u);
        
        u.audioSrc.loop = false;
        u.audioSrc.clip = u.combatUnitVars.combatAudio.audioClips.clip_Vocal_Die;
        u.audioSrc.spatialBlend = 0;
        u.audioSrc.Play();

        Object.Instantiate(u.humanUnitVars.deathFX, fxParent.position, fxParent.rotation).transform.parent = fxParent;
        ActuallyDie(u);

        yield return new WaitForSeconds(3.0f);
        Unit_Human.DeathCleanUp(u);
    }
    
    //=============  Function - ActuallyDie()  =============================//
    static void ActuallyDie(Unit u)
    {
        Unit_Human.EnableDisableRagdoll(u, true);

        if (u.combatUnitVars.frontLineVars.behind != null) {
            u.combatUnitVars.frontLineVars.behind.combatUnitVars.frontLineVars.inFront = null;
            u.combatUnitVars.frontLineVars.behind.anims[0].SetBool("FrontLine", true);
        }
    }
}
