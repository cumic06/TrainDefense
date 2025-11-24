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

        private struct DeadTrainInfo
        {
            public Train Train;
            public int OriginalIndex;
        }

        private readonly List<DeadTrainInfo> _deadTrains = new();//죽은 Train 목록
        private readonly Dictionary<Train, int> _trainOriginalIndexMap = new();//Train의 원래 인덱스 매핑

        protected override void Start()
        {
            base.Start();

            GameEventSystem.Subscribe<TriChoiceSelectEvent>(OnTriChoiceSelect);
            GameEventSystem.Subscribe<TrainDeadEvent>(CheckDeadTrain);
            GameEventSystem.Subscribe<InspectionEvent>(OnInspection);

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
            GameEventSystem.Unsubscribe<InspectionEvent>(OnInspection);
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
            int originalIndex = _currentTrainCount;
            _trainOriginalIndexMap[trainObject] = originalIndex;
            GameEventSystem.Publish(new AddTrainEvent(_trainData.Icon, trainObject));
            
            // 살아있는 기차 재정렬
            RearrangeTrains();
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

                    // 원래 인덱스 가져오기
                    int originalIndex = _trainOriginalIndexMap.ContainsKey(train) ? _trainOriginalIndexMap[train] : _currentTrains.Count;

                    // 오브젝트 비활성화
                    train.gameObject.SetActive(false);

                    // 죽은 기차 정보 저장
                    _deadTrains.Add(new DeadTrainInfo
                    {
                        Train = train,
                        OriginalIndex = originalIndex
                    });

                    // 살아있는 기차 재정렬
                    RearrangeTrains();

                    if (_currentTrains.Count == 0)
                    {
                        OnDead();
                    }
                    break;
                }
            }
        }

        private void RearrangeTrains()
        {
            // 살아있는 기차만 연속적으로 재정렬
            for (int i = 0; i < _currentTrains.Count; i++)
            {
                Vector3 newPos = Vector3.left * trainOffset * (i + 1);
                _currentTrains[i].transform.localPosition = newPos;
            }
        }

        private void RearrangeAllTrainsToOriginalOrder()
        {
            // 모든 기차를 원래 순서대로 재정렬
            // _currentTrains와 _deadTrains를 합쳐서 원래 인덱스 순서로 정렬
            var allTrains = new List<(Train train, int originalIndex)>();

            // 살아있는 기차 추가
            foreach (var train in _currentTrains)
            {
                if (_trainOriginalIndexMap.ContainsKey(train))
                {
                    allTrains.Add((train, _trainOriginalIndexMap[train]));
                }
            }

            // 죽은 기차 추가
            foreach (var deadTrainInfo in _deadTrains)
            {
                allTrains.Add((deadTrainInfo.Train, deadTrainInfo.OriginalIndex));
            }

            // 원래 인덱스 순서로 정렬
            allTrains.Sort((a, b) => a.originalIndex.CompareTo(b.originalIndex));

            // 정렬된 순서대로 위치 재설정
            for (int i = 0; i < allTrains.Count; i++)
            {
                Vector3 newPos = Vector3.left * trainOffset * (i + 1);
                allTrains[i].train.transform.localPosition = newPos;
            }
        }

        private void OnInspection(InspectionEvent inspectionEvent)
        {
            // 죽은 기차 복원
            foreach (var deadTrainInfo in _deadTrains.ToList())
            {
                Train train = deadTrainInfo.Train;
                
                // HP 최대치로 복원 및 _isDead = false 설정 (레벨과 업그레이드는 유지)
                train.Resurrect();
                
                // 오브젝트 활성화
                train.gameObject.SetActive(true);
                
                // _deadTrains에서 제거하고 _currentTrains에 다시 추가
                _deadTrains.Remove(deadTrainInfo);
                _currentTrains.Add(train);
            }

            // 모든 기차를 원래 순서대로 재정렬
            RearrangeAllTrainsToOriginalOrder();
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