using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ResourceSys
{
    [System.Serializable]
    public struct Resources
    {
        public int meat;
        public int wood;
        public int stone;
        public int metal;
        public int gold;
    }

    public static Resources ResourceAddSubtract(Resources a, Resources b)
    {
        a.meat += b.meat;
        a.wood += b.wood;
        a.stone += b.stone;
        a.metal += b.metal;
        a.gold += b.gold;

        return a;
    }
}
