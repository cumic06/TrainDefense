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
        private string passiveType;
        [SerializeField]
        private string param1;
        [SerializeField]
        private string param2;
        [SerializeField]
        private string param3;

        public string Id => id;
        public string PassiveType => passiveType;
        public string Param1 => param1;
        public string Param2 => param2;
        public string Param3 => param3;

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
