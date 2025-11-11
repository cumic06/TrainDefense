using System.Collections;
using Cumic.Events;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense.Game.UI
{
    public class WarningUISpawner : MonoBehaviour
    {
        private void Start()
        {
            GameEventSystem.Subscribe<WarningEvent>(OnWarning);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<WarningEvent>(OnWarning);
        }

        private void OnWarning(WarningEvent warningEvent)
        {
            if (warningEvent.WarningObject == null) return;

            Vector3 screenPosition = Camera.main.WorldToScreenPoint(warningEvent.WorldPosition);
            GameObject warningInstance = ResourceManager.Instance.Spawn(warningEvent.WarningObject, parent: transform);
            warningInstance.SetActive(true);

            // UI 위치 설정
            RectTransform rectTransform = warningInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.position = screenPosition;
            }

            // 지연 시간 후 제거 및 WarningRemovedEvent 발행
            StartCoroutine(RemoveWarningAfterDelay(warningInstance, warningEvent.DelaySeconds, warningEvent));
        }

        private IEnumerator RemoveWarningAfterDelay(GameObject warningInstance, float delaySeconds, WarningEvent warningEvent)
        {
            yield return new WaitForSeconds(delaySeconds);

            if (warningInstance != null)
            {
                // UI의 스크린 위치를 월드 위치로 변환
                Vector3 worldPosition = warningEvent.WorldPosition;
                RectTransform rectTransform = warningInstance.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Vector3 screenPosition = rectTransform.position;
                    worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Camera.main.nearClipPlane + 1f));
                    worldPosition.z = 0f; // 2D 게임이므로 Z는 0으로 설정
                }

                // WarningRemovedEvent 발행
                GameEventSystem.Publish(new WarningRemovedEvent(worldPosition, warningEvent.Target));

                ResourceManager.Instance.Destroy(warningInstance);
            }
        }
    }
}