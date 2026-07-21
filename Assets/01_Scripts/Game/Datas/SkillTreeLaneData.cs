using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 스킬트리 레인(계열) 표기 데이터 — DB(SkillTree Data 탭)에서 관리한다.
    /// name은 로컬라이즈 키다 (SkillNodeData와 동일 방식 — 미등록 키면 문자열 그대로 표시).
    /// </summary>
    [Serializable]
    public class SkillTreeLaneData
    {
        #region Fields
        [SerializeField] private SkillTreeLane lane;
        [SerializeField]
        [Tooltip("레인 이름 로컬라이즈 키 (예: UI_SkillTree_Lane_Fire)")]
        private string name;
        #endregion

        public SkillTreeLane Lane => lane;
        public string Name => TrainDefense.Localize.LocalizeHelper.GetByKey(name, name);
    }
}
