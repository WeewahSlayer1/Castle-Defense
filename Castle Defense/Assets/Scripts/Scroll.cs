using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scroll : MonoBehaviour
{
    public ScrollButtons scrollButtons;
    [SerializeField] Animator scrollAnim;
    bool unravelled;

    //=============  ClickedOnBuild()  ===================//
    public HUD.BuildingVars ClickedOnBuild(BuildingAsset bA, HUD.BuildingVars bS, HUD.ScrollAudio sA)
    {
        //---------------------  buildAsset, BuildObj  ---------------------------//
        bS.currentBuildAsset = bA;
        bS.currentBuildObj = Object.Instantiate(bA.buildingObj, Vector3.zero, Quaternion.identity, bS.hierarchy_buildings);

        //---------------------  Proto Material  ---------------------------//
        bS.currentBuildObj.GetComponent<Renderer>().material = bS.currentBuildAsset.mat_Proto;
        for (int i = 0; i < bS.currentBuildObj.transform.childCount; i++)
            bS.currentBuildObj.transform.GetChild(i).GetComponent<Renderer>().material = bS.currentBuildAsset.mat_Proto;

        //---------------------  Audio  ---------------------------//
        sA.scrollAudioSrc.clip = sA.ding_confirm;
        sA.scrollAudioSrc.Play();

        bS.buildAudioSrc = bS.currentBuildObj.AddComponent<AudioSource>();
        bS.buildAudioSrc.clip = sA.ding_selected;
        bS.buildAudioSrc.playOnAwake = false;
        bS.buildAudioSrc.Play();

        //---------------------  BuildPhase  ---------------------------//
        bS.buildPhase = HUD.BuildPhase.moving;

        //---------------------  EnableDisableScroll  ---------------------------//
        EnableDisableScroll(false, sA);

        return bS;
    }

    //=============  ClickedOnScroll()  ==================// Called by UI buttons
    public void ClickedOnScroll(HUD.ScrollAudio sA)
    {
        EnableDisableScroll(!unravelled, sA);
    }
    
    //=============  EnableDisableScroll()  ==============//
    void EnableDisableScroll(bool unravelOrRavel, HUD.ScrollAudio sA)
    {
        unravelled = unravelOrRavel;

        if (unravelled)
        {
            scrollButtons.scrollButtonOpen.SetActive(false);
            for (int i = 0; i < scrollButtons.scrollButtons.Count; i++)
                scrollButtons.scrollButtons[i].SetActive(true);

            sA.scrollAudioSrc.clip = sA.scrollOpen;
            sA.scrollAudioSrc.Play();
        }
        else
        {
            scrollButtons.scrollButtonOpen.SetActive(true);
            for (int i = 0; i < scrollButtons.scrollButtons.Count; i++)
                scrollButtons.scrollButtons[i].SetActive(false);

            sA.scrollAudioSrc.clip = sA.scrollClose;
            sA.scrollAudioSrc.Play();
        }

        scrollAnim.SetBool("Unravelled", unravelled);
    }
    
    //==============  Struct scroll buttons  =======================================//
    [System.Serializable]
    public struct ScrollButtons
    {
        public List<GameObject> scrollButtons;
        public GameObject scrollButtonOpen;
    }

}
