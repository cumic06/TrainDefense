using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using TrainDefense.Game.Events;
using TrainDefense.Game.Datas;
using System.Linq;
using Cumic;
using Cumic.Events;

namespace TrainDefense.Game
{
    public class MainTrain : Train
    {
        #region Field

        [SerializeField]
        private float moveSpeed;

        [SerializeField]
        [BoxGroup("TrainSetting")]
        private int maxTrainCount;
        [SerializeField]
        [BoxGroup("TrainSetting")]
        private float trainOffset;
        [SerializeField]
        [BoxGroup("TrainSetting")]
        private Train startTrainablePrefab;
        #endregion

        private readonly List<ITrainable> _trainables = new();

        protected override void Start()
        {
            base.Start();

            GameEventSystem.Subscribe<TriChoiceSelectEvent>(OnTriChoiceSelect);
            GameEventSystem.Subscribe<TrainDeadEvent>(CheckDeadTrain);

            if (startTrainablePrefab != null && startTrainablePrefab.TryGetComponent(out ITrainable trainable))
            {
                AddTrain(startTrainablePrefab);
            }
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<TriChoiceSelectEvent>(OnTriChoiceSelect);
            GameEventSystem.Unsubscribe<TrainDeadEvent>(CheckDeadTrain);
        }

        private void FixedUpdate()
        {
            if (_isDead) return;
            Move();
        }

        private void OnTriChoiceSelect(TriChoiceSelectEvent triChoiceSelectEvent)
        {
            if (triChoiceSelectEvent.Data is AddTrainChoiceData addTrainChoiceData)
            {
                AddTrain(addTrainChoiceData.TrainData.TrainPrefab);
            }
        }

        private void Move()
        {
            transform.Translate(Vector3.right * Time.deltaTime * moveSpeed);
        }

        [Button("AddTrain")]
        private void AddTrain(Train trainPrefab)
        {
            if (_trainables.Count >= maxTrainCount)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Train count is max");
#endif
                return;
            }

            Train trainObject = Instantiate(trainPrefab, transform);
            _trainables.Add(trainObject);
            Vector3 spawnPos = Vector3.left * trainOffset * _trainables.Count;
            trainObject.transform.localPosition = spawnPos;
            GameEventSystem.Publish(new AddTrainEvent(trainData.Icon, trainObject));//추후에 데이터로 아이콘 추가해주게 변경
        }

        private void CheckDeadTrain(TrainDeadEvent trainDeadEvent)
        {

            foreach (var train in _trainables.ToList())
            {
                if (trainDeadEvent.Train == train as Train)
                {
                    Debug.Log($"{trainDeadEvent.Train.name} {train as Train}");
                    _trainables.Remove(train);

                    if (_trainables.Count == 0)
                    {
                        OnDead();
                    }
                }
            }
        }

        protected override void OnDead()
        {
            base.OnDead();
            GameEventSystem.Publish(new GameEndEvent());
        }
    }
}