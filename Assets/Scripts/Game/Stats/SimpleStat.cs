using System;
using UnityEngine;

namespace TrainDefense.Game.Stats
{
    [Serializable]
    public class SimpleStat : IStat
    {
        [field: SerializeField]
        public StatType Type { get; set; }

        [field: SerializeField]
        public float Value { get; set; }

        public void Combine(IStat other)
        {
            if (other == null) return;
            if (other.Type != Type) return;
            if (Mathf.Approximately(other.Value, 0f)) return;

            Value += other.Value;
        }
    }
}