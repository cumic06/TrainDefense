using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class MonsterData : IDescribableData, IPrefabData
    {
        #region Fields
        [SerializeField]
        private string id;
        [SerializeField]
        private string name;
        [SerializeField]
        [TextArea(2, 4)]
        private string description;
        [SerializeField]
        private Sprite icon;
        [SerializeField]
        private MonsterStatusInfo monsterStatusData;
        [SerializeField]
        private string prefabId;
        private GameObject prefab;

        [ShowIf("@AttackType == TrainDefense.MonsterAttackType.Ranged")]
        [SerializeField]
        private Projectile rangedProjectilePrefab;
        #endregion

        #region IData
        public string Id => id;
        #endregion

        #region IDescribableData
        public string Name => TrainDefense.Localize.LocalizeHelper.GetByKey($"Monster_{id}_Name", name);
        public string Description => TrainDefense.Localize.LocalizeHelper.GetByKey($"Monster_{id}_Desc", description);
        public Sprite Icon => icon;
        #endregion

        #region IPrefabData
        public string PrefabId => prefabId;
        [ShowInInspector, ReadOnly]
        public GameObject Prefab
        {
            get
            {
                if (prefab == null && !string.IsNullOrEmpty(prefabId))
                {
                    prefab = Resources.Load<GameObject>($"Prefabs/Monsters/{prefabId}");
                    if (prefab == null)
                    {
                        Debug.LogWarning($"MonsterData [{id}]: Prefab not found at 'Prefabs/{prefabId}'");
                    }
                }
                return prefab;
            }
        }
        #endregion

        public string MonsterName => Name;
        public MonsterStatusInfo MonsterStatusData => monsterStatusData;
        public MonsterAttackType AttackType => monsterStatusData.AttackType;
        public Projectile RangedProjectilePrefab => rangedProjectilePrefab;

        [Obsolete("Use Prefab property instead")]
        public Monster MonsterPrefab => Prefab?.GetComponent<Monster>();
    }
}

