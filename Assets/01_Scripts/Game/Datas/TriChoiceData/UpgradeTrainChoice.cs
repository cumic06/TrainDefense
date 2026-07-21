using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// (구) 고정 업그레이드 트리 선택지 — 레벨업↔상점 리워크로 폐기됨.
    /// 상점의 포탑 강화는 TrainStatUpgradeChoice(원하는 스탯 강화)가 대체한다.
    /// ★ TriChoiceDB에 [SerializeReference]로 직렬화돼 있어 클래스·필드를 지우면
    /// DB 에셋 디시리얼라이즈가 깨진다 → 필드는 보존하고 동작만 무효화한 스텁.
    /// </summary>
    [Serializable]
    public class UpgradeTrainChoice : IChoiceOption
    {
        #region Fields
        [SerializeField]
        private string id;

        [SerializeField]
        [Tooltip("업그레이드 대상 Train ID")]
        private string targetTrainId;

        [SerializeField]
        [Tooltip("업그레이드 데이터 목록 (가중치 기반)")]
        private WeightedUpgradeData[] weightedUpgrades;
        #endregion

        public string Id => id;
        public string TargetTrainId => targetTrainId;
        public WeightedUpgradeData[] WeightedUpgrades => weightedUpgrades;

        // 어떤 선택지 풀에도 후보로 오르지 않도록 항상 무효.
        public bool IsValid() => false;

        public void Execute()
        {
            Debug.LogWarning($"UpgradeTrainChoice [{id}]: 폐기된 선택지가 실행되었습니다. TrainStatUpgradeChoice로 대체되었습니다.");
        }
    }
}
