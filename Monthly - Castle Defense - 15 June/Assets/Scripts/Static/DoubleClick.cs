using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DoubleClick
{
    public static dblClickSettings UpdateDblClick(dblClickSettings dblClickS)
    {
        //If previous firstClick expired, set to this click
        if (Time.time - dblClickS.firstClick >= dblClickS.clickInterval)
            dblClickS.firstClick = Time.time;
        else
            dblClickS.dblClick = true;

        return dblClickS;
    }

    [System.Serializable]
    public struct dblClickSettings
    {
        public float clickInterval;

        [System.NonSerialized]
        public float firstClick;

        [System.NonSerialized]
        public float clickTimer;

        [System.NonSerialized]
        public bool dblClick;
    }
}
