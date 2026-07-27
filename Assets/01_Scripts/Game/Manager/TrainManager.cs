using UnityEngine;
using Cumic;
using Cumic.Sequence;
using TrainDefense;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Controller;
using Cumic.Events;
using System.Linq;

namespace TrainDefense.Game
{
   public class TrainManager : Singleton<TrainManager>
   {
      #region Field
      [SerializeField]
      private MainTrain mainTrain;
      [SerializeField]
      private CameraController cameraController;
      #endregion

      public MainTrain MainTrain => mainTrain;

      // 이번 판 누적 생존 시간(초). Engage 중에만 unscaled 시간으로 누적해 일시정지/상점/배속에 영향받지 않게 한다.
      private float _survivalElapsed;
      private bool _isSurvivalTiming;

      private void Start()
      {
         GameEventSystem.Subscribe<GameEnterEvent>(OnGameEnter);
         GameEventSystem.Subscribe<GameEndEvent>(OnGameEnd);
      }

      private void OnDestroy()
      {
         GameEventSystem.Unsubscribe<GameEnterEvent>(OnGameEnter);
         GameEventSystem.Unsubscribe<GameEndEvent>(OnGameEnd);
      }

      private void Update()
      {
         if (_isSurvivalTiming && InGameSequence.Instance != null && InGameSequence.Instance.IsRunning)
            _survivalElapsed += Time.unscaledDeltaTime;
      }

      private void OnGameEnter(GameEnterEvent gameEnterEvent)
      {
         if (mainTrain == null || DatabaseManager.Instance == null)
            return;

         var trainData = DatabaseManager.Instance.GetTrainData(mainTrain.Id);
         if (trainData == null)
         {
            Debug.LogWarning($"TrainManager: TrainData not found for id '{mainTrain.Id}'");
            return;
         }

         var trainObject = ResourceManager.Instance.Spawn(trainData.Prefab).GetComponent<MainTrain>();
         mainTrain = trainObject;
         ResourceManager.Instance.RegisterPersistent(mainTrain.gameObject);
         mainTrain.Initialize(trainData);
         cameraController?.SetFollowTarget(mainTrain.transform);

         _EquipSelectedWeapon();

         _survivalElapsed = 0f;
         _isSurvivalTiming = !gameEnterEvent.IsLobby;
      }

      private void OnGameEnd(GameEndEvent gameEndEvent)
      {
         if (!_isSurvivalTiming)
            return;

         _isSurvivalTiming = false;

         if (UserDataManager.Instance != null)
            UserDataManager.Instance.ReportSurvivalTime(UserDataManager.Instance.SelectedTurretId, _survivalElapsed);
      }

      // 포탑 선택창에서 고른 주무기를 MainTrain에 장착한다. 미선택이면 무기 없이 진행.
      private void _EquipSelectedWeapon()
      {
         string selectedId = UserDataManager.Instance != null ? UserDataManager.Instance.SelectedTurretId : null;
         if (string.IsNullOrEmpty(selectedId))
            return;

         var turretData = DatabaseManager.Instance.GetTrainData(selectedId) as TurretTrainData;
         if (turretData == null)
         {
            Debug.LogWarning($"TrainManager: Selected turret '{selectedId}' is not a TurretTrainData");
            return;
         }

         mainTrain.EquipWeapon(turretData);
      }

      public bool CheckHasTrain(TrainData trainData)
      {
         return mainTrain.CheckHasTrain(trainData);
      }

      public bool CheckHasTrainById(string trainId)
      {
         return mainTrain.CheckHasTrainById(trainId);
      }

      public bool IsTrainIdReplaced(string trainId)
      {
         return mainTrain != null && mainTrain.IsTrainIdReplaced(trainId);
      }

      public bool IsMaxTrainCountReached()
      {
         if (mainTrain.CurrentTrainCount == 0)
            return false;

         return mainTrain.CurrentTrainCount >= mainTrain.MaxTrainCount;
      }

      public int GetMaxTrainCount()
      {
         return mainTrain.MaxTrainCount;
      }

      public int GetTrainCount()
      {
         return mainTrain.CurrentTrainCount;
      }

      public bool TryUseTrainSkill(Train train)
      {
         if (mainTrain == null || train == null)
            return false;

         if (!mainTrain.CurrentAliveTrains.Contains(train))
            return false;

         if (!train.HasActiveSkill || !train.CanUseSkill)
            return false;

         return train.TryUseSkill();
      }

      public Train GetNearTrain(Vector3 position)
      {
         var trains = mainTrain.CurrentAliveTrains;
         if (trains.Count == 0)
            return null;

         Train closest = null;
         float minSqrDistance = float.MaxValue;

         foreach (var train in trains)
         {
            if (train.IsDead || train.IsMainTrain || train == null || !train.gameObject.activeInHierarchy)
               continue;

            float sqrDistance = position.SqrDistance(train.transform.position);

            if (sqrDistance < minSqrDistance)
            {
               minSqrDistance = sqrDistance;
               closest = train;
            }
         }

         return closest;
      }

      /// <summary>
      /// 살아있는 모든 기차들을 반환한다.
      /// </summary>
      /// <returns></returns>
      public Train[] GetAliveTrains()
      {
         return mainTrain.CurrentAliveTrains.ToArray();
      }

      /// <summary>
      /// 획득한 모든 기차들을 반환한다.
      /// </summary>
      /// <returns></returns>
      public Train[] GetTrains()
      {
         return mainTrain.CurrentTrains.ToArray();
      }

      public void ApplyUpgrade(UpgradeData upgradeData, int newLevel, int prevLevel)
      {
         if (mainTrain == null)
         {
            Debug.LogWarning("TrainManager: MainTrain is null");
            return;
         }

         mainTrain.ApplyUpgrade(upgradeData, newLevel, prevLevel);
      }
   }
}