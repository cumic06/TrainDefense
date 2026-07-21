using System;
using System.Collections.Generic;
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
        [Tooltip("기본(Run) 애니메이션 클립에서 추출한 스프라이트 프레임. 도감에서 재생용.")]
        private Sprite[] animationFrames;
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
        public string Description => TrainDefense.Localize.LocalizeHelper.GetByKey(description, description);
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

        /// <summary>기본(Run) 애니메이션의 스프라이트 프레임. 도감 상세에서 재생, 그리드는 첫 프레임만 쓴다.</summary>
        public IReadOnlyList<Sprite> AnimationFrames => animationFrames;
        public bool HasAnimationFrames => animationFrames != null && animationFrames.Length > 0;
        /// <summary>도감 표시용 대표 프레임. 베이크된 프레임이 있으면 첫 프레임, 없으면 기존 아이콘.</summary>
        public Sprite DisplaySprite => HasAnimationFrames ? animationFrames[0] : icon;
        public MonsterAttackType AttackType => monsterStatusData.AttackType;
        public Projectile RangedProjectilePrefab => rangedProjectilePrefab;

        [Obsolete("Use Prefab property instead")]
        public Monster MonsterPrefab => Prefab?.GetComponent<Monster>();
    }
}

