using UnityEngine;
using Cumic.Events;
using TrainDefense.Game.Events;
using System.Collections;

namespace TrainDefense.Game.UI
{
    public class WarningUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject warningPanel;
        

        private void Start()
        {
            GameEventSystem.Subscribe<MonsterRushEvent>(OnMonsterRush);
            
            if (warningPanel != null)
            {
                warningPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<MonsterRushEvent>(OnMonsterRush);
        }

        private void OnMonsterRush(MonsterRushEvent _)
        {
            ShowWarning();
        }

        private void ShowWarning()
        {
            if (warningPanel != null)
            {
                warningPanel.SetActive(true);
            }
            
            StartCoroutine(BlinkCoroutine());
        }

        private void HideWarning()
        {
            if (warningPanel != null)
            {
                warningPanel.SetActive(false);
            }
        }

        private IEnumerator BlinkCoroutine()
        {
            yield return new WaitForSecondsRealtime(3f);
            HideWarning();
        }
    }
}
