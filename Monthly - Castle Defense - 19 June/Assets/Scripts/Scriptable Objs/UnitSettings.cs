using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class UnitSettings : ScriptableObject
{
    public AudioClips audioClips;

    [System.Serializable]
    public struct AudioClips
    {
        public AudioClip clip_GUI_Selected;
        public AudioClip clip_GUI_Attack;
        public AudioClip clip_Running;
        public AudioClip clip_Vocal_Attack;
        public AudioClip clip_Vocal_Die;
    }
}
