using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cumic.Events;
using TrainDefense.Game.Events;
using System.Collections;

namespace TrainDefense.Game.UI
{
    public class BossWarningUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject warningPanel;
        

        private void Start()
        {
            GameEventSystem.Subscribe<BossSpawnEvent>(OnBossSpawn);
            
            if (warningPanel != null)
            {
                warningPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<BossSpawnEvent>(OnBossSpawn);
        }

        private void OnBossSpawn(BossSpawnEvent spawnEvent)
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
            yield return new WaitForSeconds(3f);
            HideWarning();
        }
    }
}
