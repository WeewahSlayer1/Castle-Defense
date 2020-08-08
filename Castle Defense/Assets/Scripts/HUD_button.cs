using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUD_button : MonoBehaviour
{
    public HUD HUDscript;
    public BuildingAsset buildingAsset;
    public UnitAsset unitAsset;

    //===============  ClickedOnBuild  ================//
    public void ClickedOnBuild()
    {
        if (buildingAsset != null)
            HUDscript.ClickedOnScrollBuildButton(buildingAsset);
            //parentScript.scroll.ClickedOnBuild(buildingAsset);
        else
            Debug.Log("ERROR: " + this.name + " has no attached buildingAsset");
    }

    //==============  ClickedOnScroll  ================//
    public void ClickedOnScroll()
    {
        HUDscript.scroll.ClickedOnScroll(HUDscript.scrollAudio);
    }
    
    //===============  ClickedOnBuild  ================//
    public void ClickedOnUnit()
    {
        if (unitAsset != null)
            HUDscript.ClickedOnScrollTrainUnit(unitAsset);
        else
            Debug.Log("ERROR: " + this.name + " has no attached unitAsset");
    }
}
