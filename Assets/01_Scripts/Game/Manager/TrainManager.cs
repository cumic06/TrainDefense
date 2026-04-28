using UnityEngine;
using Cumic;
using TrainDefense;
using TrainDefense.Game.Datas;
using Cumic.Events;
using Unity.Cinemachine;
using System.Linq;

namespace TrainDefense.Game
{
   public class TrainManager : Singleton<TrainManager>
   {
      #region Field
      [SerializeField]
      private MainTrain mainTrain;
      [SerializeField]
      private CinemachineCamera cinemachineCamera;
      #endregion

      public MainTrain MainTrain => mainTrain;

      private void Start()
      {
         GameEventSystem.Subscribe<GameEnterEvent>(OnGameEnter);
      }

      private void OnDestroy()
      {
         GameEventSystem.Unsubscribe<GameEnterEvent>(OnGameEnter);
      }

      private void OnGameEnter(GameEnterEvent gameEnterEvent)
      {
         if (mainTrain != null)
         {
            var trainData = DatabaseManager.Instance.GetTrainData(mainTrain.Id);
            var trainObject = ResourceManager.Instance.Spawn(trainData.Prefab).GetComponent<MainTrain>();
            mainTrain = trainObject;
            mainTrain.Initialize(trainData);
            cinemachineCamera.Target.TrackingTarget = mainTrain.transform;
         }
      }

      public bool CheckHasTrain(TrainData trainData)
      {
         return mainTrain.CheckHasTrain(trainData);
      }

      public bool CheckHasTrainById(string trainId)
      {
         return mainTrain.CheckHasTrainById(trainId);
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

      public int GetMaxUpgradeTrainCount()
      {
         return GetMaxUpgradeTrains().Length;
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

      /// <summary>
      /// 최대 업그레이드 레벨에 도달한 기차들을 반환한다.
      /// </summary>
      /// <returns></returns>
      public Train[] GetMaxUpgradeTrains()
      {
         var allUpgradeData = DatabaseManager.Instance.GetAllTrainUpgradeData();
         return mainTrain.CurrentTrains.Where(train => allUpgradeData.Any(upgrade => upgrade.MaxLevel == train.CurrentLevel)).ToArray();
      }

      public void ApplyUpgrade(UpgradeData upgradeData)
      {
         if (mainTrain == null)
         {
            Debug.LogWarning("TrainManager: MainTrain is null");
            return;
         }

         mainTrain.ApplyUpgrade(upgradeData);
      }
   }
}