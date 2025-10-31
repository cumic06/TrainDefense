using System.Linq;
using UnityEngine;
using Cumic;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game
{
    public class DataBaseManager : Singleton<DataBaseManager>
    {
        private DB _db;

        protected override void Awake()
        {
            base.Awake();
            _db = Resources.Load<DB>("Data/DB");
        }

        public DB GetDB() => _db;

        public StageData[] GetStageDatas() => _db.StageDataList.ToArray();

        public MonsterData[] GetMonsterDatas() => _db.MonsterDataList.ToArray();
    }
}
