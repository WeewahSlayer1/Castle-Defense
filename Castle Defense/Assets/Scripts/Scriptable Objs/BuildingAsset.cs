using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class BuildingAsset : ScriptableObject
{
    public GameObject               buildingObj;
    public Material                 mat_Proto;
    public Material                 mat_Final;
    public ResourceSys.Resources    cost;
    public Wall                     wall;
    
    [System.Serializable]
    public struct Wall
    {
        public GameObject   wallObj;
        public float        wallLength;
        public float        wallOffset;
    }
}
