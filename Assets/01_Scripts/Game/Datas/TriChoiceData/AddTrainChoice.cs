using System;
using System.Linq;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    public enum AddTrainChoiceSkillType { None, Passive, Active }

    [Serializable]
    public class AddTrainChoice : IChoiceOption
    {
        #region Fields
        [SerializeField]
        private string id;

        [SerializeField]
        [Tooltip("소환할 Train 데이터 ID")]
        private string trainDataId;

        [SerializeField]
        [Tooltip("대체할 기존 Train 데이터 ID (0이면 대체 없이 추가)")]
        private string replaceTrainId;

        [SerializeField]
        [Tooltip("표시할 스킬 타입 (None=기본, Passive=패시브만, Active=액티브만)")]
        private AddTrainChoiceSkillType skillType;
        #endregion

        public string Id => id;
        public string TrainDataId => trainDataId;
        public string ReplaceTrainId => replaceTrainId;
        public AddTrainChoiceSkillType SkillType => skillType;

        /// <summary>
        /// 대체 로직이 필요한지 확인
        /// </summary>
        public bool IsReplaceMode => !string.IsNullOrEmpty(replaceTrainId) && replaceTrainId != "0";

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(trainDataId)) return false;

            var trainManager = TrainManager.Instance;

            // 이미 해당 Train을 보유하고 있으면 유효하지 않음
            if (trainManager.CheckHasTrainById(trainDataId)) return false;

            // 이 카드가 추가/대체하려는 ID 중 하나라도 Elite 교체로 소비된 base ID면 영구 차단
            if (trainManager.IsTrainIdReplaced(trainDataId)) return false;
            if (IsReplaceMode && trainManager.IsTrainIdReplaced(replaceTrainId)) return false;

            // 대체 모드인 경우: 대체할 Train이 존재하고 레벨 조건 충족 필요
            if (IsReplaceMode)
            {
                var mainTrain = trainManager.MainTrain;
                if (mainTrain == null) return false;

                // 대체할 Train 찾기
                var targetTrain = mainTrain.CurrentTrains.FirstOrDefault(t => t.TrainData.Id == replaceTrainId);
                if (targetTrain == null) return false;

                // 레벨 조건: CurrentLevel >= 2 (3번 업그레이드 완료)
                return targetTrain.CurrentLevel >= 2;
            }

            // 일반 추가 모드
            return true;
        }

        public void Execute()
        {
            var trainData = DatabaseManager.Instance.GetTrainData(trainDataId);

            if (trainData == null || trainData.Prefab == null)
            {
                Debug.LogError($"AddTrainChoice [{id}]: Invalid TrainData");
                return;
            }

            var main = TrainManager.Instance.MainTrain;
            if (main == null)
            {
                Debug.LogError("AddTrainChoice: MainTrain is null");
                return;
            }

            // 대체 모드인 경우: 기존 Train을 새 Train으로 대체
            if (IsReplaceMode)
            {
                main.ReplaceTrain(replaceTrainId, trainData, skillType);
            }
            else
            {
                main.SpawnTrain(trainData, skillType);
            }
        }
    }
}
