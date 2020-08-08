using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HUD : MonoBehaviour
{
    //=============  Variables  ===========================// Currently used exclusively for constructing defenses
    public              Scroll          scroll;
    public              ScrollAudio     scrollAudio;
    public              AudioGUI        audioGUI;
    [SerializeField]    HUD_Building.BuildingVars           buildingVars;
    [SerializeField]    LayerMask       layerMaskGround;        //Used to place buildings
    [SerializeField]    LayerMask       layerMaskNotGround;     //Used to check building placement overlaps
    [SerializeField]    LayerMask       layerMaskUnitsAndBuildings;
    [SerializeField]    LayerMask       layerMaskGroundAndUnits;

    [System.NonSerialized]
    public              Building        activeBuilding;

    [SerializeField]    Transform       hierarchy_units;
    public              ClickManager.dblClickSettings clicks;
                        Unit.Team       playerTeam = Unit.Team.human;
                        bool            overlap;
                        AudioSource     audioSrcGUI;
                        List<Unit>      selectedUnits = new List<Unit>();
                        Transform       protoFormationLocRot;
    public              List<GameObject>    protoFormation = new List<GameObject>();
    public              Unit_Squad.Formation    formation;
    public HUD_SelectionBox.SelectionBoxVars    selectionBoxVars;

    [System.NonSerialized]
    public HUD_AdvancedWalling.ProtoWall    protoWallAdvanced = null;

    //=============  Function - Start()  ===========================//
    private void Start()
    {
        audioGUI.audioSrc = this.GetComponent<AudioSource>();

        scroll.scrollButtons.scrollButtonOpen.GetComponent<HUD_button>().hudScript = this;
        for (int i = 0; i < scroll.scrollButtons.scrollButtons.Count; i++)
            scroll.scrollButtons.scrollButtons[i].GetComponent<HUD_button>().hudScript = this;

        scrollAudio.scrollAudioSrc = scroll.GetComponent<AudioSource>();

        protoFormationLocRot = new GameObject().transform;
        protoFormationLocRot.name = "ProtoFormationLocRot";
        protoFormationLocRot.parent = this.transform;

        if (formation.columns == 0) formation.columns = 5;
    }

    //=============  Function - Update()  ===========================// Used for selecting buildings or units, or placing defenses
    private void Update()
    {
        HUD_UnitSelection.RemoveDeadUnitsFromSelection(selectedUnits);

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            clicks = ClickManager.CheckForDblClick(clicks);

        if (buildingVars.buildPhase == HUD_Building.BuildPhase.none)
            HUD_UnitSelection.UpdateSelection(this, clicks, layerMaskUnitsAndBuildings, layerMaskGround, playerTeam, selectedUnits, protoFormationLocRot, hierarchy_units, audioGUI);

        else if (buildingVars.buildPhase == HUD_Building.BuildPhase.advancedWalling)
            HUD_AdvancedWalling.WallingUpdate(protoWallAdvanced, audioGUI);

        else
            buildingVars = HUD_Building.BuildUpdate(buildingVars, overlap, layerMaskGround, scrollAudio, audioGUI, layerMaskNotGround);

        clicks.dblClick = false;
    }
    
    //=============  Function - ClickedOnScrollBuildButton()  ===============================//Called by HUD_button
    public void ClickedOnScrollBuildButton(BuildingAsset buildingAsset)
    {
        if (scroll.finishedRavelUnravel)
            buildingVars = scroll.ClickedOnBuild(buildingAsset, buildingVars, scrollAudio, audioGUI);
    }

    //=============  Function - ClickedOnBuildingReplaceScroll()  ===========================//Called by HUD_button
    public void ClickedOnReplaceScroll(GameObject replacementScroll)
    {
        scroll = Scroll.ReplaceScroll(scroll, replacementScroll, this);
        
        scrollAudio.scrollAudioSrc = scroll.GetComponent<AudioSource>();

        scroll.ClickedOnScroll(scrollAudio);
    }

    //=============  Function - ClickedOnScrollTrainUnit()  =================================// Called by HUD_button
    public void ClickedOnScrollTrainUnit(UnitAsset unitAsset)
    {
        if (scroll.finishedRavelUnravel)
            if (Time.time - activeBuilding.unitTrainingVars.lastSpawnTime > activeBuilding.unitTrainingVars.spawnInterval)
            {
                scrollAudio.scrollAudioSrc.clip = audioGUI.click_whoosh;
                scrollAudio.scrollAudioSrc.Play();


                activeBuilding.unitTrainingVars.lastSpawnTime = Time.time;

                Building.TrainUnit(activeBuilding.unitTrainingVars.SpawnWaypoints[0], unitAsset.unitObj, hierarchy_units, formation, activeBuilding.transform, activeBuilding, activeBuilding.squad);
            }
    }
    
    //=============  Function - ClickedOnScrollTrainUnit()  =================================// Called by HUD_button
    public void ClickedOnWalling()
    {
        if (buildingVars.buildPhase != HUD_Building.BuildPhase.advancedWalling) {
            protoWallAdvanced = new HUD_AdvancedWalling.ProtoWall();
            HUD_AdvancedWalling.CreateStarterBezierUI(protoWallAdvanced);

            buildingVars.buildPhase = HUD_Building.BuildPhase.advancedWalling;
        }
        else {
            buildingVars.buildPhase = HUD_Building.BuildPhase.none;
            protoWallAdvanced = null;
            GameObject.Find("World - MeshGen").GetComponent<MeshGen_BezierWall>().GenerateNextWall();
            Debug.Log("Shutting down AdvancedWalling");
        }
    }

    //=============  Struct - AudioGUI  =================================================//
    [System.Serializable]
    public class AudioGUI
    {
        [System.NonSerialized]
        public AudioSource  audioSrc;
        public AudioClip    click_null;
        public AudioClip    click_attack;
        public AudioClip    click_whoosh;
        public AudioClip    click_confirm;
        public AudioClip    click_selected;
    }

    //==============  Struct - ScrollAudio  =================================================//
    [System.Serializable]
    public class ScrollAudio
    {
        public AudioClip scrollOpen;
        public AudioClip scrollClose;

        public AudioSource scrollAudioSrc;
    }

    //==============  OnGUI  ========================================================//
    private void OnGUI()
    {
        if (selectionBoxVars.p1 != Vector3.zero)
        {
            Rect rect = Utils.GetScreenRect(selectionBoxVars.p1, Input.mousePosition);
            Utils.DrawScreenRect(rect, new Color(0.8f, 0.8f, 0.8f, 0.25f));
            Utils.DrawScreenRectBorder(rect, 2, new Color(0.8f, 0.8f, 0.8f));
        }
    }

    //==============  OnTriggerEnter  =================================================//
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Unit>() != null && !other.GetComponent<Unit>().selected)
        {
            if (
                    other.GetComponent<Unit>().type == Unit.UnitType.SOLDIER && other.GetComponent<Unit>().combatUnitVars.squad.team == playerTeam
                    || other.GetComponent<Unit>().type == Unit.UnitType.WAGON && other.GetComponent<Unit>().wagonUnitVars.team == playerTeam
                    || other.GetComponent<Unit>().type == Unit.UnitType.WORKER && other.GetComponent<Unit>().workerUnitVars.team == playerTeam
                )
                selectedUnits.Add(other.GetComponent<Unit>());

            other.GetComponent<Unit>().SelectUnit(other.GetComponent<Unit>().type);
        }
    }
}