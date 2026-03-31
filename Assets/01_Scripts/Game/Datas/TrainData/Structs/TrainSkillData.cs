using System;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 기차의 스킬 관련 데이터. Struct로 유지해서 ScriptableObject 원본이
    /// 런타임에 변경되지 않도록 합니다.
    /// Sprite 로딩은 TrainData 클래스에서 처리합니다.
    /// </summary>
    [Serializable]
    public struct TrainSkillData
    {
        [UnityEngine.SerializeField]
        private bool hasSkill;
        [UnityEngine.SerializeField]
        private float skillCooldown;
        [UnityEngine.SerializeField]
        private string skillIconId;

        public bool HasSkill => hasSkill;
        public float SkillCooldown => skillCooldown;
        public string SkillIconId => skillIconId;
    }
}
