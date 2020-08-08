using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AudioSource), typeof(NavMeshAgent), typeof(CapsuleCollider))]
public class Unit : MonoBehaviour
{
    //=============  Enums  ======================================//
    public enum Team { none, human, orc }
    public enum UnitType { SOLDIER, WAGON, WORKER }
    public enum UnitState { spawning, available, unavailable, dying, retreating}

    //=============  Variables  ====================================================//
    [NonSerialized] public AudioSource audioSrc;

    [NonSerialized] public NavMeshAgent navMeshAgent;

    public Animator[] anims;

    [NonSerialized] public PositionLerpData lerpData;

    public float formationRandom;
    public float timingRandom;

    [System.NonSerialized] public List<Vector3> wayPoints = new List<Vector3>();

    [System.NonSerialized] public int wayPointIndex;

    GameObject selectionCircle;

    public GameObject protoUnit;

    [System.NonSerialized] public bool properlyRotated;

    [System.NonSerialized] public bool selected;

    public UnitType type;
    [System.NonSerialized] public Vector3 destination;  //If we take a detour to kill an enemy, resume by going to this position

    public Unit_Combat.UnitCombatVars   combatUnitVars; // enemyUnit, audio, squad
    public Unit_Wagon.UnitWagonVars     wagonUnitVars;  // team
    public Unit_Worker.UnitWorkerVars   workerUnitVars; // team
    public Unit_Human.HumanVars         humanUnitVars;  // skinnedMeshRenderer, item transform, item mesh
    
    [NonSerialized] public UnitState currentState;

    //=============  Start()  ============================================//
    private void Start()
    {
        if (type == UnitType.WAGON || type == UnitType.WORKER)
            Initialise(null, name);
    }

    //=============  Initialise()  ============================================//
    public void Initialise(Unit_Squad _squad, string _name)
    {
        name = _name;
        audioSrc = this.GetComponent<AudioSource>();
        
        navMeshAgent = this.GetComponent<NavMeshAgent>();
        if (navMeshAgent == null)
            navMeshAgent = this.gameObject.AddComponent<NavMeshAgent>();

        if (type == UnitType.SOLDIER) {
            currentState = Unit.UnitState.spawning;
            combatUnitVars.squad = _squad;

            Unit_Human.EnableDisableRagdoll(this, false);
        }
        else if (type == UnitType.WAGON)
        {
            if (wagonUnitVars.wagonMeshRenderer != null)
                switch (wagonUnitVars.wagonType)
                {
                    case Unit_Wagon.WagonType.hay:
                        wagonUnitVars.wagonMeshRenderer.sharedMesh = wagonUnitVars.wagonMeshes.hayCart[UnityEngine.Random.Range(0, wagonUnitVars.wagonMeshes.hayCart.Length)];
                        break;
                    case Unit_Wagon.WagonType.wood:
                        wagonUnitVars.wagonMeshRenderer.sharedMesh = wagonUnitVars.wagonMeshes.woodCart[UnityEngine.Random.Range(0, wagonUnitVars.wagonMeshes.woodCart.Length)];
                        break;
                    case Unit_Wagon.WagonType.stone:
                        wagonUnitVars.wagonMeshRenderer.sharedMesh = wagonUnitVars.wagonMeshes.stoneCart[UnityEngine.Random.Range(0, wagonUnitVars.wagonMeshes.stoneCart.Length)];
                        break;
                    default:
                        break;
                }
            else
                Debug.Log(this.name + " wagonUnitVars.wagonMeshRenderer == null, cannot assign mesh");
        }
    }

    //=============  Update()  ================================================//
    private void Update()
    {
        if (type == UnitType.SOLDIER)
            Unit_Combat.Update_Combat(this);
        else if (type == UnitType.WAGON)
            Unit_Wagon.Update_Wagon(this);

        MovementAnimAndAudioUpdate();
    }

    //=============  Coroutine - CoRoutine_AssignObjective()  =================//
    public IEnumerator CoRoutine_AssignObjective(Vector3 target, bool delay)
    {
        float delayTime;

        if (delay)
            delayTime = UnityEngine.Random.Range(0, timingRandom);
        else
            delayTime = UnityEngine.Random.Range(0, 0.01f);

        properlyRotated = false;

        yield return new WaitForSeconds(delayTime);
        
        AssignObjective(target);
    }

    //=============  Function - AssignObjective()  ==============================//
    public void AssignObjective(Vector3 target)
    {
        if (currentState != Unit.UnitState.dying)
        {
            switch (type) {
                case UnitType.SOLDIER:
                    navMeshAgent.SetDestination(target);
                    navMeshAgent.speed = Unit_Combat.humanJoggingSpeed;
                    destination = target;
                    break;

                case UnitType.WAGON:
                    wayPoints.Clear();

                    if (wagonUnitVars.lineRenderer == null)
                        wagonUnitVars.lineRenderer = new GameObject().AddComponent<LineRenderer>();
                    else
                        wagonUnitVars.lineRenderer.enabled = true;

                    Vector3[] _wayPoints = BezierCurves.BezierPositions(wagonUnitVars.lineRenderer, 50, this.transform.position, target, wagonUnitVars.initialHeading, wagonUnitVars.destinationHeading);
                    wagonUnitVars.lineRenderer.sharedMaterial = GameObject.Find("World - Generic variables").GetComponent<World_GenericVars>().materials.lineMovementTrailMaterial;

                    for (int i = 0; i < _wayPoints.Length; i++)
                        wayPoints.Add(_wayPoints[i]);

                    break;

                case UnitType.WORKER:
                    navMeshAgent.speed = Unit_Combat.humanWalkingSpeed;
                    navMeshAgent.SetDestination(target);
                    destination = target;
                    break;
            }
        }
    }

    //=============  Function - Select()  =====================================//
    public void SelectUnit(Unit.UnitType unitType)
    {
        selected = true;
        selectionCircle = UnityEngine.Object.Instantiate(HUD_UnitSelection.GetSelectionObj(unitType), transform.position, transform.rotation, this.transform);

        if (unitType == UnitType.SOLDIER) {
            if (combatUnitVars.combatAudio != null) {
                audioSrc.clip = combatUnitVars.combatAudio.audioClips.clip_GUI_Selected;
                audioSrc.loop = false;
                audioSrc.Play();
            }
            else
                Debug.Log(this.name + "combatUnitVars.combatAudio == null");
        }
        else if (unitType == UnitType.WAGON) {
            if (wagonUnitVars.wagonAudio != null) {
                audioSrc.clip = wagonUnitVars.wagonAudio.audioClips.clip_GUI_Selected;
                audioSrc.loop = false;
                audioSrc.Play();
            }
            else
                Debug.Log(this.name + "combatUnitVars.combatAudio == null");
        }
    }

    //=============  Function - Deselect()  =====================================//
    public void DeselectUnit()
    {
        selected = false;
        Destroy(selectionCircle);
    }

    //=============  Function - MovementAnimUpdate()  =========================//
    void MovementAnimAndAudioUpdate()
    {
        float speed = 0;
        speed = navMeshAgent.velocity.magnitude;

        if (speed > 1)
        {
            for (int i = 0; i < anims.Length; i++)
                anims[i].SetFloat("Speed", navMeshAgent.velocity.magnitude);

            if (type == UnitType.SOLDIER)
            {
                if (!audioSrc.isPlaying || audioSrc.clip != combatUnitVars.combatAudio.audioClips.clip_Running)
                {
                    audioSrc.clip = combatUnitVars.combatAudio.audioClips.clip_Running;
                    audioSrc.loop = true;
                    audioSrc.Play();
                }
            }
            else if (type == UnitType.WAGON)
                if (!audioSrc.isPlaying || audioSrc.clip != wagonUnitVars.wagonAudio.audioClips.clip_Moving)
                {
                    audioSrc.clip = wagonUnitVars.wagonAudio.audioClips.clip_Moving;
                    audioSrc.loop = true;
                    audioSrc.Play();

                    for (int i = 0; i < wagonUnitVars.particles.Length; i++)
                        wagonUnitVars.particles[i].Play();
                }
        }
        else
        {
            for (int i = 0; i < anims.Length; i++)
            {
                anims[i].SetFloat("Speed", 0);
                anims[i].SetFloat("AnimSpeed", UnityEngine.Random.Range(0.9f, 1.1f));
            }

            //------------------  Audio  ---------------------------------------------------------------------------------//
            if 
            (
                type == UnitType.SOLDIER && combatUnitVars.combatAudio != null && audioSrc.clip == combatUnitVars.combatAudio.audioClips.clip_Running && audioSrc.isPlaying
                ||
                type == UnitType.WAGON && wagonUnitVars.wagonAudio != null && audioSrc.clip == wagonUnitVars.wagonAudio.audioClips.clip_Moving && audioSrc.isPlaying
                ||
                type == UnitType.WORKER && workerUnitVars.workerAudio != null && audioSrc.clip == workerUnitVars.workerAudio.audioClips.clip_Moving && audioSrc.isPlaying
            ) {
                audioSrc.Stop();
                audioSrc.loop = false;
            }

            if (type == UnitType.WAGON)
                for (int i = 0; i < wagonUnitVars.particles.Length; i++)
                    wagonUnitVars.particles[i].Stop();

            if (combatUnitVars.squad != null)
                if (!properlyRotated && Vector3.Angle(transform.forward, combatUnitVars.squad.squadTransform.forward) > 5)
                    transform.rotation = Quaternion.Lerp(transform.rotation, combatUnitVars.squad.squadTransform.rotation, 0.05f);
                else
                    properlyRotated = true;
        }
    }

    //=============  Struct - PositionLerpData  ==============================//
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