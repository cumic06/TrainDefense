using System;
using UnityEngine;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 강화 상품 등급 하나. 상점 슬롯 수가 제약이라 후반에는 고등급을 살 수밖에 없는데,
    /// 가격 배수를 효과 배수보다 가파르게 두어 고등급일수록 1원당 효율이 떨어지게 만든다
    /// (수입이 그대로 성장으로 환전되는 것을 막는 브레이크).
    /// 등장 구간은 누적 상점 방문 수(StageManager.TotalInspectionPassedCount) 기준.
    /// ChoiceData.xlsx의 stat_upgrade_tier_data 시트에서 임포트한다.
    /// </summary>
    [Serializable]
    public class StatUpgradeTierData : IData
    {
        [SerializeField]
        private string id;

        [SerializeField]
        [Tooltip("표시용 등급 번호(1부터). 밸런스 계산에는 쓰이지 않는다")]
        private int grade = 1;

        [SerializeField]
        [Tooltip("기본 증가량에 곱해지는 배수")]
        private float valueMultiplier = 1f;

        [SerializeField]
        [Tooltip("기본 가격에 곱해지는 배수. 효과 배수보다 크게 두면 고등급일수록 1원당 효율이 떨어진다")]
        private float costMultiplier = 1f;

        [SerializeField]
        [Tooltip("이 등급이 상점에 나오기 시작하는 누적 상점 방문 수")]
        private int firstShopVisit = 1;

        [SerializeField]
        [Tooltip("이 등급이 마지막으로 나오는 누적 상점 방문 수. 0 이하면 판이 끝날 때까지 계속 나온다")]
        private int lastShopVisit;

        public string Id => id;
        public int Grade => grade;
        public float ValueMultiplier => valueMultiplier;
        public float CostMultiplier => costMultiplier;

        public bool IsAvailableAt(int shopVisitCount)
        {
            if (shopVisitCount < firstShopVisit)
                return false;

            return lastShopVisit <= 0 || shopVisitCount <= lastShopVisit;
        }
    }

    /// <summary>
    /// 스탯 한 종류의 공통 설정 — 몇 등급부터 상점에 나오는지.
    /// 강력한 정수 스탯(대상 수·공격 횟수)을 고등급으로 밀어 비싸게 만드는 장치다.
    /// StatUpgradeData.xlsx의 stat_upgrade_stat_data 시트에서 임포트한다.
    /// </summary>
    [Serializable]
    public class StatUpgradeStatData : IData
    {
        [SerializeField]
        private string id;

        [SerializeField]
        private StatType statType;

        [SerializeField]
        [Tooltip("이 스탯이 나오기 시작하는 등급. 정수 스탯은 이 등급에서만 나온다(증가량이 +1 고정이라 상위 등급은 비싸기만 하다)")]
        private int minGrade = 1;

        public string Id => id;
        public StatType StatType => statType;
        public int MinGrade => minGrade;
    }

    /// <summary>
    /// 포탑 하나가 상점에서 강화할 수 있는 스탯 한 종류와 그 증가율.
    /// 이 목록에 없는 스탯은 그 포탑의 강화 카드로 뜨지 않는다(포탑별 스탯 풀 = 데이터).
    /// ChoiceData.xlsx의 train_stat_upgrade_data 시트에서 임포트한다.
    /// </summary>
    [Serializable]
    public class TrainStatUpgradeRuleData : IData
    {
        [SerializeField]
        private string id;

        [SerializeField]
        [Tooltip("대상 포탑 데이터 ID (엘리트는 따로 적어야 한다)")]
        private string trainDataId;

        [SerializeField]
        [Tooltip("강화할 스탯")]
        private StatType statType;

        [SerializeField]
        [Tooltip("1등급 기준 증가율 — base 대비. 정수 스탯(대상 수·공격 횟수)은 등급 배수가 곧 증가 개수라 쓰이지 않는다")]
        private float increaseRate;

        public string Id => id;
        public string TrainDataId => trainDataId;
        public StatType StatType => statType;
        public float IncreaseRate => increaseRate;
    }
}
