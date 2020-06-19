using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AudioSource), typeof(NavMeshAgent))]
public class Unit : MonoBehaviour
{
    AudioSource                     audioSrc;
    [SerializeField]    AudioClip   clip_GUI_Selected;
    [SerializeField]    AudioClip   clip_GUI_Attack;
    [SerializeField]    AudioClip   clip_Running;
    [SerializeField]    AudioClip   clip_Vocal_Attack;
    public              AudioClip   clip_Vocal_Die;
    
    NavMeshAgent                    navMeshAgent;
    Animator                        anim;

    public UnitState                currentState;
    public PositionLerpData         lerpData;

    Unit                            enemyUnit = null;

    //=============  Start()  ======================================//
    private void Start()
    {
        navMeshAgent    = this.GetComponent<NavMeshAgent>();
        audioSrc        = this.GetComponent<AudioSource>();
        anim            = this.GetComponent<Animator>();
        currentState    = UnitState.notAttacking;
    }

    //=============  Update()  =====================================//
    private void Update()
    {
        if (currentState != UnitState.dying)
        {
            //------------------------------  Attacking  ----------------------------------------//
            if (enemyUnit != null)
            {
                if (currentState == UnitState.attacking)
                {
                    lerpData.actionTimer += Time.deltaTime;
                    float t = Mathf.Clamp(lerpData.actionTimer / lerpData.actionDuration, 0, 1);

                    transform.position = Vector3.Lerp(lerpData.originalPos, lerpData.targetPos, t);
                    transform.LookAt(Vector3.Lerp(lerpData.originalLookAt, lerpData.targetLookAt, t));
                }
                else
                {
                    if (Vector3.Distance(this.transform.position, enemyUnit.transform.position) > 3.5)
                        navMeshAgent.SetDestination(enemyUnit.transform.position);
                    else if (currentState == UnitState.notAttacking)
                        StartCoroutine(CoRoutine_Attack());
                }
            }

            //------------------------------  Movement  ----------------------------------------//
            if (navMeshAgent.velocity.magnitude > 0)
            {
                anim.SetFloat("Speed", navMeshAgent.velocity.magnitude);
                if (!audioSrc.isPlaying)
                {
                    audioSrc.clip = clip_Running;
                    audioSrc.loop = true;
                    audioSrc.Play();
                }
            }
            else
            {
                anim.SetFloat("Speed", 0);
                if (audioSrc.clip == clip_Running)
                {
                    audioSrc.Stop();
                    audioSrc.loop = true;
                }
            }
        }
    }

    //============ Attack CoRoutine  ===============================//
    IEnumerator CoRoutine_Attack()
    {
        currentState = UnitState.attacking;
        navMeshAgent.enabled = false;
        audioSrc.Stop();

        anim.SetTrigger("Attack - Standard");
        anim.SetFloat("Speed", 0);

        lerpData.originalPos = this.transform.position;
        lerpData.originalLookAt = this.transform.position + this.transform.forward * 3;
        lerpData.targetPos = enemyUnit.transform.position + (transform.position - enemyUnit.transform.position) * (3 / (enemyUnit.transform.position - transform.position).magnitude);
        lerpData.targetLookAt = enemyUnit.transform.position + enemyUnit.transform.forward * -0.2f;
        lerpData.actionDuration = 0.15f;
        lerpData.actionTimer = 0;

        enemyUnit.StartCoroutine(enemyUnit.CoRoutine_Die(this.transform.position));

        //------------  WaitForSeconds(0.1)  ---------------//
        yield return new WaitForSeconds(0.1f);
        audioSrc.loop = false;
        audioSrc.clip = clip_Vocal_Attack;
        audioSrc.Play();
        
        enemyUnit = null;

        transform.position = lerpData.targetPos;
        transform.LookAt(lerpData.targetLookAt);

        //------------  WaitForSeconds(1.25)  ---------------//
        yield return new WaitForSeconds(1.25f);
        navMeshAgent.enabled = true;
        currentState = UnitState.notAttacking;
    }

    //============ Die CoRoutine  ===============================//
    IEnumerator CoRoutine_Die(Vector3 attackerPos)
    {
        transform.LookAt(attackerPos);
        currentState = UnitState.dying;
        navMeshAgent.enabled = false;
        anim.SetFloat("Speed", 0);

        audioSrc.clip = clip_Vocal_Die;
        audioSrc.Play();

        anim.SetTrigger("Death - Standard");

        yield return new WaitForSeconds(0.1f);
    }

    //=============  Select()  ====================================//
    public void Select()
    {
        audioSrc.clip = clip_GUI_Selected;
        audioSrc.Play();
    }

    //=============  AssignObjective()  ===========================//
    public void AssignObjective(Vector3 target)
    {
        navMeshAgent.SetDestination(target);
    }

    //=============  AssignEnemy()  ==============================//
    public void AssignEnemy(Unit newEnemy)
    {
        enemyUnit = newEnemy;
    }

    public enum UnitState {notAttacking, attacking, dying}

    [System.Serializable]
    public struct PositionLerpData
    {
        public Vector3  originalPos;
        public Vector3  originalLookAt;

        public Vector3  targetPos;
        public Vector3  targetLookAt;

        public float    actionTimer;
        public float    actionDuration;
    }
}
