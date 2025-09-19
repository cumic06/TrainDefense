using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using TrainDefense.Game.Events;
using System;
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
        [BoxGroup("TrainSetting")]
        private Train startTrainablePrefab;
        [SerializeField]
        private int[] levelUpExp;
        #endregion

        private int _currentExp;
        private int _currentLevel;
        private readonly List<ITrainable> _trainables = new();

        protected override void Start()
        {
            base.Start();

            GameEventSystem.Subscribe<ExpChangeEvent>(ChangeExp);
            GameEventSystem.Subscribe<TriChoiceSelectEvent>(OnTriChoiceSelect);

            if (startTrainablePrefab != null && startTrainablePrefab.TryGetComponent(out ITrainable trainable))
            {
                AddTrain(startTrainablePrefab);
            }
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

            _trainables.Add(trainPrefab);
            Train trainObject = Instantiate(trainPrefab, transform);
            Vector3 spawnPos = Vector3.left * trainOffset * _trainables.Count;
            trainObject.transform.localPosition = spawnPos;
            GameEventSystem.Publish(new AddTrainEvent(trainData.Icon, trainObject));//추후에 데이터로 아이콘 추가해주게 변경
        }

        private void ChangeExp(ExpChangeEvent expChangeEvent)
        {
            if (levelUpExp.Length <= _currentLevel)
            {
                Debug.LogError("max Level");
                return;
            }

            _currentExp += expChangeEvent.ChangeValue;

            GameEventSystem.Publish(new ExpChangeUIEvent(_currentExp, levelUpExp[_currentLevel]));

            if (_currentExp >= levelUpExp[_currentLevel])
            {
                LevelUp();
            }
        }

        private void LevelUp()
        {
            _currentLevel++;
            _currentExp = 0;
            GameEventSystem.Publish(new LevelUpEvent());
        }
    }
}