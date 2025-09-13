using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace TrainDefense.Game
{
    public class Projectile : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private int damage = 10;
        [SerializeField]
        private float speed;
        [SerializeField]
        private float destroyDelay;
        #endregion

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
                monster.TakeDamage(damage);
                ResourceManager.Instance.Destroy(gameObject);
            }
        }

        private IEnumerator DestroyCoroutine()
        {
            yield return new WaitForSeconds(destroyDelay);
            ResourceManager.Instance.Destroy(gameObject);
        }
    }
}