using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class TrainPassiveSkillData : IData
    {
        [SerializeField]
        private string id;
        [SerializeField]
        private string name;
        [SerializeField]
        private string passiveType;
        [SerializeField]
        private string param1;
        [SerializeField]
        private string param2;
        [SerializeField]
        private string param3;
        [SerializeField]
        private string description;
        [SerializeField]
        [Tooltip("개조 해금 조건(개조 스킬에만). 한 판 안에서 그 포탑이 전부 만족하면 판이 끝날 때 해금돼 다음 판부터 상점에 뜬다. 비어 있으면 처음부터 해금")]
        private RemodelUnlockCondition[] unlockConditions;

        public string Id => id;
        public string Name => TrainDefense.Localize.LocalizeHelper.GetByKey(name, name);
        public string PassiveType => passiveType;
        public string Param1 => param1;
        public string Param2 => param2;
        public string Param3 => param3;
        public string Description => TrainDefense.Localize.LocalizeHelper.GetByKey(description, description).Replace("\\n", "\n");
        public RemodelUnlockCondition[] UnlockConditions => unlockConditions ?? Array.Empty<RemodelUnlockCondition>();
        /// <summary>해금 조건이 없는 개조 — 처음부터 상점에 뜰 수 있다.</summary>
        public bool IsUnlockedByDefault => UnlockConditions.Length == 0;

        public string ToDsl()
        {
            var dsl = passiveType;
            if (!string.IsNullOrEmpty(param1)) dsl += ":" + param1;
            if (!string.IsNullOrEmpty(param2)) dsl += ":" + param2;
            if (!string.IsNullOrEmpty(param3)) dsl += ":" + param3;
            return dsl;
        }
    }
}
