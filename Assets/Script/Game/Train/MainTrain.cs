using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrainDefense.Game
{
    public class MainTrain : MonoBehaviour, IDamageable
    {
        #region Field
        [SerializeField]
        private int maxHp;

        [SerializeField]
        private float moveSpeed;

        [SerializeField]
        [BoxGroup("TrainSetting")]
        private int maxTrainCount;
        [SerializeField]
        [BoxGroup("TrainSetting")]
        private float trainOffset;
        [BoxGroup("TrainSetting")]
        [SerializeField]
        [BoxGroup("TrainSetting")]
        private GameObject startTrainablePrefab;
        #endregion

        private int _currentHp;
        private bool _isDead;
        private readonly List<ITrainable> _trainables = new();

        private void Start()
        {
            _currentHp = maxHp;

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

        private void Move()
        {
            transform.Translate(Vector3.right * Time.deltaTime * moveSpeed);
        }

        [Button("AddTrain")]
        private void AddTrain(GameObject trainPrefab)
        {
            if (_trainables.Count >= maxTrainCount)
            {
#if UNITY_EDITOR
                Debug.LogError("Train count is max");
#endif
                return;
            }

            if (trainPrefab.TryGetComponent(out ITrainable trainable))
            {
                _trainables.Add(trainable);
                GameObject trainObject = Instantiate(trainPrefab, transform);
                Vector3 spawnPos = Vector3.left * trainOffset * _trainables.Count;
                trainObject.transform.localPosition = spawnPos;
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogError("this Prefab is not ITrainable");
#endif
            }
        }

        public void TakeDamage(int damage)
        {
            if (_isDead) return;

            _currentHp -= damage;

            Debug.Log($"MainTrain HP: {_currentHp}");

            if (_currentHp <= 0)
            {
                OnDead();
            }
        }

        private void OnDead()
        {
            _isDead = true;
            Destroy(gameObject);
        }
    }
}