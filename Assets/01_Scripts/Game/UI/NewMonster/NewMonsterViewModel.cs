using UnityEngine;
using TrainDefense.Game.Datas;
using Cumic;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 새 몬스터 발견 UI의 ViewModel
    /// MonsterData에서 UI에 필요한 데이터를 제공합니다.
    /// </summary>
    public class NewMonsterViewModel
    {
        public string MonsterId { get; private set; }
        public string MonsterName { get; private set; }
        public Sprite Icon { get; private set; }

        public bool IsValid => !string.IsNullOrEmpty(MonsterId);

        public NewMonsterViewModel(string monsterId)
        {
            MonsterId = monsterId;
            LoadMonsterData(monsterId);
        }

        private void LoadMonsterData(string monsterId)
        {
            MonsterData monsterData = DatabaseManager.Instance.GetMonsterData(monsterId);

            if (monsterData == null)
            {
                Debug.LogWarning($"[NewMonsterViewModel] Monster data not found for ID: {monsterId}");
                return;
            }

            MonsterName = monsterData.Name;
            Icon = monsterData.Icon;
        }
    }
}
