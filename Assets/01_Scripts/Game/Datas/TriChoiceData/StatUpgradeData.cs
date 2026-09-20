using System;
using UnityEngine;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 강화 상품 등급 하나. 고등급 = 낮은 weight(희귀) + 낮은 1원당 가격 — 뜨면 이득인 희귀 상품.
    /// 수입-성장 환전의 브레이크는 가격이 아니라 희소성(weight)과 방문당 인상률이 담당한다.
    /// 등장 구간은 누적 상점 방문 수(StageManager.TotalInspectionPassedCount) 기준.
    /// StatUpgradeData.xlsx의 stat_upgrade_tier_data 시트에서 임포트한다.
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
        [Tooltip("기본 가격에 곱해지는 배수. 효과 배수보다 작게 두면 고등급일수록 1원당 가격이 싸진다(희귀 보상)")]
        private float costMultiplier = 1f;

        [SerializeField]
        [Tooltip("이 등급이 상점에 나오기 시작하는 누적 상점 방문 수")]
        private int firstShopVisit = 1;

        [SerializeField]
        [Tooltip("이 등급이 마지막으로 나오는 누적 상점 방문 수. 0 이하면 판이 끝날 때까지 계속 나온다")]
        private int lastShopVisit;

        [SerializeField]
        [Tooltip("등장 구간이 시작될 때의 상점 추첨 가중치 — 높을수록 자주 나온다. 창이 겹친 등급끼리의 노출 비율을 정한다(고등급 = 희귀)")]
        private int weight = 1;

        [SerializeField]
        [Tooltip("등장 구간이 끝날 때의 가중치. 시작값보다 낮게 두면 뒤로 갈수록 덜 나온다(저등급), 높게 두면 더 자주 나온다(고등급). 그 사이는 방문 수에 비례해 이어진다")]
        private int lastVisitWeight = 1;

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

        /// <summary>
        /// 이번 상점에서의 추첨 가중치. 등장 구간의 처음(weight)에서 끝(lastVisitWeight)까지 방문 수에 비례해 옮겨간다.
        /// 끝까지 나오는 등급(lastShopVisit 0)은 종착역을 구간 끝으로 본다.
        /// </summary>
        public int GetWeightAt(int shopVisitCount, int finalShopVisit)
        {
            int windowEnd = lastShopVisit > 0 ? lastShopVisit : finalShopVisit;
            if (windowEnd <= firstShopVisit)
                return weight;

            float progress = Mathf.Clamp01((float)(shopVisitCount - firstShopVisit) / (windowEnd - firstShopVisit));

            return Mathf.Max(0, Mathf.RoundToInt(Mathf.Lerp(weight, lastVisitWeight, progress)));
        }
    }

    /// <summary>
    /// 스탯 한 종류의 공통 설정 — 몇 등급부터 상점에 나오는지, 가격 프리미엄이 얼마인지.
    /// 강력한 정수 스탯(대상 수·공격 횟수)을 고등급으로 밀고 비싸게 만드는 장치다.
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

        [SerializeField]
        [Tooltip("기본 가격에 곱해지는 스탯 프리미엄. +1의 가치가 % 스탯보다 큰 정수 스탯을 비싸게 만든다. 1 = 프리미엄 없음")]
        private float costMultiplier = 1f;

        public string Id => id;
        public StatType StatType => statType;
        public int MinGrade => minGrade;
        public float CostMultiplier => costMultiplier;
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
