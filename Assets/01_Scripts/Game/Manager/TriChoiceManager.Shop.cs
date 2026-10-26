using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Manager;

namespace TrainDefense.Game
{
    // 역 상점(ShopUI.offerSlotCount칸 랜덤 판매) 후보 관리.
    // 판매 품목 = 포탑 구매(Add) / 보유 포탑 스탯 강화(TrainStatUpgrade) / 엘리트 승격(Elite) / 전체 포탑 아이템(StatUpgradeChoice, 상점 풀).
    // 가격 책정은 상점(ShopOfferPricing)의 책임 — 여기서는 후보 추첨만 담당한다.
    public partial class TriChoiceManager
    {
        /// <summary>런 세이브가 남기는 상점 품목 한 칸 — 선택지 id와, 포탑 구매·승격 카드에 부여된 랜덤 스킬 id(없으면 null).</summary>
        public readonly struct ShopChoiceSnapshot
        {
            public readonly string ChoiceId;
            public readonly string SkillId;

            public ShopChoiceSnapshot(string choiceId, string skillId)
            {
                ChoiceId = choiceId;
                SkillId = skillId;
            }
        }

        // 지금 상점에 진열된 품목(마지막 GetShopChoices 결과). 런 세이브가 진열을 그대로 남기는 데 읽는다.
        private readonly List<string> _currentShopChoiceIds = new();
        // 이어하기로 되살릴 진열. 다음 GetShopChoices가 한 번 소비한다.
        private List<ShopChoiceSnapshot> _pendingShopChoiceSnapshots;

        public IReadOnlyList<string> CurrentShopChoiceIds => _currentShopChoiceIds;

        /// <summary>상점에서 팔린 품목을 진열 기록에서 빈칸("")으로 바꾼다 — 이어하기 뒤 같은 카드가 다시 진열되지 않게.</summary>
        public void MarkShopChoiceSold(string choiceId)
        {
            int index = _currentShopChoiceIds.IndexOf(choiceId);

            if (index >= 0)
                _currentShopChoiceIds[index] = string.Empty;
        }

        /// <summary>이어하기 복원 — 다음 상점 추첨에서 저장 당시 품목을 그대로 돌려준다.</summary>
        public void RestoreShopChoices(List<ShopChoiceSnapshot> snapshots)
        {
            _pendingShopChoiceSnapshots = snapshots;
        }

        // 스탯 강화 후보의 가중치는 등급 데이터(stat_upgrade_tier_data.weight, 고등급 = 희귀)가 정한다.
        // 포탑 구매는 DB addTrainChoices에 직렬화된 Weight(7 = 열린 티어 가중치 합과 등가)를 쓰고,
        // 엘리트 승격은 추첨하지 않는다 — 승격 조건(강화 카드 등급의 합)을 넘긴 포탑이 있으면 한 장을 반드시 낸다.
        // 비율을 바꿀 때는 DB(엑셀) 값과 등급 weight를 함께 맞춰야 하며, ChoiceEntry.Weight가 int라
        // 소수 비율은 10배 등으로 스케일해서 표현해야 한다.

        // 역 상점용: 승격 카드(조건을 넘긴 포탑이 있으면 1장)를 먼저 넣고, 나머지 칸을 판매 후보에서 랜덤으로 뽑는다.
        public List<ChoiceEntry> GetShopChoices(int count)
        {
            List<ChoiceEntry> result = new();

            var eliteChoices = _GetShopEliteTrainChoices();
            var candidates = _GetShopCandidates();

            // 이어하기 직후의 첫 추첨은 저장 당시 진열을 되찾는다. 빈 칸(후보에서 사라진 품목)만 아래서 평소처럼 채운다.
            bool isRestored = _pendingShopChoiceSnapshots != null;

            if (isRestored)
            {
                _FillFromSnapshots(result, eliteChoices, candidates, count);
                _pendingShopChoiceSnapshots = null;
            }

            if (!isRestored && count > 0 && eliteChoices.Count > 0)
                _AddChoiceToResult(result, eliteChoices);

            // 복원한 진열의 팔린 칸(null)도 한 칸으로 센다 — 새로 채우면 산 만큼 품목이 늘어난다.
            int filledCount = result.Count;

            for (int i = filledCount; i < count; i++)
            {
                if (!_AddChoiceToResult(result, candidates))
                    break;
            }

            _currentShopChoiceIds.Clear();

            foreach (var entry in result)
                _currentShopChoiceIds.Add(entry?.Option != null ? entry.Option.Id : string.Empty);

            return result;
        }

        // 세이브에 남긴 품목을 같은 후보 풀에서 id로 다시 찾는다. 복원된 편성·강화 레벨이 저장 당시와 같으므로 풀도 같다.
        private void _FillFromSnapshots(List<ChoiceEntry> result, List<ChoiceEntry> eliteChoices, List<ChoiceEntry> candidates, int count)
        {
            foreach (var snapshot in _pendingShopChoiceSnapshots)
            {
                if (result.Count >= count)
                    break;

                // 팔린 칸: 자리만 지키는 빈 항목(상점 UI가 SetEmpty로 비운다)
                if (string.IsNullOrEmpty(snapshot.ChoiceId))
                {
                    result.Add(null);
                    continue;
                }

                var entry = eliteChoices.FirstOrDefault(e => e.Option.Id == snapshot.ChoiceId)
                            ?? candidates.FirstOrDefault(e => e.Option.Id == snapshot.ChoiceId);

                if (entry == null)
                {
                    // 복합 스탯 카드는 열 때마다 조합이 바뀌어 풀에 없다 — id로 같은 조합을 다시 만든다.
                    var statChoice = TrainStatUpgradeChoice.TryCreateFromId(snapshot.ChoiceId);

                    if (statChoice == null || !statChoice.IsValid())
                        continue;

                    entry = new ChoiceEntry { Option = statChoice, Weight = _GetStatUpgradeWeight(statChoice), Tier = 0 };
                }

                result.Add(entry);
                _RestoreSelectedSkill(entry.Option, snapshot.SkillId);
            }
        }

        // 포탑 구매 후보는 레벨업 카드와 동일한 필터(_GetAddTrainChoices: 미보유·스킬트리 언락·Tier 게이트)를 재사용한다.
        private List<ChoiceEntry> _GetShopCandidates()
        {
            List<ChoiceEntry> result = new();

            if (TrainManager.Instance == null)
                return result;

            if (!TrainManager.Instance.IsMaxTrainCountReached())
                result.AddRange(_GetAddTrainChoices());

            result.AddRange(_GetTrainStatUpgradeChoices());
            result.AddRange(_GetShopItemChoices(result.Sum(entry => entry.Weight)));

            return result;
        }

        // 아이템 전체의 추첨 가중치 = 포탑·강화 후보 가중치 합 × 이 비율. 1/3이면 칸당 25% → 랜덤 4칸에 평균 1장.
        // 상수 한 장 값으로 두면 포탑이 늘어 강화 후보가 불어날수록 아이템이 묻히므로 비율로 둔다.
        private const float SHOP_ITEM_TOTAL_WEIGHT_RATIO = 1f / 3f;

        // 전체 포탑 아이템(UpgradeData 중 상점 풀) — 중첩 상한(maxUpgradeCount) 전까지 반복 판매.
        // 등장 시점은 아이템 등급의 등급표 행(firstShopVisit)을 따른다 — 강화 카드와 같은 역부터 같은 등급이 열린다.
        // 적용은 레벨업 카드와 같은 StatUpgradeSelectEvent 파이프라인(전 포탑 적용·새 포탑 승계·세이브)을 그대로 탄다.
        private static List<ChoiceEntry> _GetShopItemChoices(int otherCandidatesTotalWeight)
        {
            List<ChoiceEntry> result = new();

            var databaseManager = DatabaseManager.Instance;

            if (databaseManager == null)
                return result;

            var stageManager = StageManager.Instance;
            int shopVisitCount = stageManager != null ? stageManager.TotalInspectionPassedCount : 0;

            foreach (var upgradeData in databaseManager.GetUpgradeDatas())
            {
                if (upgradeData.UpgradePool != UpgradePool.ShopItem)
                    continue;

                var tier = databaseManager.GetStatUpgradeTier(upgradeData.Grade);

                if (tier != null && !tier.IsAvailableAt(shopVisitCount))
                    continue;

                var choice = new StatUpgradeChoice(upgradeData);

                if (choice.IsValid())
                    result.Add(new ChoiceEntry { Option = choice, Tier = 0 });
            }

            if (result.Count == 0)
                return result;

            int weightPerItem = Mathf.Max(1, Mathf.RoundToInt(otherCandidatesTotalWeight * SHOP_ITEM_TOTAL_WEIGHT_RATIO / result.Count));

            foreach (var entry in result)
                entry.Weight = weightPerItem;

            return result;
        }

        // 조건을 넘긴 포탑이 여럿일 때 어느 포탑의 승격 카드를 낼지 고르는 포탑 하나의 가중치. 포탑끼리는 같은 확률.
        private const int ELITE_OFFER_TURRET_WEIGHT = 2;

        // 복합 카드가 같은 티어 단일 카드 대비 얼마나 자주 뜨는가. 1이면 단일 카드가 밀려나므로 절반으로 둔다.
        private const float COMBO_WEIGHT_RATIO = 0.5f;

        // 스탯 카드 추첨 가중치 — 등급 데이터가 정한다(고등급 = 희귀). 복합은 같은 티어 단일의 절반.
        // 등급마다 등장 구간 안에서 가중치가 변한다(저등급은 뒤로 갈수록 덜, 고등급은 더) — 그래서 방문 수가 필요하다.
        private static int _GetStatUpgradeWeight(TrainStatUpgradeChoice option)
        {
            var stageManager = StageManager.Instance;
            int shopVisitCount = stageManager != null ? stageManager.TotalInspectionPassedCount : 0;
            int finalShopVisit = stageManager != null ? stageManager.FinalStationCount : 0;
            int tierWeight = option.DisplayTier.GetWeightAt(shopVisitCount, finalShopVisit);

            return option.IsCombo ? Mathf.Max(1, Mathf.RoundToInt(tierWeight * COMBO_WEIGHT_RATIO)) : tierWeight;
        }

        // 승격 조건은 카드마다 원본 포탑 기준으로 판정한다(EliteTrainChoice.IsValid).
        // DB 엔트리는 ScriptableObject 원본이라 가중치를 직접 고치지 않고 복사본에 담는다.
        // 같은 포탑에 승격 카드가 여러 장(예: 기관총 61001·61001a)이면 가중치를 나눠 포탑마다 같은 빈도로 뜨게 한다.
        private List<ChoiceEntry> _GetShopEliteTrainChoices()
        {
            List<ChoiceEntry> result = new();

            float turretWeight = ELITE_OFFER_TURRET_WEIGHT;

            var eliteEntries = _GetEliteTrainChoices();

            foreach (var entry in eliteEntries)
            {
                var eliteChoice = (EliteTrainChoice)entry.Option;
                int sameTurretEntryCount = eliteEntries.Count(other => ((EliteTrainChoice)other.Option).BaseTrainId == eliteChoice.BaseTrainId);

                result.Add(new ChoiceEntry
                {
                    Option = entry.Option,
                    Weight = Mathf.Max(1, Mathf.RoundToInt(turretWeight / sameTurretEntryCount)),
                    Tier = entry.Tier
                });
            }

            return result;
        }

        // 보유 포탑별 "원하는 스탯 강화" 선택지(상점 전용).
        private List<ChoiceEntry> _GetTrainStatUpgradeChoices()
        {
            List<ChoiceEntry> result = new();

            var mainTrain = TrainManager.Instance?.MainTrain;

            if (mainTrain == null)
                return result;

            foreach (var train in mainTrain.CurrentTrains)
            {
                foreach (var option in TrainStatUpgradeChoice.CreateOptionsFor(train))
                {
                    if (option.IsValid())
                        result.Add(new ChoiceEntry { Option = option, Weight = _GetStatUpgradeWeight(option), Tier = 0 });
                }

                // 스탯 두 개가 한 장에 담긴 복합 카드. 가중치가 같은 티어 단일의 절반이라 가끔 섞여 나온다.
                foreach (var option in TrainStatUpgradeChoice.CreateComboOptionsFor(train))
                {
                    if (option.IsValid())
                        result.Add(new ChoiceEntry { Option = option, Weight = _GetStatUpgradeWeight(option), Tier = 0 });
                }
            }

            return result;
        }
    }
}
