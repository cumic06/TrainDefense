using UnityEngine;

namespace TrainDefense.Game
{
    public class Money : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private float speed = 10f;

        [SerializeField]
        private float bounceBackDuration = 0.2f;
        #endregion

        private float _elapsedTime;
        private Vector3 _initialDirection;
        private bool _isInitialized;

        private void OnEnable()
        {
            _elapsedTime = 0f;
            _isInitialized = false;
        }

        private void FixedUpdate()
        {
            if (TrainManager.Instance.MainTrain == null)
                return;

            if (!_isInitialized)
            {
                Vector3 directionToPlayer = (TrainManager.Instance.MainTrain.transform.position - transform.position).normalized;
                _initialDirection = -directionToPlayer;
                _isInitialized = true;
            }

            _elapsedTime += Time.deltaTime;

            Vector3 moveDirection;


            if (_elapsedTime < bounceBackDuration)
            {
                moveDirection = _initialDirection; // 초반에는 플레이어 반대 방향으로 이동
            }
            else
            {
                moveDirection = (TrainManager.Instance.MainTrain.transform.position - transform.position).normalized;
            }

            transform.Translate(moveDirection * (speed * Time.fixedDeltaTime), Space.World);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out MainTrain mainTrain))
            {
                ResourceManager.Instance.Destroy(gameObject);
            }
        }
    }
}