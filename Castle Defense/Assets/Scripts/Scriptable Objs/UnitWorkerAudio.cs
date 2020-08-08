using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class UnitWorkerAudio : ScriptableObject
{
    public AudioClips audioClips;

    [System.Serializable]
    public struct AudioClips
    {
        public AudioClip clip_GUI_Selected;
        public AudioClip clip_Moving;
        public AudioClip clip_Vocal_Die;
    }
}