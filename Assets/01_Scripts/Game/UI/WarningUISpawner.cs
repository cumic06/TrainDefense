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
            RectTransform warningInstance = ResourceManager.Instance.Spawn(warningEvent.WarningObject, parent: transform).GetComponent<RectTransform>();
            warningInstance.gameObject.SetActive(true);

            if (warningInstance != null)
            {
                warningInstance.position = screenPosition;

                if (warningEvent.IsScaleByAttackRange && warningEvent.AttackRange > 0f)
                {
                    Vector3 currentScale = warningInstance.localScale;
                    float scaledX = warningEvent.AttackRange / 2;
                    warningInstance.localScale = new Vector3(scaledX, currentScale.y, currentScale.z);

                    Vector3 currentLocalPosition = warningInstance.localPosition;
                    float offsetHalf = warningEvent.AttackRange / 2;
                    warningInstance.localPosition = new Vector3(currentLocalPosition.x + offsetHalf, currentLocalPosition.y, currentLocalPosition.z);

                    if (warningEvent.Target != null)
                    {
                        warningInstance.LookAt2D(warningEvent.Target.transform);
                        warningInstance.rotation = Quaternion.Euler(0f, 0f, warningInstance.rotation.eulerAngles.z + 30);
                        warningInstance.localPosition = new Vector3(currentLocalPosition.x / 2, currentLocalPosition.y / 2, currentLocalPosition.z / 2);
                    }

                }
            }

            StartCoroutine(RemoveWarningAfterDelay(warningInstance, warningEvent.WarningDelaySeconds, warningEvent));
        }

        private IEnumerator RemoveWarningAfterDelay(RectTransform warningInstance, float delaySeconds, WarningEvent warningEvent)
        {
            yield return new WaitForSeconds(delaySeconds);

            if (warningInstance != null)
            {
                Vector3 worldPosition = warningEvent.WorldPosition;
                RectTransform rectTransform = warningInstance.GetComponent<RectTransform>();

                if (rectTransform != null)
                {
                    Vector3 screenPosition = rectTransform.position;
                    worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Camera.main.nearClipPlane + 1f));
                    worldPosition.z = 0f;
                }

                GameEventSystem.Publish(new WarningRemovedEvent(worldPosition, warningEvent.WarningDelaySeconds, warningEvent.Target, warningEvent.Sender));

                ResourceManager.Instance.Destroy(warningInstance.gameObject);
            }
        }
    }
}