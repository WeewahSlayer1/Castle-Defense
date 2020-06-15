using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUD_button : MonoBehaviour
{
    public HUD parentScript;
    public BuildingAsset buildingAsset;

    //===============  ClickedOnBuild  ================//
    public void ClickedOnBuild()
    {
        if (buildingAsset != null)
            parentScript.ClickedOnBuild(buildingAsset);
        else
            Debug.Log("ERROR: " + this.name + " has no attached buildingAsset");
    }

    //==============  ClickedOnScroll  ================//
    public void ClickedOnScroll()
    {
        parentScript.ClickedOnScroll();
    }
}
