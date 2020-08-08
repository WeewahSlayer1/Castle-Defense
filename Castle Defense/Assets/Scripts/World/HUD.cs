using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HUD : MonoBehaviour
{
    //=============  Variables  ===========================// Currently used exclusively for constructing defenses
    public Scroll          scroll;
    public              ScrollAudio     scrollAudio;
    public              UnitControl     unitControl;
    public              AudioGUI        audioGUI;
    public              CursorMods      cursorMods;
    [SerializeField]    BuildingVars    buildingVars;
    [SerializeField]    LayerMask       layerMaskGround;        //Used to place buildings
    [SerializeField]    LayerMask       layerMaskNotGround;     //Used to check building placement overlaps
    [SerializeField]    LayerMask       layerMaskCombat;
    [SerializeField]    LayerMask       layerMaskGroundAndCombat;
    [SerializeField]    Building        activeBuilding;
    [SerializeField]    Transform       hierarchy_units;
    public DoubleClick.dblClickSettings dblClickS;
    Unit.Team                           playerTeam = Unit.Team.human;
    bool                                overlap;
    AudioSource                         audioSrcGUI;
    float                               doubleClickTimer;
    List<Unit>                          selectedUnits = new List<Unit>();
    Unit_Squad                          currentSquad;
    List<GameObject>                    protoFormation = new List<GameObject>();


    //=============  Enums  ===========================// Currently used exclusively for constructing defenses
    public enum BuildPhase {none, moving, walling}

    private void Start()
    {
        audioGUI.audioSrc = this.GetComponent<AudioSource>();

        scroll.scrollButtons.scrollButtonOpen.GetComponent<HUD_button>().HUDscript = this;
        for (int i = 0; i < scroll.scrollButtons.scrollButtons.Count; i++)
            scroll.scrollButtons.scrollButtons[i].GetComponent<HUD_button>().HUDscript = this;

        scrollAudio.scrollAudioSrc = scroll.GetComponent<AudioSource>();
    }

    //=============  Update()  ===========================// Used for selecting buildings or units, or placing defenses
    private void Update()
    {
        for (int i = 0; i < selectedUnits.Count; i++)
            if (selectedUnits[i].currentState == Unit.UnitState.dying)
                selectedUnits.Remove(selectedUnits[i]);
        
        if (buildingVars.buildPhase == BuildPhase.none)
        {
            //---------------  Left Click  --------------------------------------------------------------------------------------------------//
            if (Input.GetMouseButtonDown(0))
            {
                dblClickS = DoubleClick.UpdateDblClick(dblClickS);

                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (!EventSystem.current.IsPointerOverGameObject())
                    if (Physics.Raycast(ray, out hit, float.MaxValue, layerMaskCombat, QueryTriggerInteraction.Collide))
                    {
                        //------------  Click on Unit  --------------------------------------------------//
                        if (hit.collider.GetComponent<Unit>() != null)
                        {
                            if (hit.collider.GetComponent<Unit>().currentState != Unit.UnitState.dying)
                                if (hit.collider.GetComponent<Unit>().squad.team == playerTeam) //Friendly unit
                                {
                                    if (!Input.GetKey(KeyCode.LeftShift))
                                    {
                                        for (int i = 0; i < selectedUnits.Count; i++)
                                            selectedUnits[i].DeselectUnit();

                                        selectedUnits.Clear();
                                        currentSquad = null;
                                    }

                                    selectedUnits.Add(hit.collider.GetComponent<Unit>());
                                    hit.collider.GetComponent<Unit>().SelectUnit(unitControl.selectionCircle);
                                }
                                else if (currentSquad != null) //Enemy unit
                                {
                                    if (currentSquad != null)
                                    {
                                        audioGUI.audioSrc.clip = audioGUI.clip_attackClick; audioGUI.audioSrc.Play();
                                        currentSquad.AssignSquadTarget(hit.point);
                                    }
                                }
                        }
                        //------------  Click on Building  --------------------------------------------------//
                        else if (hit.collider.GetComponent<Building>() != null && hit.collider.GetComponent<Building>().team == playerTeam)
                            ClickedOnBuildingReplaceScroll(hit.collider.GetComponent<Building>());
                        //------------  Click on invalid Unit/Building  --------------------------------------------------//
                        else
                            Debug.Log("hit LayerMaskCombat, but neither friendly unit nor building if-statements were satisfied");
                    }
                    else if (Physics.Raycast(ray, out hit, float.MaxValue, layerMaskGround) && currentSquad != null)
                    {
                        protoFormation = new List<GameObject>();
                        currentSquad.squadTransform.position = hit.point;

                        //Spawn protoFormation;
                        for (int i = 0; i < selectedUnits.Count; i++)
                        {
                            GameObject protoUnit = Object.Instantiate(selectedUnits[i].protoUnit);
                            protoFormation.Add(protoUnit);
                        }

                        for (int i = 0; i < protoFormation.Count; i++)
                            protoFormation[i].transform.position = Unit_Squad.FormationPos(currentSquad.formation.columns, i, currentSquad.formation.spacing, selectedUnits[i].formationRandom, protoFormation.Count, currentSquad.squadTransform.transform);
                    }
                    else
                    {
                        audioGUI.audioSrc.clip = audioGUI.clip_nullClick; audioGUI.audioSrc.Play();
                        Debug.Log("NullClick");
                    }
            }
            else if (Input.GetMouseButton(0))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit, float.MaxValue, layerMaskGround) && currentSquad != null)
                {
                    currentSquad.squadTransform.LookAt(new Vector3 (hit.point.x, currentSquad.squadTransform.position.y, hit.point.z));

                    for (int i = 0; i < protoFormation.Count; i++)
                        protoFormation[i].transform.position = Unit_Squad.FormationPos(currentSquad.formation.columns, i, currentSquad.formation.spacing, selectedUnits[i].formationRandom, protoFormation.Count, currentSquad.squadTransform.transform);
                }
            }

            //------------  Click on ground  --------------------------------------------------//
            if (Input.GetMouseButtonUp(0))
            {
                for (int i = 0; i < protoFormation.Count; i++)
                    Destroy(protoFormation[i]);

                protoFormation.Clear();

                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                
                if (!Physics.Raycast(ray, out hit, float.MaxValue, layerMaskCombat))
                    if (Physics.Raycast(ray, out hit, float.MaxValue, layerMaskGround))
                    {
                        //---------------------------  send new squad  -------------------------------------//
                        // 1 - CreateSquad()
                        currentSquad = Unit_Squad.CreateSquad(hierarchy_units, playerTeam);

                        // 2 - selectedUnits to new squad
                        for (int i = 0; i < selectedUnits.Count; i++)
                        {
                            selectedUnits[i].squad.unitList.Remove(selectedUnits[i]);
                            currentSquad.unitList.Add(selectedUnits[i]);
                            selectedUnits[i].squad = currentSquad;
                            selectedUnits[i].transform.parent = currentSquad.transform;
                        }

                        // 3 - AssignSquadTargets()
                        currentSquad.AssignSquadTarget(hit.point);

                        //---------------------------  protoFormation  -------------------------------------//
                    }
            }

            //---------------  Right Click  -------------------------------------------------------------------------------------------------//
            if (Input.GetMouseButtonDown(1))
            {
                for (int i = 0; i < selectedUnits.Count; i++)
                    selectedUnits[i].DeselectUnit();

                selectedUnits.Clear();
                currentSquad = null;
            }

            //---------------  Double Click  -------------------------------------------------------------------------------------------------//
            if (dblClickS.dblClick)
            {
                Debug.Log("Double click");
            }

            dblClickS.dblClick = false;
        }
        else
            if (buildingVars.currentBuildObj != null)
            if (Input.GetMouseButtonDown(1) && !overlap)
            //-------------------------  Finish placement  ---------------------------------------------------------------------//
            {
                //Apply material
                buildingVars.currentBuildObj.GetComponent<Renderer>().material = buildingVars.currentBuildAsset.mat_Final;
                for (int i = 0; i < buildingVars.currentBuildObj.transform.childCount; i++)
                    buildingVars.currentBuildObj.transform.GetChild(i).GetComponent<Renderer>().material = buildingVars.currentBuildAsset.mat_Final;

                //Place final wall node
                if (buildingVars.buildPhase == BuildPhase.walling)
                {
                    Object.Instantiate(buildingVars.currentBuildAsset.buildingObj, buildingVars.currentNode.position + (buildingVars.currentBuildObj.transform.position - buildingVars.currentNode.position) * 2, Quaternion.identity, buildingVars.hierarchy_buildings);
                    buildingVars.currentNode = null;
                }

                //reset currentBuildObj, currentBuildAsset, & buildphase
                buildingVars.currentBuildObj = null;
                buildingVars.currentBuildAsset = null;
                buildingVars.buildPhase = BuildPhase.none;

                //Audio
                buildingVars.buildAudioSrc.clip = scrollAudio.ding_confirm;
                buildingVars.buildAudioSrc.Play();
            }
            else
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit, 100.0f, layerMaskGround))
                {
                    //Collision detection
                    Collider[] colliders = Physics.OverlapBox(hit.point, buildingVars.currentBuildObj.GetComponent<BoxCollider>().size / 2, buildingVars.currentBuildObj.transform.rotation, layerMaskNotGround, QueryTriggerInteraction.Collide);
                    if (colliders.Length > 1) overlap = true;
                    else overlap = false;

                    if (overlap)
                        buildingVars.currentBuildAsset.mat_Proto.color = new Color(1, 0, 0, buildingVars.currentBuildAsset.mat_Proto.color.a);
                    else
                        buildingVars.currentBuildAsset.mat_Proto.color = new Color(1, 1, 1, buildingVars.currentBuildAsset.mat_Proto.color.a);

                    //-------------------------  Moving & rotating  -----------------------------------------------------------//
                    if (buildingVars.buildPhase != BuildPhase.walling)
                        if (!Input.GetMouseButton(0))           //---------------  Moving  -----------------------------------------//
                            buildingVars.currentBuildObj.transform.position = hit.point;
                        else
                        {
                            if (buildingVars.currentBuildAsset.wall.wallObj == null) //-----------------  Rotating  ------------------------//
                                buildingVars.currentBuildObj.transform.LookAt(hit.point);
                            else                                        //-----------------  Walling  ------------------------//
                            {
                                buildingVars.buildPhase = BuildPhase.walling;

                                buildingVars.currentBuildObj.GetComponent<Renderer>().material = buildingVars.currentBuildAsset.mat_Final;
                                for (int i = 0; i < buildingVars.currentBuildObj.transform.childCount; i++)
                                    buildingVars.currentBuildObj.transform.GetChild(i).GetComponent<Renderer>().material = buildingVars.currentBuildAsset.mat_Final;

                                buildingVars.currentNode = buildingVars.currentBuildObj.transform;
                                buildingVars.currentBuildObj = Object.Instantiate(buildingVars.currentBuildAsset.wall.wallObj, buildingVars.hierarchy_buildings);
                                buildingVars.currentBuildObj.GetComponent<Renderer>().material = buildingVars.currentBuildAsset.mat_Proto;
                            }
                        }
                    else  //-----------------  Walling  ------------------------//
                        if (Vector3.Distance(hit.point, buildingVars.currentNode.position) > buildingVars.currentBuildAsset.wall.wallLength)
                    {
                        buildingVars.currentBuildObj.transform.position = buildingVars.currentNode.position + Vector3.Normalize(hit.point - buildingVars.currentNode.position) * (buildingVars.currentBuildAsset.wall.wallLength / 2);
                        buildingVars.currentBuildObj.transform.LookAt(hit.point);
                        buildingVars.currentBuildObj.transform.rotation = Quaternion.Euler(0, buildingVars.currentBuildObj.transform.eulerAngles.y - 90, 0);
                        Debug.Log("buildingVars.currentBuildObj.transform.rotation: " + buildingVars.currentBuildObj.transform.rotation);

                        if (Input.GetMouseButtonDown(0) && !overlap)
                        {
                            buildingVars.buildAudioSrc.clip = scrollAudio.ding_confirm;
                            buildingVars.buildAudioSrc.Play();

                            buildingVars.currentNode = Object.Instantiate(buildingVars.currentBuildAsset.buildingObj, buildingVars.currentNode.position + Vector3.Normalize(hit.point - buildingVars.currentNode.position) * (buildingVars.currentBuildAsset.wall.wallLength), Quaternion.identity, buildingVars.hierarchy_buildings).transform;

                            buildingVars.currentBuildObj.GetComponent<Renderer>().material = buildingVars.currentBuildAsset.mat_Final;
                            for (int i = 0; i < buildingVars.currentBuildObj.transform.childCount; i++)
                                buildingVars.currentBuildObj.transform.GetChild(i).GetComponent<Renderer>().material = buildingVars.currentBuildAsset.mat_Final;

                            //Instantiate new object
                            buildingVars.currentBuildObj = Object.Instantiate(buildingVars.currentBuildAsset.wall.wallObj, buildingVars.hierarchy_buildings);

                            buildingVars.currentBuildObj.transform.position = buildingVars.currentNode.position + Vector3.Normalize(hit.point - buildingVars.currentNode.position) * (buildingVars.currentBuildAsset.wall.wallLength / 2);
                            buildingVars.currentBuildObj.transform.LookAt(hit.point);
                            buildingVars.currentBuildObj.transform.rotation = Quaternion.Euler(0, buildingVars.currentBuildObj.transform.eulerAngles.y - 90, 0);

                            buildingVars.currentBuildObj.GetComponent<Renderer>().material = buildingVars.currentBuildAsset.mat_Proto;
                        }
                    }
                }
            }
    }

    //=============  ClickedOnScrollBuildButton()  =================================//Called by HUD_button
    public void ClickedOnScrollBuildButton(BuildingAsset buildingAsset)
    {
        buildingVars = scroll.ClickedOnBuild(buildingAsset, buildingVars, scrollAudio);
    }

    //=============  ClickedOnBuildingReplaceScroll()  =============================//Called by HUD_button
    public void ClickedOnBuildingReplaceScroll(Building building)
    {
        if (scroll != null)
        {
            Vector3 scrollPos = scroll.transform.position;
            Quaternion scrollRot = scroll.transform.rotation;
            Transform scrollParent = scroll.transform.parent;

            Destroy(scroll.gameObject);

            scroll = Object.Instantiate(building.BuildMenuScroll, scrollPos, scrollRot, scrollParent).GetComponent<Scroll>();

            scroll.scrollButtons.scrollButtonOpen.GetComponent<HUD_button>().HUDscript = this;
            for (int i = 0; i < scroll.scrollButtons.scrollButtons.Count; i++)
                scroll.scrollButtons.scrollButtons[i].GetComponent<HUD_button>().HUDscript = this;

            scrollAudio.scrollAudioSrc = scroll.GetComponent<AudioSource>();

            scroll.ClickedOnScroll(scrollAudio);

            activeBuilding = building;
        }
        else
            Debug.Log("ERROR: our old scroll was deleted, so we don't know where to place the new one");
    }

    //=============  ClickedOnScrollTrainUnit()  ===================================// Called by HUD_button
    public void ClickedOnScrollTrainUnit(UnitAsset unitAsset)
    {
        if (Time.time - activeBuilding.lastSpawnTime > activeBuilding.spawnInterval)
        {
            if (activeBuilding.squad == null)
                activeBuilding.squad = Unit_Squad.CreateSquad(hierarchy_units, playerTeam);

            if (activeBuilding.squad.unitList.Count <= activeBuilding.UnitLimit - 1)
            {
                if (activeBuilding.SpawnWaypoints.Count >= 2)
                {
                    Vector3 spawnPos = activeBuilding.transform.position + activeBuilding.SpawnWaypoints[0].x * activeBuilding.transform.right + activeBuilding.SpawnWaypoints[0].z * activeBuilding.transform.forward;
                    Unit unit = Object.Instantiate(unitAsset.unitObj, spawnPos, Quaternion.identity, activeBuilding.squad.transform).GetComponent<Unit>();

                    activeBuilding.squad.unitList.Add(unit);

                    unit.Initialise(activeBuilding.squad, "Soldier #" + (activeBuilding.squad.unitList.Count).ToString());

                    for (int i = 0; i < activeBuilding.SpawnWaypoints.Count; i++)
                    {
                        Vector3 wayPoint = activeBuilding.transform.position + activeBuilding.SpawnWaypoints[i].x * activeBuilding.transform.right + activeBuilding.SpawnWaypoints[i].z * activeBuilding.transform.forward;
                        unit.wayPoints.Add(wayPoint);
                        unit.currentState = Unit.UnitState.spawning;
                    }
                    activeBuilding.squad.squadTransform.position = unit.wayPoints[unit.wayPoints.Count - 1];
                    activeBuilding.squad.squadTransform.rotation = activeBuilding.transform.rotation;

                    //If we've just added a new row, update formationPositions of every other squad member
                    if (activeBuilding.squad.unitList.Count != 1 && (activeBuilding.squad.unitList.Count - 1) % activeBuilding.squad.formation.columns == 0)
                    {
                        for (int i = 0; i < activeBuilding.squad.unitList.Count - 1; i++)
                        {
                            Vector3 newPos = Unit_Squad.FormationPos(activeBuilding.squad.formation.columns, i, activeBuilding.squad.formation.spacing, activeBuilding.squad.unitList[i].formationRandom, activeBuilding.squad.unitList.Count, activeBuilding.squad.squadTransform);

                            if (activeBuilding.squad.unitList[i].currentState == Unit.UnitState.spawning)
                                activeBuilding.squad.unitList[i].wayPoints[unit.wayPoints.Count - 1] = newPos;
                            else if (activeBuilding.squad.unitList[i].currentState == Unit.UnitState.notAttacking)
                                activeBuilding.squad.unitList[i].StartCoroutine(activeBuilding.squad.unitList[i].CoRoutine_AssignObjective(newPos));
                        }
                    }

                    //Replace last wayPoint with formationPosition
                    unit.wayPoints[unit.wayPoints.Count - 1] = Unit_Squad.FormationPos(activeBuilding.squad.formation.columns, activeBuilding.squad.unitList.Count - 1, activeBuilding.squad.formation.spacing, unit.formationRandom, activeBuilding.squad.unitList.Count, activeBuilding.squad.squadTransform);

                    scrollAudio.scrollAudioSrc.clip = unit.unitSettings.audioClips.clip_GUI_Selected;
                    scrollAudio.scrollAudioSrc.Play();
                }
                else
                    Debug.Log("ERROR: activeBuilding " + activeBuilding.name + " has less than 2 spawnWaypoints, so where will we spawn the unit?");

                activeBuilding.lastSpawnTime = Time.time;
            }
            else
                Debug.Log("Squad size is at limit of building.unitLimit, please relocate to another barracks");
        }
    }
    
    //==============  Unit control UI  =================================================//
    [System.Serializable]
    public struct UnitControl
    {
        public GameObject selectionCircle;
    }

    //==============  AudioGUI  =================================================//
    [System.Serializable]
    public struct AudioGUI
    {
        public AudioSource  audioSrc;
        public AudioClip    clip_nullClick;
        public AudioClip    clip_attackClick;
    }

    //==============  CursorMods  =================================================//
    [System.Serializable]
    public struct CursorMods
    {
        public Texture2D cursorTexture;
    }
    
    //==============  Struct Audio  =================================================//
    [System.Serializable]
    public struct ScrollAudio
    {
        public AudioClip scrollOpen;
        public AudioClip scrollClose;
        public AudioClip ding_confirm;
        public AudioClip ding2;
        public AudioClip ding_selected;

        public AudioSource scrollAudioSrc;
    }

    //==============  Struct BuildingVars  =================================================//
    [System.Serializable]
    public struct BuildingVars
    {
        public BuildingAsset    currentBuildAsset;
        public GameObject       currentBuildObj;
        public Transform        currentNode;
        public AudioSource      buildAudioSrc;
        public BuildPhase       buildPhase;
        public Transform        hierarchy_buildings;
    }

    void OnMouseEnter()
    {
        Cursor.SetCursor(cursorMods.cursorTexture, Vector2.zero, CursorMode.Auto);
    }

    void OnMouseExit()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}