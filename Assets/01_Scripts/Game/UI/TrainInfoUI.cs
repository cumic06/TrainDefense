using UnityEngine;
using Cumic.Events;
using TrainDefense;
using TrainDefense.Game.Events;

namespace TrainDefense.Game.UI
{
    public class TrainInfoUI : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private TrainInfoSlotUI trainInfoSlotUI;
        #endregion

        private void Awake()
        {
            ResourceManager.Instance.RegisterPersistent(gameObject);
            GameEventSystem.Subscribe<AddTrainEvent>(OnAddTrain);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<AddTrainEvent>(OnAddTrain);
        }

        private void OnAddTrain(AddTrainEvent addTrainEvent)
        {
            TrainInfoSlotUI spawnTrainInfoSlotUI = ResourceManager.Instance.Spawn(trainInfoSlotUI, parent: transform);
            spawnTrainInfoSlotUI.Init(addTrainEvent.Train);
            spawnTrainInfoSlotUI.SetIcon(addTrainEvent.Icon);
        }
    }
}