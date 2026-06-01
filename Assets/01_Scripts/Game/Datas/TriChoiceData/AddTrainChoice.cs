using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
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

        #endregion

        public string Id => id;
        public string TrainDataId => trainDataId;
        public string ReplaceTrainId => replaceTrainId;

        /// <summary>
        /// 대체 로직이 필요한지 확인
        /// </summary>
        public bool IsReplaceMode => !string.IsNullOrEmpty(replaceTrainId) && replaceTrainId != "0";

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(trainDataId)) return false;

            var trainManager = TrainManager.Instance;
            if (trainManager == null) return false;

            // 이미 해당 Train을 보유하고 있으면 유효하지 않음
            if (trainManager.CheckHasTrainById(trainDataId)) return false;

            // Elite 교체로 소비된 base ID는 다시 추가하지 않음.
            if (trainManager.IsTrainIdReplaced(trainDataId)) return false;

            if (IsReplaceMode)
            {
                Debug.LogWarning($"AddTrainChoice [{id}]: replaceTrainId is set. Use EliteTrainChoice for replacements.");
                return false;
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

            if (IsReplaceMode)
            {
                Debug.LogError($"AddTrainChoice [{id}]: replaceTrainId is set. Use EliteTrainChoice for replacements.");
                return;
            }

            var skillType = TriChoiceManager.Instance != null
                ? TriChoiceManager.Instance.GetCachedAddSkillType(id)
                : TrainChoiceSkillType.None;
            var selectedSkillId = TriChoiceManager.Instance != null
                ? TriChoiceManager.Instance.GetCachedAddSkillId(id)
                : null;

            main.SpawnTrain(trainData, skillType, selectedSkillId);
        }
    }
}
