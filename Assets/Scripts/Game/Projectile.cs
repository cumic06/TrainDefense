using System.Collections;
using UnityEngine;

namespace TrainDefense.Game
{
    public class Projectile : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private float speed;
        [SerializeField]
        private float destroyDelay;
        #endregion

        private int _damage;
        private Coroutine _destroyCoroutine;

        private void OnEnable()
        {
            if (destroyDelay <= 0) return;

            if (_destroyCoroutine != null)
            {
                StopCoroutine(_destroyCoroutine);
            }
            _destroyCoroutine = StartCoroutine(DestroyCoroutine());
        }

        public void Init(int damage)
        {
            _damage = damage;
        }

        private void FixedUpdate()
        {
            Move();
        }

        private void Move()
        {
            transform.Translate(Vector3.right * Time.deltaTime * speed);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Monster monster))
            {
                monster.TakeDamage(_damage);
                ResourceManager.Instance.Destroy(gameObject);
            }
        }

        private void OStay2D(Collider2D collision)
        {

        }

        private IEnumerator DestroyCoroutine()
        {
            yield return new WaitForSeconds(destroyDelay);
            ResourceManager.Instance.Destroy(gameObject);
        }
    }
}