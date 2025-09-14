using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense.Game.UI
{
    public class TrainInfoUI : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private TrainInfoSlotUI trainInfoSlotUI;
        #endregion

        private void Start()
        {
            GameEventSystem.Subscribe<AddTrainEvent>(OnAddTrain);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<AddTrainEvent>(OnAddTrain);
        }

        private void OnAddTrain(AddTrainEvent addTrainEvent)
        {
            TrainInfoSlotUI spawnTrainInfoSlotUI = ResourceManager.Instance.Spawn(trainInfoSlotUI, parent: transform);
            spawnTrainInfoSlotUI.SetIcon(addTrainEvent.Icon);
        }
    }
}