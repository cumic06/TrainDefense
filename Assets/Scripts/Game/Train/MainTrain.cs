using System.Linq;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Cumic.Events;
using TrainDefense.Game.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Data;

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

        private readonly List<ITrainable> _currentTrainables = new();
        private Dictionary<string, int> _currentTrainDataCount = new();//이걸로 나중에 기차 업그레이드 해야 함.

        protected override void Start()
        {
            base.Start();

            GameEventSystem.Subscribe<TriChoiceSelectEvent>(OnTriChoiceSelect);
            GameEventSystem.Subscribe<TrainDeadEvent>(CheckDeadTrain);

            if (startTrainablePrefab != null && startTrainablePrefab.TryGetComponent(out ITrainable trainable))
            {
                SpawnTrain(startTrainablePrefab);
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
                TrainData choiceTrainData = addTrainChoiceData.TrainData;

                AddTrainData(choiceTrainData);
                SpawnTrain(choiceTrainData.TrainPrefab);
            }
        }

        private void Move()
        {
            transform.Translate(Vector3.right * Time.deltaTime * moveSpeed);
        }

        [Button("SpawnTrain")]
        private void SpawnTrain(Train trainPrefab)
        {
            if (_currentTrainables.Count >= maxTrainCount)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Train count is max");
#endif
                return;
            }

            Train trainObject = Instantiate(trainPrefab, transform);
            _currentTrainables.Add(trainObject);
            Vector3 spawnPos = Vector3.left * trainOffset * _currentTrainables.Count;
            trainObject.transform.localPosition = spawnPos;
            GameEventSystem.Publish(new AddTrainEvent(trainData.Icon, trainObject));
        }

        private void AddTrainData(TrainData choiceTrainData)//이걸로 나중에 기차 업그레이드 해야함.
        {
            if (_currentTrainDataCount.ContainsKey(choiceTrainData.Id))
            {
                _currentTrainDataCount[choiceTrainData.Id]++;
            }
            else
            {
                _currentTrainDataCount.Add(choiceTrainData.Id, 1);
            }
        }

        private void CheckDeadTrain(TrainDeadEvent trainDeadEvent)
        {

            foreach (var train in _currentTrainables.ToList())
            {
                if (trainDeadEvent.Train == train as Train)
                {
                    Debug.Log($"{trainDeadEvent.Train.name} {train as Train}");
                    _currentTrainables.Remove(train);

                    if (_currentTrainables.Count == 0)
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