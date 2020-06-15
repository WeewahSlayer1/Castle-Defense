using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class BuildingAsset : ScriptableObject
{
    public GameObject   BuildingObj;
    public Material     Mat_Proto;
    public Material     Mat_Final;
    public Cost         cost;
    public Wall         wall;

    [System.Serializable]
    public struct Cost
    {
        public int wood;
        public int stone;
        public int metal;
        public int food;
    }

    [System.Serializable]
    public struct Wall
    {
        public GameObject   wallObj;
        public float        wallLength;
        public float        wallOffset;
    }
}
