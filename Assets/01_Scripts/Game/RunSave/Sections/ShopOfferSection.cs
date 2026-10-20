using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game.RunSave.Sections
{
    /// <summary>
    /// 상점 진열 섹션 — 역 도착 때 뽑힌 판매 품목과 카드에 부여된 랜덤 스킬.
    /// 저장이 역 도착(상점이 열린 직후)에 이뤄지므로, 이어하기 뒤 상점이 다시 열릴 때 저장 당시와 같은 품목이 보이게 한다.
    /// 품목 자체는 TriChoiceManager가 복원된 편성·강화 레벨로 다시 만든 후보 풀에서 id로 되찾는다.
    /// </summary>
    public class ShopOfferSection : IRunStateSection
    {
        public string SectionId => "shop_offers";

        [Serializable]
        private class Payload
        {
            public List<OfferEntry> offers = new();
        }

        [Serializable]
        private class OfferEntry
        {
            public string choiceId;
            // 포탑 구매·승격 카드에 붙은 랜덤 스킬. 스탯 카드는 비어 있다.
            public string skillId;
        }

        public string Capture()
        {
            var triChoiceManager = TriChoiceManager.Instance;

            if (triChoiceManager == null || triChoiceManager.CurrentShopChoiceIds.Count == 0)
                return null;

            var payload = new Payload();

            foreach (var choiceId in triChoiceManager.CurrentShopChoiceIds)
            {
                payload.offers.Add(new OfferEntry
                {
                    choiceId = choiceId,
                    skillId = triChoiceManager.GetCachedSkillId(choiceId),
                });
            }

            return JsonUtility.ToJson(payload);
        }

        public void Restore(string json)
        {
            var triChoiceManager = TriChoiceManager.Instance;

            if (triChoiceManager == null)
                return;

            var payload = JsonUtility.FromJson<Payload>(json);

            if (payload?.offers == null || payload.offers.Count == 0)
                return;

            var snapshots = new List<TriChoiceManager.ShopChoiceSnapshot>(payload.offers.Count);

            foreach (var offer in payload.offers)
                snapshots.Add(new TriChoiceManager.ShopChoiceSnapshot(offer.choiceId, offer.skillId));

            triChoiceManager.RestoreShopChoices(snapshots);
        }
    }
}
