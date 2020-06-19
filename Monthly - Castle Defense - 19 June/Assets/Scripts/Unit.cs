using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AudioSource))]
public class Unit : MonoBehaviour
{
    //=============  Enums  ======================================//
    public enum UnitState { spawning, notAttacking, attacking, dying }
    public enum Team { none, human, orc }

    //=============  Variables  ======================================//
    AudioSource audioSrc;
    public UnitSettings unitSettings;
    public Unit_Squad squad;

    [System.NonSerialized]
    public NavMeshAgent navMeshAgent;
    Animator anim;

    [NonSerialized]
    public UnitState currentState;
    PositionLerpData lerpData;

    [NonSerialized]
    public Unit enemyUnit = null;

    public float formationRandom;
    public float timingRandom;

    public List<Vector3> wayPoints;
    public int wayPointIndex;

    GameObject selectionCircle;

    public GameObject protoUnit;

    //=============  Initialise()  ============================================//
    public void Initialise(Unit_Squad _squad, string name)
    {
        navMeshAgent = this.GetComponent<NavMeshAgent>();
        if (navMeshAgent == null)
            navMeshAgent = this.gameObject.AddComponent<NavMeshAgent>();
        audioSrc = this.GetComponent<AudioSource>();
        anim = this.GetComponent<Animator>();
        currentState = UnitState.spawning;
        this.tag = "Unit";
        squad = _squad;
        this.name = name;
    }

    //=============  Update()  ================================================//
    private void Update()
    {
        switch (currentState)
        {
            case UnitState.notAttacking:
                //------------------------------  CURRENTLY attacking or dying, OR within range  ----------------------------------------//
                if (enemyUnit != null)
                {
                    Debug.Log(this.name + "enemyUnit != null");
                    if (currentState == UnitState.attacking)
                    {
                        lerpData.actionTimer += Time.deltaTime;
                        float t = Mathf.Clamp(lerpData.actionTimer / lerpData.actionDuration, 0, 1);

                        transform.position = Vector3.Lerp(lerpData.originalPos, lerpData.targetPos, t);
                        transform.LookAt(Vector3.Lerp(lerpData.originalLookAt, lerpData.targetLookAt, t));
                    }
                    else
                    {
                        if (enemyUnit.currentState == UnitState.dying)
                            enemyUnit = null;
                        else if (Vector3.Distance(this.transform.position, enemyUnit.transform.position) > 3.5)
                            navMeshAgent.SetDestination(enemyUnit.transform.position);
                        else if (currentState == UnitState.notAttacking)
                            StartCoroutine(CoRoutine_Attack());
                    }
                }

                //------------------------------  Movement  ----------------------------------------//
                MovementAnimUpdate();

                break;

            case UnitState.spawning:
                if (wayPoints.Count > 0)
                {
                    if (Vector3.Distance(this.transform.position, navMeshAgent.destination) < 0.25f)
                    {
                        if (wayPointIndex == wayPoints.Count - 1)
                        {
                            wayPoints.Clear();
                            wayPointIndex = 0;
                            currentState = UnitState.notAttacking;
                        }
                        else
                        {
                            wayPointIndex++;
                            navMeshAgent.SetDestination(wayPoints[wayPointIndex]);
                        }
                    }
                }
                else
                {
                    currentState = UnitState.notAttacking;
                    Debug.Log("wayPoints.Count not greater than 0");
                }

                MovementAnimUpdate();

                break;
        }
    }

    //=============  CoRoutine - CoRoutine_Attack()  ==========================//
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

        Quaternion savedRot = enemyUnit.transform.rotation;
        enemyUnit.transform.LookAt(this.transform.position);
        lerpData.targetLookAt = enemyUnit.transform.position + enemyUnit.transform.right * 0.3f;
        enemyUnit.transform.rotation = savedRot;

        lerpData.actionDuration = 0.15f;
        lerpData.actionTimer = 0;

        enemyUnit.StartCoroutine(enemyUnit.CoRoutine_Die(this.transform.position));
        enemyUnit = null;

        //------------  WaitForSeconds(0.1)  ---------------//
        yield return new WaitForSeconds(0.1f);
        if (currentState != UnitState.dying)
        {
            audioSrc.loop = false;
            audioSrc.clip = unitSettings.audioClips.clip_Vocal_Attack;
            audioSrc.Play();

            transform.position = lerpData.targetPos;
            transform.LookAt(lerpData.targetLookAt);
        }

        //------------  WaitForSeconds(1.25)  ---------------//
        yield return new WaitForSeconds(1.25f);
        if (currentState != UnitState.dying)
        {
            navMeshAgent.enabled = true;
            currentState = UnitState.notAttacking;
        }
    }

    //=============  Coroutine - CoRoutine_Die()  =============================//
    IEnumerator CoRoutine_Die(Vector3 attackerPos)
    {
        transform.LookAt(attackerPos);
        currentState = UnitState.dying;
        anim.SetFloat("Speed", 0);

        audioSrc.loop = false;
        audioSrc.clip = unitSettings.audioClips.clip_Vocal_Die;
        audioSrc.Play();

        anim.SetTrigger("Death - Standard");

        yield return new WaitForSeconds(0.1f);
        Destroy(navMeshAgent);
    }

    //=============  Coroutine - CoRoutine_AssignObjective()  =================//
    public IEnumerator CoRoutine_AssignObjective(Vector3 target)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(0, timingRandom));

        navMeshAgent.SetDestination(target);
    }

    public void AssignObjective(Vector3 target)
    {
        navMeshAgent.SetDestination(target);
    }

    //=============  Function - Select()  =====================================//
    public void SelectUnit(GameObject _selectionCircle)
    {
        selectionCircle = UnityEngine.Object.Instantiate(_selectionCircle, transform.position, transform.rotation, this.transform);

        audioSrc.clip = unitSettings.audioClips.clip_GUI_Selected;
        audioSrc.loop = false;
        audioSrc.Play();
    }

    public void DeselectUnit()
    {
        Destroy(selectionCircle);
    }

    //=============  Function - AssignEnemy()  ================================//
    public void AssignEnemy(Unit newEnemy)
    {
        enemyUnit = newEnemy;
    }

    //=============  Function - MovementAnimUpdate()  =========================//
    void MovementAnimUpdate()
    {
        if (Vector3.Distance(this.transform.position, navMeshAgent.destination) > 0.1f)
        {
            anim.SetFloat("Speed", navMeshAgent.velocity.magnitude);

            if (!audioSrc.isPlaying || audioSrc.clip != unitSettings.audioClips.clip_Running)
            {
                audioSrc.clip = unitSettings.audioClips.clip_Running;
                audioSrc.loop = true;
                audioSrc.Play();
            }
        }
        else
        {
            anim.SetFloat("Speed", 0);
            if (audioSrc.clip == unitSettings.audioClips.clip_Running && audioSrc.isPlaying)
            {
                audioSrc.Stop();
                audioSrc.loop = false;
                anim.SetFloat("AnimSpeed", UnityEngine.Random.Range(0.9f, 1.1f));
            }

            if (navMeshAgent.velocity.magnitude > 0)
                navMeshAgent.SetDestination(this.transform.position);
        }
    }

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
