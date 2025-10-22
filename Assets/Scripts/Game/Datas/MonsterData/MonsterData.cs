using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    [System.Serializable]
    public class MonsterData
    {
        #region Fields
        [SerializeField]
        private string id;
        [SerializeField]
        private string monsterName;
        [SerializeField]
        private Sprite icon;
        [SerializeField]
        [TextArea(2, 4)]
        private string description;
        [SerializeField]
        private MonsterStatusInfo monsterStatusData;
        [SerializeField]
        private Monster monsterPrefab;
        #endregion

        public string Id => id;
        public string MonsterName => monsterName;
        public Sprite Icon => icon;
        public string Description => description;
        public MonsterStatusInfo MonsterStatusData => monsterStatusData;
        public Monster MonsterPrefab => monsterPrefab;
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