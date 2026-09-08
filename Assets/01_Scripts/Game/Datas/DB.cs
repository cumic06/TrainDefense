using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 게임 내 모든 데이터를 통합 관리하는 중앙 데이터베이스
    /// </summary>
    [CreateAssetMenu(fileName = "DB", menuName = "Data/DB")]
    public class DB : ScriptableObject, ISerializationCallbackReceiver
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

        [TabGroup("Train Skill Data")]
        [InfoBox("패시브 스킬 데이터 관리")]
        [SerializeField]
        private TrainSkillDataDB trainSkillDataDB = new();

        // 마이그레이션용 레거시 필드 — 이미 이전 완료된 에셋에서는 비어있음
        [HideInInspector]
        [FormerlySerializedAs("trainPassiveSkillDataList")]
        [SerializeField]
        private List<TrainPassiveSkillData> _legacyTrainPassiveSkillDataList = new();
        #endregion

        [TabGroup("Stage Data")]
        [InfoBox("스테이지 데이터 관리")]
        [SerializeField]
        public List<StageData> stageDataList = new();

        [TabGroup("Upgrade Data")]
        [InfoBox("일반 업그레이드 데이터 관리")]
        [SerializeField]
        public List<UpgradeData> upgradeDataList = new();

        [TabGroup("Upgrade Data")]
        [InfoBox("영구(메타) 업그레이드 데이터 관리 — 엘리트 처치 재화로 구매, 런 사이 유지")]
        [SerializeField]
        public List<PermanentUpgradeData> permanentUpgradeDataList = new();

        [TabGroup("SkillTree Data")]
        [InfoBox("스킬트리 노드 데이터 관리 — 스킬 포인트로 습득, 런 사이 유지 (유일한 노드 데이터 소스)")]
        [SerializeField]
        public List<SkillNodeData> skillNodeDataList = new();

        [TabGroup("SkillTree Data")]
        [InfoBox("스킬트리 레인(계열) 표기 — 레인별 이름 로컬라이즈 키")]
        [SerializeField]
        public List<SkillTreeLaneData> skillTreeLaneDataList = new();

        [TabGroup("TriChoice Database")]
        [InfoBox("3지선다 데이터베이스 (선택지 관리)")]
        [SerializeField]
        private TriChoiceDB triChoiceDB = new();

        [TabGroup("TriChoice Database")]
        [InfoBox("상점 스탯 강화 등급 (배수·등장 구간)")]
        [SerializeField]
        public List<StatUpgradeTierData> statUpgradeTierDataList = new();

        [TabGroup("TriChoice Database")]
        [InfoBox("스탯별 최소 등급 (강한 스탯을 고등급으로 미는 장치)")]
        [SerializeField]
        public List<StatUpgradeStatData> statUpgradeStatDataList = new();

        [TabGroup("TriChoice Database")]
        [InfoBox("포탑별 강화 가능 스탯과 증가율")]
        [SerializeField]
        public List<TrainStatUpgradeRuleData> trainStatUpgradeRuleDataList = new();

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

        public IReadOnlyList<StatUpgradeTierData> StatUpgradeTierDataList => statUpgradeTierDataList;
        public IReadOnlyList<StatUpgradeStatData> StatUpgradeStatDataList => statUpgradeStatDataList;
        public IReadOnlyList<TrainStatUpgradeRuleData> TrainStatUpgradeRuleDataList => trainStatUpgradeRuleDataList;
        public IReadOnlyList<MonsterData> MonsterDataList => monsterDataList;
        public IReadOnlyList<TrainData> TrainDataList => trainDataList;
        public IReadOnlyList<RangeTrainData> RangeTrainDataList => rangeTrainDataList;
        public TrainSkillDataDB TrainSkillDataDB => trainSkillDataDB;
        public IReadOnlyList<TurretTrainData> TurretTrainDataList => turretTrainDataList;
        public IReadOnlyList<StageData> StageDataList => stageDataList;
        public IReadOnlyList<UpgradeData> UpgradeDataList => upgradeDataList;
        public IReadOnlyList<PermanentUpgradeData> PermanentUpgradeDataList => permanentUpgradeDataList;
        public IReadOnlyList<SkillNodeData> SkillNodeDataList => skillNodeDataList;
        public IReadOnlyList<SkillTreeLaneData> SkillTreeLaneDataList => skillTreeLaneDataList;
        public TriChoiceDB TriChoiceDB => triChoiceDB;
        public SoundDB SoundDB => soundDB;
        public EliteData EliteData => eliteData;
        public ScoreData ScoreData => scoreData;
        #endregion

        #region ISerializationCallbackReceiver
        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            if (trainSkillDataDB == null)
                trainSkillDataDB = new TrainSkillDataDB();

            if (_legacyTrainPassiveSkillDataList != null && _legacyTrainPassiveSkillDataList.Count > 0
                && trainSkillDataDB.trainPassiveSkillDataList.Count == 0)
            {
                trainSkillDataDB.trainPassiveSkillDataList.AddRange(_legacyTrainPassiveSkillDataList);
                _legacyTrainPassiveSkillDataList.Clear();
                UnityEngine.Debug.Log($"[DB] 마이그레이션: Passive 스킬 {trainSkillDataDB.trainPassiveSkillDataList.Count}개 이전 완료");
            }
        }
        #endregion
    }
}