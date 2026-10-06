using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Manager;

namespace TrainDefense.Game
{
    // 역 상점(ShopUI.offerSlotCount칸 랜덤 판매) 후보 관리.
    // 판매 품목 = 포탑 구매(Add) / 보유 포탑 스탯 강화(TrainStatUpgrade) / 엘리트 승격(Elite).
    // 가격 책정은 상점(ShopOfferPricing)의 책임 — 여기서는 후보 추첨만 담당한다.
    public partial class TriChoiceManager
    {
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

            if (count > 0 && eliteChoices.Count > 0)
                _AddChoiceToResult(result, eliteChoices);

            var candidates = _GetShopCandidates();

            for (int i = result.Count; i < count; i++)
            {
                if (!_AddChoiceToResult(result, candidates))
                    break;
            }

            return result;
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

            return result;
        }

        // 조건을 넘긴 포탑이 여럿일 때 어느 포탑의 승격 카드를 낼지 고르는 포탑 하나의 가중치. 포탑끼리는 같은 확률.
        private const int ELITE_OFFER_TURRET_WEIGHT = 2;

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
                        result.Add(new ChoiceEntry { Option = option, Weight = option.TierWeight, Tier = 0 });
                }

                // 스탯 두 개가 한 장에 담긴 복합 카드. 가중치가 같은 티어 단일의 절반이라 가끔 섞여 나온다.
                foreach (var option in TrainStatUpgradeChoice.CreateComboOptionsFor(train))
                {
                    if (option.IsValid())
                        result.Add(new ChoiceEntry { Option = option, Weight = option.TierWeight, Tier = 0 });
                }
            }

            return result;
        }
    }
}
