using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit_Squad : MonoBehaviour
{
    public Unit.Team team;
    public List<Unit> unitList;
    public bool enemiesAcquired;
    float timeTillNextUpdate;
    const float timeBetweenUpdates = 5;
    public Transform squadTransform;
    public Formation formation;
    public Transform squadTarget;
    public LineRenderer line_movingToFormation;
    List<RectTransform> positionChevrons = new List<RectTransform>();
    public LineRenderer line_formationToTarget;

    public float range_squad = 100;
    public float range_unit = 10;

    [System.NonSerialized]
    public Unit_Squad enemySquad;

    [System.NonSerialized]
    public bool hadEnemySquad = false;

    public enum SquadState { idle, movingToFormation, formationToObjective, rotatingFormation, retreating}
    [System.NonSerialized]
    public SquadState squadState;

    //===================  Update()  ==============================================================================================//
    private void Update()
    {
        timeTillNextUpdate -= Time.deltaTime;

        if (timeTillNextUpdate <= 0)
        {
            // ---------------  Rest timer  -------------------------------------------------------------------------//
            timeTillNextUpdate = timeBetweenUpdates;

            //-----------------------------  If we have no units (alive OR dead), destroy this obj  ---------------------//
            if (transform.childCount < 5) {
                DestroyVisualMarkers();
                Destroy(this.gameObject);
                return;
            }

            //-----------------------------  If all units are dead, wipe squad  -------------------------------------//
            else if (unitList.Count == 0) {
                DestroyVisualMarkers();
                Destroy(this);
                return;
            }

            //-----------------------------  If enemySquad has been eliminated  -------------------------------------//
            if (hadEnemySquad && enemySquad == null) {
                hadEnemySquad = false;

                for (int i = 0; i < unitList.Count; i++)
                    unitList[i].AssignObjective(unitList[i].destination);
            }

            //-----------------------------  GetEnemySquad()  -------------------------------------//
            else if (enemySquad == null && team != Unit.Team.none) {
                GetEnemySquad();

                if (enemySquad != null)
                    if (squadState == SquadState.idle)
                        RotateSquadToFaceEnemy();

                foreach (Unit u in unitList)
                    u.anims[0].SetBool("Guard", true);
            }

            //-----------------------------  Break formation  -------------------------------------//
            if (enemySquad != null && enemySquad.squadState == SquadState.retreating)
            {
                Debug.Log(this.name + " break formation");
                foreach (Unit u in unitList) {
                    u.combatUnitVars.frontLineVars.inFront = null;
                    u.combatUnitVars.frontLineVars.behind = null;
                    u.combatUnitVars.frontLineVars.queued = false;
                    u.combatUnitVars.frontLineVars.inFormation = false;
                }
            }

            //--------------------------------  Movement  ------------------------------------------------//
            if (squadState == SquadState.idle || squadState == SquadState.retreating) {
                int numberRetreating = 0;
                for (int i = 0; i < unitList.Count; i++)
                    if (unitList[i].currentState == Unit.UnitState.retreating)
                        numberRetreating++;

                if (squadState == SquadState.idle && numberRetreating > (float)unitList.Count * 0.5) {
                    squadState = SquadState.retreating;
                   
                }
                else if (squadState == SquadState.retreating && numberRetreating < (float)unitList.Count * 0.5)
                    squadState = SquadState.idle;
            }
            else
            {
                bool inFormation = false;
                int numberInFormation = 0;

                for (int i = 0; i < unitList.Count; i++)
                    if (unitList[i].properlyRotated)
                        numberInFormation++;

                if (numberInFormation > (float)unitList.Count * 0.9)
                    inFormation = true;

                switch (squadState) {
                    //-------  case: movingToFormation  ----------------//
                    case SquadState.movingToFormation:
                        if (team == Unit.Team.human)
                            line_movingToFormation.SetPosition(0, this.SquadPos());
                        if (inFormation)
                            FormationMarch();
                        break;

                    //-------  case: formationToObjective  ----------------//
                    case SquadState.formationToObjective:
                        if (team == Unit.Team.human)
                            line_formationToTarget.SetPosition(0, this.SquadPos());
                        if (inFormation)
                            FinishFormationMarch();
                        break;

                    //-------  case: rotatingFormation  ----------------//
                    case SquadState.rotatingFormation:
                        if (inFormation)
                            FinishFormationMarch();
                        break;
                }
            }
        }
    }

    //============ Function - SquadPos()  ===============================//
    public Vector3 SquadPos()
    {
        if (unitList.Count == 0)
            Debug.Log("ERROR: " + this.name + " unitList == 0");

        Vector3 squadPos = Vector3.zero;
        for (int i = 0; i < unitList.Count; i++)
            squadPos += unitList[i].transform.position;

        squadPos /= unitList.Count;

        return squadPos;
    }

    //============ Function - FormationPos()  ===============================//
    public static Vector3 FormationPos(int columns, int index, Unit_Combat_Types.Type type, float random, int formationSize, Transform squadTransform)
    {
        float spacing;
        switch (type) {
            case Unit_Combat_Types.Type.archer:
                spacing = Unit_Combat_Types.archerSpacing;
                break;

            case Unit_Combat_Types.Type.pikeman:
                spacing = Unit_Combat_Types.pikemanSpacing;
                break;

            case Unit_Combat_Types.Type.shield:
                spacing = Unit_Combat_Types.shieldSpacing;
                break;

            default:
                spacing = 1.5f;
                break;
        }

        if (columns == 0)
            Debug.Log("columns == 0");

        Vector3 localPos = Vector3.zero;

        int rows = (int)Mathf.Ceil((float)formationSize / (float)columns);

        int row = (int)Mathf.Ceil(index / columns);
        int column = index - row * columns;

        // Y position
        float yLength = (rows - 1) * spacing;
        localPos.y = yLength / 2 - spacing * row;

        // X position
        float xWidth = (columns - 1) * spacing;
        localPos.x = -xWidth / 2 + spacing * column;

        Vector3 globalPos = squadTransform.position + squadTransform.transform.right * localPos.x + squadTransform.transform.forward * localPos.y;

        Vector3 randomVec = new Vector3(Random.Range(-random, random), 0, Random.Range(-random, random));
        globalPos += randomVec;

        return globalPos;
    }

    //============ Function - FrontLineVarsAssignment()  ===============================//
    public void FrontLineVarsAssignment(int index, List<Unit> _unitList)
    {
        //----------------  Who will take our place if we die?  --------------------//
        Unit behind = null;
        if (_unitList.Count > index + formation.columns)
            behind = _unitList[index + formation.columns];

        _unitList[index].combatUnitVars.frontLineVars.behind = behind;
        
        //----------------  Who do we queue up behind?  --------------------//
        Unit inFront = null;
        if (0 < index - formation.columns)
            inFront = _unitList[index - formation.columns];

        _unitList[index].combatUnitVars.frontLineVars.inFront = inFront;
        _unitList[index].anims[0].SetBool("FrontLine", inFront == null);

        //----------------  Reset combat-related variables  --------------------//
        _unitList[index].combatUnitVars.frontLineVars.queued = false;
        _unitList[index].combatUnitVars.frontLineVars.inFormation = true;
    }

    //============ Function - CreateSquad()  ===============================//
    public static Unit_Squad CreateSquad(Transform hierarchyUnits, Unit.Team team, Formation formation)
    {
        Unit_Squad squad = new GameObject().AddComponent<Unit_Squad>();
        squad.transform.position = Vector3.zero;
        squad.transform.rotation = Quaternion.identity;
        squad.name = team + " squad " + (1 + hierarchyUnits.childCount).ToString();
        squad.transform.parent = hierarchyUnits;
        squad.unitList = new List<Unit>();

        squad.squadTransform = new GameObject().transform;
        squad.squadTransform.transform.parent = squad.transform;
        squad.squadTransform.name = "SquadTransform";

        squad.squadTarget = new GameObject().transform;
        squad.squadTarget.name = "squadTarget";
        squad.squadTarget.parent = squad.transform;

        squad.formation.columns = 5;

        squad.line_movingToFormation = new GameObject().AddComponent<LineRenderer>();
        squad.line_movingToFormation.material = FindObjectOfType<World_GenericVars>().materials.lineMovementTrailMaterial;
        squad.line_movingToFormation.textureMode = LineTextureMode.Tile;
        squad.line_movingToFormation.SetPosition(1, Vector3.zero);
        squad.line_movingToFormation.gameObject.layer = 5;
        squad.line_movingToFormation.transform.parent = squad.transform;
        squad.line_movingToFormation.name = "Line - movingToFormation";

        squad.line_formationToTarget = new GameObject().AddComponent<LineRenderer>();
        squad.line_formationToTarget.material = FindObjectOfType<World_GenericVars>().materials.lineMovementTrailMaterial;
        squad.line_formationToTarget.textureMode = LineTextureMode.Tile;
        squad.line_formationToTarget.SetPosition(1, Vector3.zero);
        squad.line_formationToTarget.gameObject.layer = 5;
        squad.line_formationToTarget.transform.parent = squad.transform;
        squad.line_formationToTarget.name = "Line - formationToTarget";

        Canvas canvas = new GameObject().AddComponent<Canvas>();
        canvas.gameObject.layer = 5;

        squad.formation.columns = formation.columns;

        squad.team = team;

        return squad;
    }

    //============ Function - GetEnemySquad()  ===============================//
    public void GetEnemySquad()
    {
        Unit_Squad[] otherSquads = FindObjectsOfType<Unit_Squad>();

        squadTransform.position = SquadPos();

        Unit_Squad closestSquad = null;
        float shortestDistance = float.MaxValue;

        for (int i = 0; i < otherSquads.Length; i++)
            if (otherSquads[i] != this && otherSquads[i].team != team)
            {
                float distance = Vector3.Distance(SquadPos(), otherSquads[i].SquadPos());

                if (distance < range_squad)
                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        closestSquad = otherSquads[i];
                    }
            }

        enemySquad = closestSquad;
    }

    //============ Function - RotateSquadToFaceEnemy()  ===============================//
    void RotateSquadToFaceEnemy()
    {
        squadTransform.LookAt(enemySquad.squadTransform.position);

        Vector3[] formationPositions = OptimalUnitPositions();

        for (int i = 0; i < unitList.Count; i++) {
            unitList[i].StartCoroutine(unitList[i].CoRoutine_AssignObjective(formationPositions[i], true));
            FrontLineVarsAssignment(i, unitList);
        }
    }
    
    //============ Function - OptimalUnitPositions()  ===============================//
    Vector3[] OptimalUnitPositions()
    {
        // Create array of formation positions, for each position find the nearest unit, 
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        Vector3[] formationPositions = new Vector3[unitList.Count];                                                             //
        List<int> takenUnits = new List<int>();                                                                                 //
                                                                                                                                //
        for (int i = 0; i < formationPositions.Length; i++)                                                                     //
            formationPositions[i] = FormationPos(formation.columns, i, unitList[0].combatUnitVars.type, unitList[i].formationRandom, unitList.Count, squadTransform); //  1 - Create an array of formation positions
                                                                                                                                                //
        List<Unit> newUnitList = new List<Unit>();                                                                              //  2 - Create a new unitList (for the sake of re-indexing the original)
                                                                                                                                //
        for (int i = 0; i < formationPositions.Length; i++)                                                                     //  3 - 
        {                                                                                                                       //
            float prevShortestDistance = float.MaxValue;                                                                        //
            int closestUnit = int.MaxValue;                                                                                     //
                                                                                                                                //
            for (int j = 0; j < unitList.Count; j++)                                                                            //
            {                                                                                                                   //
                bool unitTaken = false;                                                                                         //
                                                                                                                                //
                for (int k = 0; k < takenUnits.Count; k++)                                                                      //
                    if (j == takenUnits[k])                                                                                     //
                        unitTaken = true;                                                                                       //
                                                                                                                                //
                if (!unitTaken)                                                                                                 //
                {                                                                                                               //
                    float distance_Unit = Vector3.Distance(unitList[j].transform.position, formationPositions[i]);              //
                                                                                                                                //
                    if (distance_Unit < prevShortestDistance)                                                                   //
                    {                                                                                                           //
                        closestUnit = j;                                                                                        //
                        prevShortestDistance = distance_Unit;                                                                   //
                    }                                                                                                           //
                }                                                                                                               //
            }                                                                                                                   //
                                                                                                                                //
            takenUnits.Add(closestUnit);                                                                                        //
            newUnitList.Add(unitList[closestUnit]);                                                                             //
        }                                                                                                                       //
                                                                                                                                //                                             
        unitList = newUnitList;                                                                                                 //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        for (int i = 0; i < unitList.Count; i++)
            FrontLineVarsAssignment(i, unitList);

        return formationPositions;
    }

    //============ Function - AssignSquadTarget()  ===============================//
    public void MoveSquadToFormationForMarch(Transform _squadTarget)
    {
        //-----------------  Reset Visuals from prevous marches  -------------------------//
        DestroyVisualMarkers();

        //-----------------  Squad calculations  -------------------------//
        float distance_Squad = Vector3.Distance(squadTransform.position, _squadTarget.position);
        
        squadTarget.position = _squadTarget.position;
        squadTarget.rotation = _squadTarget.rotation;
        squadTransform.LookAt(squadTarget.position);
        
        if (distance_Squad > 20)
            squadTransform.position += squadTransform.forward * 10;

        Vector3[] formationPositions = OptimalUnitPositions();

        if (distance_Squad > 50) {
            for (int i = 0; i < unitList.Count; i++) {
                if (unitList.Count > 3)
                    unitList[i].StartCoroutine(unitList[i].CoRoutine_AssignObjective(formationPositions[i], true));
                else
                    unitList[i].AssignObjective(formationPositions[i]);

                if (team == Unit.Team.human)
                    CreateVisualMarker(formationPositions[i], squadTransform.rotation);
            }

            if (team == Unit.Team.human) {
                line_movingToFormation.SetPosition(0, squadTransform.position);
                line_movingToFormation.SetPosition(1, squadTarget.position);
            }

            squadState = SquadState.movingToFormation;
        }
        else
            FormationMarch();
    }

    //============ Function - FormationMarch()  =================================//
    void FormationMarch() {
        if (team == Unit.Team.human)
        {
            DestroyVisualMarkers();

            line_formationToTarget.SetPosition(0, squadTransform.position);
            line_formationToTarget.SetPosition(1, squadTarget.position);
        }

        for (int i = 0; i < unitList.Count; i++) {
            Vector3 target = FormationPos(formation.columns, i, unitList[0].combatUnitVars.type, unitList[i].formationRandom, unitList.Count, squadTarget);

            if (unitList.Count > 3)
                unitList[i].StartCoroutine(unitList[i].CoRoutine_AssignObjective(target, false));
            else
                unitList[i].AssignObjective(target);

            if (team == Unit.Team.human)
                CreateVisualMarker(target, squadTarget.rotation);
        }

        squadState = SquadState.formationToObjective;
    }

    //============ Function - FinishFormationMarch()  ===========================//
    void FinishFormationMarch()
    {
        DestroyVisualMarkers();

        squadState = SquadState.idle;
    }
    
    //============ Function - CreateVisualMarker()  ===============================//
    void CreateVisualMarker(Vector3 pos, Quaternion rot)
    {
        RectTransform rt = Instantiate(FindObjectOfType<World_GenericVars>().materials.unitChevron, pos, rot, GameObject.Find("World - Canvas").transform).GetComponent<RectTransform>();
        positionChevrons.Add(rt);
        rt.name = this.name + ", position " + positionChevrons.Count + " of " + unitList.Count;
        rt.gameObject.layer = LayerMask.NameToLayer("UI");
    }

    //============ Function - DestroyVisualMarkers()  ===============================//
    void DestroyVisualMarkers()
    {
        for (int i = 0; i < positionChevrons.Count; i++)
            Destroy(positionChevrons[i].gameObject);

        positionChevrons.Clear();

        line_movingToFormation.SetPosition(0, Vector3.zero);
        line_movingToFormation.SetPosition(1, Vector3.zero);
        line_formationToTarget.SetPosition(0, Vector3.zero);
        line_formationToTarget.SetPosition(1, Vector3.zero);
    }

    //============ Struct - Formation  ===============================//
    [System.Serializable]
    public struct Formation
    {
        public int columns;
    }
    
}
