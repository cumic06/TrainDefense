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
        [SerializeField]
        [BoxGroup("TrainSetting")]
        private bool isUnDead = false;
        #endregion

        private readonly List<Train> _currentTrains = new();//살아있는 Train만 있는 목록
        public List<Train> CurrentTrains => _currentTrains;
        public int MaxTrainCount => maxTrainCount;

        private int _currentTrainCount;//생성된 Train 개수
        public int CurrentTrainCount => _currentTrainCount;

        protected override void Start()
        {
            base.Start();

            GameEventSystem.Subscribe<TriChoiceSelectEvent>(OnTriChoiceSelect);
            GameEventSystem.Subscribe<TrainDeadEvent>(CheckDeadTrain);

            if (startTrainablePrefab != null && startTrainablePrefab.TryGetComponent(out Train train))
            {
                SpawnTrain(startTrainablePrefab);
            }

            _currentTrainCount = 0;
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
            var choice = triChoiceSelectEvent.ChoiceOption;
            if (choice == null) return;
            choice.Execute();
        }

        private void Move()
        {
            transform.Translate(Vector3.right * Time.deltaTime * moveSpeed);
        }

        [Button("SpawnTrain")]
        public void SpawnTrain(Train trainPrefab)
        {
            if (_currentTrains.Count >= maxTrainCount)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Train count is max");
#endif
                return;
            }

            Train trainObject = Instantiate(trainPrefab, transform);
            trainObject.Initialize(DataBaseManager.Instance.GetDB().GetTrainData(trainPrefab.Id));
            trainObject.IsUnDead = isUnDead;
            _currentTrains.Add(trainObject);
            _currentTrainCount++;
            Vector3 spawnPos = Vector3.left * trainOffset * _currentTrainCount;
            trainObject.transform.localPosition = spawnPos;
            GameEventSystem.Publish(new AddTrainEvent(_trainData.Icon, trainObject));
        }

        public void UpgradeTrain(string targetTrainId, ITrainUpgradeData upgradeData)
        {
            Train upgradeTrain = _currentTrains.FirstOrDefault(train => train.TrainData.Id == targetTrainId);

            if (upgradeTrain != null)
            {
                upgradeTrain.Upgrade(upgradeData);
            }
        }

        private void CheckDeadTrain(TrainDeadEvent trainDeadEvent)
        {
            if (isUnDead) return;

            foreach (var train in _currentTrains.ToList())
            {
                if (trainDeadEvent.Train == train)
                {
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

        public void ApplyUpgrade(UpgradeData upgradeData)
        {
            if (upgradeData == null) return;

            if (upgradeData.Stats == null || upgradeData.Stats.Length == 0) return;

            foreach (var train in _currentTrains)
            {
                train.ApplyStats(upgradeData.Stats);
            }
        }

        protected override void OnDead()
        {
            if (isUnDead) return;

            base.OnDead();
            GameEventSystem.Publish(new GameEndEvent(false));
        }
    }
}