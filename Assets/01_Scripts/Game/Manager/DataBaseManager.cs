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
            Debug.Log($"DB: {_db}");
        }

        public DB GetDB() => _db;

        public StageData[] GetStageDatas() => _db.StageDataList.ToArray();

        public MonsterData[] GetMonsterDatas() => _db.MonsterDataList.ToArray();

        public UpgradeData[] GetUpgradeDatas() => _db.UpgradeDataList.ToArray();
        #region TriChoiceDB

        public TriChoiceDB GetTriChoiceDB() => _db.TriChoiceDB;
        public IChoiceOption[] GetAddTrainChoices() => _db.TriChoiceDB.AddTrainChoices.Select(x => x.Option).ToArray();
        public IChoiceOption[] GetUpgradeTrainChoices() => _db.TriChoiceDB.UpgradeTrainChoices.Select(x => x.Option).ToArray();
        #endregion

        public TrainData[] GetTrainDatas() => _db.TrainDataList.ToArray();

        public TurretTrainData[] GetTurretTrainDatas() => _db.TurretTrainDataList.ToArray();

        public RangeTrainData[] GetRangeTrainDatas() => _db.RangeTrainDataList.ToArray();

        public TrainUpgradeData[] GetTrainUpgradeDatas() => _db.TrainUpgradeDataList.ToArray();

        public TurretTrainUpgradeData[] GetTurretTrainUpgradeDatas() => _db.TurretTrainUpgradeDataList.ToArray();

        public RangeTrainUpgradeData[] GetRangeTrainUpgradeDatas() => _db.RangeTrainUpgradeDataList.ToArray();
    }
}