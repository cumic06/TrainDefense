using UnityEngine;
using Cumic;
using TrainDefense;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Controller;
using Cumic.Events;

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