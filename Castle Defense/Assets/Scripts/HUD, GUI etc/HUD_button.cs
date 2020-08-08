using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUD_button : MonoBehaviour
{
    public HUD hudScript;
    public BuildingAsset buildingAsset;
    public UnitAsset unitAsset;

    //===============  ClickedOnBuild  ================//
    public void ClickedOnBuild()
    {
        if (buildingAsset != null)
            hudScript.ClickedOnScrollBuildButton(buildingAsset);
        else
            Debug.Log("ERROR: " + this.name + " has no attached buildingAsset");
    }

    //==============  ClickedOnScroll  ================//
    public void ClickedOnScroll()
    {
        hudScript.scroll.ClickedOnScroll(hudScript.scrollAudio);
    }

    //==============  ClickedOnScroll  ================//
    public void ClickedOnAdvancedWalling()
    {
        hudScript.ClickedOnWalling();
    }
    
    //===============  ClickedOnBuild  ================//
    public void ClickedOnUnit()
    {
        if (unitAsset != null)
            hudScript.ClickedOnScrollTrainUnit(unitAsset);
        else
            Debug.Log("ERROR: " + this.name + " has no attached unitAsset");
    }
}
