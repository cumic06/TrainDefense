using System.Linq;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Cumic.Events;
using TrainDefense.Game.Events;
using TrainDefense.Game.Datas;

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
        [Header("테스트용")]
        [BoxGroup("TrainSetting")]
        private Train startTrainablePrefab;
        #endregion

        private readonly List<Train> _currentTrains = new();
        public List<Train> CurrentTrains => _currentTrains;

        protected override void Start()
        {
            base.Start();

            GameEventSystem.Subscribe<TriChoiceSelectEvent>(OnTriChoiceSelect);
            GameEventSystem.Subscribe<TrainDeadEvent>(CheckDeadTrain);

            if (startTrainablePrefab != null && startTrainablePrefab.TryGetComponent(out Train train))
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
            ChoiceOption choice = triChoiceSelectEvent.ChoiceOption;
            if (choice == null) return;

            switch (choice.ChoiceType)
            {
                case ChoiceType.AddTrain:
                    if (choice.TrainData != null)
                    {
                        SpawnTrain(choice.TrainData.TrainPrefab);
                    }
                    break;

                case ChoiceType.UpgradeTrain:
                    TrainUpgradeData selectedUpgrade = choice.GetSelectedUpgradeData();
                    if (selectedUpgrade != null)
                    {
                        UpgradeTrain(choice.TargetTrainId, selectedUpgrade);
                    }
                    break;
            }
        }

        private void Move()
        {
            transform.Translate(Vector3.right * Time.deltaTime * moveSpeed);
        }

        [Button("SpawnTrain")]
        private void SpawnTrain(Train trainPrefab)
        {
            if (_currentTrains.Count >= maxTrainCount)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Train count is max");
#endif
                return;
            }

            Train trainObject = Instantiate(trainPrefab, transform);
            _currentTrains.Add(trainObject);
            Vector3 spawnPos = Vector3.left * trainOffset * _currentTrains.Count;
            trainObject.transform.localPosition = spawnPos;
            GameEventSystem.Publish(new AddTrainEvent(trainData.Icon, trainObject));
        }

        private void UpgradeTrain(string targetTrainId, TrainUpgradeData upgradeData)
        {
            Train upgradeTrain = _currentTrains.FirstOrDefault(train => train.TrainData.Id == targetTrainId);

            if (upgradeTrain != null)
            {
                upgradeTrain.Upgrade(upgradeData);
            }
        }

        private void CheckDeadTrain(TrainDeadEvent trainDeadEvent)
        {

            foreach (var train in _currentTrains.ToList())
            {
                if (trainDeadEvent.Train == train as Train)
                {
                    Debug.Log($"{trainDeadEvent.Train.name} {train as Train}");
                    _currentTrains.Remove(train);

                    if (_currentTrains.Count == 0)
                    {
                        OnDead();
                    }
                }
            }
        }

        public bool CheckHasTrain(TrainData trainData)
        {
            return _currentTrains.Any(train => train.TrainData.Id == trainData.Id);
        }

        public bool CheckHasTrainById(string trainId)
        {
            return _currentTrains.Any(train => train.TrainData.Id == trainId);
        }

        protected override void OnDead()
        {
            base.OnDead();
            GameEventSystem.Publish(new GameEndEvent());
        }
    }
}