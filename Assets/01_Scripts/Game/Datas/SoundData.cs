using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class SoundData : IData
    {
        [SerializeField]
        [BoxGroup("IDs")]
        private string id; // Excel ID

        [SerializeField]
        [BoxGroup("IDs")]
        private string clipId; // DB / Asset ID

        [SerializeField]
        [BoxGroup("Identification")]
        private SoundType soundType;

        [SerializeField]
        [BoxGroup("Resource")]
        private AudioClip clip;

        [Range(0f, 1f)]
        [SerializeField]
        private float volume = 1f;

        public string Id => id;
        public string ClipId => clipId;
        public SoundType SoundType => soundType;
        public AudioClip Clip => clip;
        public float Volume => volume;
    }
}
