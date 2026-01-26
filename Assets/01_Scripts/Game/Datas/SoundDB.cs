using System;
using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace TrainDefense.Game.Datas
{
    [CreateAssetMenu(fileName = "SoundDB", menuName = "Data/SoundDB")]
    public class SoundDB : ScriptableObject
    {
        [SerializeField]
        [TableList]
        private List<SoundData> soundDataList = new();

        public IReadOnlyList<SoundData> SoundDataList => soundDataList;

        private Dictionary<string, SoundData> _idCache;
        private Dictionary<string, SoundData> _clipIdCache;
        private Dictionary<SoundType, SoundData> _typeCache;

        public void Initialize()
        {
            _idCache = new Dictionary<string, SoundData>();
            _clipIdCache = new Dictionary<string, SoundData>();
            _typeCache = new Dictionary<SoundType, SoundData>();

            foreach (var data in soundDataList)
            {
                if (!string.IsNullOrEmpty(data.Id) && !_idCache.ContainsKey(data.Id))
                    _idCache.Add(data.Id, data);

                if (!string.IsNullOrEmpty(data.ClipId) && !_clipIdCache.ContainsKey(data.ClipId))
                    _clipIdCache.Add(data.ClipId, data);

                if (data.SoundType != SoundType.None && !_typeCache.ContainsKey(data.SoundType))
                    _typeCache.Add(data.SoundType, data);
            }
        }

        public SoundData GetSoundData(string id)
        {
            if (_idCache == null) Initialize();
            return _idCache.TryGetValue(id, out var data) ? data : null;
        }

        public SoundData GetSoundDataByClipId(string clipId)
        {
            if (_clipIdCache == null) Initialize();
            return _clipIdCache.TryGetValue(clipId, out var data) ? data : null;
        }

        public SoundData GetSoundData(SoundType type)
        {
            if (_typeCache == null) Initialize();
            return _typeCache.TryGetValue(type, out var data) ? data : null;
        }
    }
}
