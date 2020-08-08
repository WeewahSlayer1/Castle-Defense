using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scroll : MonoBehaviour
{
    public ScrollButtons scrollButtons;
    [SerializeField] Animator scrollAnim;
    [System.NonSerialized]
    public bool unravelled;
    [SerializeField] bool fullOrJustHalf;

    [System.NonSerialized]
    public bool finishedRavelUnravel = true;

    //=============  ClickedOnBuild()  ===================//
    public HUD_Building.BuildingVars ClickedOnBuild(BuildingAsset bA, HUD_Building.BuildingVars bV, HUD.ScrollAudio sA, HUD.AudioGUI audioGUI)
    {
        //---------------------  buildAsset, BuildObj  ---------------------------//
        bV.currentBuildAsset = bA;
        bV.currentBuildObj = Object.Instantiate(bA.buildingObj, Vector3.zero, Quaternion.identity, bV.hierarchy_buildings);

        //---------------------  Proto Material  ---------------------------//
        bV.currentBuildObj.GetComponent<Renderer>().material = bV.currentBuildAsset.mat_Proto;
        for (int i = 0; i < bV.currentBuildObj.transform.childCount; i++)
            if (bV.currentBuildObj.transform.GetChild(i).GetComponent<Renderer>() != null)
                bV.currentBuildObj.transform.GetChild(i).GetComponent<Renderer>().material = bV.currentBuildAsset.mat_Proto;

        //---------------------  Audio  ---------------------------//
        sA.scrollAudioSrc.clip = audioGUI.click_whoosh;
        sA.scrollAudioSrc.Play();

        bV.buildAudioSrc = bV.currentBuildObj.AddComponent<AudioSource>();
        bV.buildAudioSrc.clip = audioGUI.click_selected;
        bV.buildAudioSrc.playOnAwake = false;
        bV.buildAudioSrc.Play();

        //---------------------  BuildPhase  ---------------------------//
        bV.buildPhase = HUD_Building.BuildPhase.basicMoving;

        return bV;
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

        scrollAnim.SetBool("Unravelled", unravelled);
        scrollAnim.SetBool("Full", fullOrJustHalf);
        World_GenericVars outlineMixer = GameObject.Find("World - Generic variables").GetComponent<World_GenericVars>();


        if (unravelled)
        {
            sA.scrollAudioSrc.clip = sA.scrollOpen;
            sA.scrollAudioSrc.Play();
            
            StartCoroutine(EnableDisableScroll_Mask(unravelOrRavel));

            outlineMixer.Mixing(1);
        }
        else
        {
            sA.scrollAudioSrc.clip = sA.scrollClose;
            sA.scrollAudioSrc.Play();

            StartCoroutine(EnableDisableScroll_Mask(unravelOrRavel));

            outlineMixer.Mixing(0);
        }
    }

    IEnumerator EnableDisableScroll_Mask(bool unravelOrRavel)
    {
        finishedRavelUnravel = false;

        float scrollWidth;
        if (fullOrJustHalf)
            scrollWidth = 900;
        else
            scrollWidth = 500;

        if (unravelOrRavel)
        {
            scrollButtons.scrollButtonOpen.SetActive(false);
            for (int i = 0; i < scrollButtons.scrollButtons.Count; i++)
                scrollButtons.scrollButtons[i].SetActive(true);

            yield return new WaitForSeconds(0.5f);

            float unravelSpeed = scrollButtons.maskUnravelSpeed;

            while (scrollButtons.mask.sizeDelta.x < scrollWidth)
            {
                if (scrollButtons.mask.sizeDelta.x >= 550)
                    unravelSpeed *= 20;

                yield return new WaitForSeconds(0.01f);
                scrollButtons.mask.sizeDelta = new Vector2(scrollButtons.mask.sizeDelta.x + 2 * unravelSpeed, scrollButtons.mask.sizeDelta.y);
                scrollButtons.mask.localPosition += Vector3.right * unravelSpeed;
                for (int i = 0; i < scrollButtons.scrollButtons.Count; i++)
                    scrollButtons.scrollButtons[i].GetComponent<RectTransform>().localPosition -= Vector3.right * unravelSpeed;
            }
        }
        else
        {
            float unravelSpeed = scrollButtons.maskUnravelSpeed * 20;

            if (!fullOrJustHalf)
                unravelSpeed *= 2;

            while (scrollButtons.mask.sizeDelta.x >= 0)
            {
                if (scrollButtons.mask.sizeDelta.x <= 550)
                    unravelSpeed  = scrollButtons.maskUnravelSpeed;

                yield return new WaitForSeconds(0.01f);
                scrollButtons.mask.sizeDelta = new Vector2(scrollButtons.mask.sizeDelta.x - 2 * unravelSpeed, scrollButtons.mask.sizeDelta.y);
                scrollButtons.mask.localPosition -= Vector3.right * unravelSpeed;
                for (int i = 0; i < scrollButtons.scrollButtons.Count; i++)
                    scrollButtons.scrollButtons[i].GetComponent<RectTransform>().localPosition += Vector3.right * unravelSpeed;
            }
            
            scrollButtons.scrollButtonOpen.SetActive(true);
            for (int i = 0; i < scrollButtons.scrollButtons.Count; i++)
                scrollButtons.scrollButtons[i].SetActive(false);
        }

        finishedRavelUnravel = true;
    }

    public static Scroll ReplaceScroll(Scroll scroll, GameObject replacementScroll, HUD hud)
    {
        if (scroll != null)
        {
            Vector3 scrollPos = scroll.transform.position;
            Quaternion scrollRot = scroll.transform.rotation;
            Transform scrollParent = scroll.transform.parent;

            Destroy(scroll.gameObject);

            scroll = Object.Instantiate(replacementScroll, scrollPos, scrollRot, scrollParent).GetComponent<Scroll>();
            scroll.name = replacementScroll.name;

            scroll.scrollButtons.scrollButtonOpen.GetComponent<HUD_button>().hudScript = hud;
            for (int i = 0; i < scroll.scrollButtons.scrollButtons.Count; i++)
                scroll.scrollButtons.scrollButtons[i].GetComponent<HUD_button>().hudScript = hud;
        }
        else
            Debug.Log("ERROR: our old scroll was deleted, so we don't know where to place the new one");

        return scroll;
    }

    //==============  Struct scroll buttons  =======================================//
    [System.Serializable]
    public struct ScrollButtons
    {
        public List<GameObject> scrollButtons;
        public GameObject scrollButtonOpen;

        public RectTransform mask;
        public float maskUnravelSpeed;
    }

}
