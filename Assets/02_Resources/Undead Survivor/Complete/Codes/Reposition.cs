using UnityEngine;
using TrainDefense.Game;

namespace Goldmetal.UndeadSurvivor
{
    public class Reposition : MonoBehaviour
    {
        [SerializeField]
        private float repositionDistance = 20f;

        void OnTriggerExit2D(Collider2D collision)
        {
            if (!collision.TryGetComponent<MainTrain>(out var train))
                return;

            Vector3 playerPos = TrainManager.Instance.MainTrain.transform.position;
            Vector3 myPos = transform.position;

            if (transform.CompareTag("Ground"))
            {
                float diffX = playerPos.x - myPos.x;
                float diffY = playerPos.y - myPos.y;

                float dirX = diffX < 0 ? -1 : 1;
                float dirY = diffY < 0 ? -1 : 1;
                diffX = Mathf.Abs(diffX);
                diffY = Mathf.Abs(diffY);

                if (diffX > diffY)
                {
                    transform.Translate(Vector3.right * dirX * repositionDistance);
                }
                else if (diffX < diffY)
                {
                    transform.Translate(Vector3.up * dirY * repositionDistance);
                }
            }
        }
    }
}