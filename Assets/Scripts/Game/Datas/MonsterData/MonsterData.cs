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
        private MonsterStatusInfo monsterStatusData;
        [SerializeField]
        private string prefabId;
        private GameObject prefab;
        #endregion

        #region IData
        public string Id => id;
        #endregion

        #region IDescribableData
        public string Name => name;
        public string Description => description;
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

        public string MonsterName => name;
        public MonsterStatusInfo MonsterStatusData => monsterStatusData;
        
        [Obsolete("Use Prefab property instead")]
        public Monster MonsterPrefab => Prefab?.GetComponent<Monster>();
    }
}

[Serializable]
public struct MonsterStatusInfo
{
    public int MaxHp;
    public int Damage;
    public float MoveSpeed;
    public float AttackDelay;
    public int DropExpMin;
    public int DropExpMax;
    public int DropMoneyMin;
    public int DropMoneyMax;
    public float AttackRange;
}
