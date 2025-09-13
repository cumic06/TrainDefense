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
        #endregion

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
                Destroy(gameObject);
            }
        }
    }
}