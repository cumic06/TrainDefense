using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 게임 내 모든 데이터를 통합 관리하는 중앙 데이터베이스
    /// </summary>
    [CreateAssetMenu(fileName = "DB", menuName = "Data/DB")]
    public class DB : ScriptableObject
    {
        #region Data Collections

        [TabGroup("Monster Data")]
        [InfoBox("몬스터 데이터 관리")]
        [SerializeField]
        public List<MonsterData> monsterDataList = new();

        #region Train Data    
        [TabGroup("Train Data")]
        [InfoBox("기차 데이터 관리")]
        [SerializeField]
        public List<TrainData> trainDataList = new();

        [TabGroup("Train Data")]
        [InfoBox("터렛 기차 데이터 관리")]
        [SerializeField]
        public List<TurretTrainData> turretTrainDataList = new();

        [TabGroup("Train Data")]
        [InfoBox("원거리 기차 데이터 관리")]
        [SerializeField]
        public List<RangeTrainData> rangeTrainDataList = new();
        #endregion

        [TabGroup("Train Upgrade Data")]
        [InfoBox("기차 업그레이드 데이터 관리 (기본 Train 및 MainTrain용)")]
        [SerializeField]
        public List<TrainUpgradeData> trainUpgradeDataList = new();

        [TabGroup("Train Upgrade Data")]
        [InfoBox("터렛 기차 업그레이드 데이터 관리")]
        [SerializeField]
        public List<TurretTrainUpgradeData> turretTrainUpgradeDataList = new();

        [TabGroup("Train Upgrade Data")]
        [InfoBox("원거리 기차 업그레이드 데이터 관리")]
        [SerializeField]
        public List<RangeTrainUpgradeData> rangeTrainUpgradeDataList = new();

        [TabGroup("Stage Data")]
        [InfoBox("스테이지 데이터 관리")]
        [SerializeField]
        public List<StageData> stageDataList = new();

        [TabGroup("Upgrade Data")]
        [InfoBox("일반 업그레이드 데이터 관리")]
        [SerializeField]
        public List<UpgradeData> upgradeDataList = new();

        [TabGroup("TriChoice Database")]
        [InfoBox("3지선다 데이터베이스 (선택지 관리)")]
        [SerializeField]
        private TriChoiceDB triChoiceDB = new();

        [TabGroup("Sound Database")]
        [InfoBox("사운드 데이터베이스")]
        [SerializeField]
        private SoundDB soundDB;

        [TabGroup("Elite Data")]
        [InfoBox("엘리트 몬스터 스폰/스탯 설정")]
        [SerializeField]
        private EliteData eliteData = new();

        [TabGroup("Score Data")]
        [InfoBox("스코어 계수 설정")]
        [SerializeField]
        private ScoreData scoreData = new();

        #endregion

        #region Public Properties

        public IReadOnlyList<MonsterData> MonsterDataList => monsterDataList;
        public IReadOnlyList<TrainData> TrainDataList => trainDataList;
        public IReadOnlyList<RangeTrainData> RangeTrainDataList => rangeTrainDataList;
        public IReadOnlyList<TurretTrainData> TurretTrainDataList => turretTrainDataList;
        public IReadOnlyList<TrainUpgradeData> TrainUpgradeDataList => trainUpgradeDataList;
        public IReadOnlyList<TurretTrainUpgradeData> TurretTrainUpgradeDataList => turretTrainUpgradeDataList;
        public IReadOnlyList<RangeTrainUpgradeData> RangeTrainUpgradeDataList => rangeTrainUpgradeDataList;
        public IReadOnlyList<StageData> StageDataList => stageDataList;
        public IReadOnlyList<UpgradeData> UpgradeDataList => upgradeDataList;
        public TriChoiceDB TriChoiceDB => triChoiceDB;
        public SoundDB SoundDB => soundDB;
        public EliteData EliteData => eliteData;
        public ScoreData ScoreData => scoreData;
        #endregion
    }
}