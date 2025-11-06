using System.Collections.Generic;
using UnityEngine;
using TrainDefense.Game.Events;
using Cumic.Events;

namespace TrainDefense.Game.UI
{
    public class WarningUISpawner : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private GameObject warningUIPrefab;
        #endregion

        private Dictionary<string, GameObject> _warningUIs = new Dictionary<string, GameObject>();

        private void Start()
        {
            GameEventSystem.Subscribe<WarningEvent>(OnWarning);
            GameEventSystem.Subscribe<WarningRemovedEvent>(OnWarningRemoved);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<WarningEvent>(OnWarning);
            GameEventSystem.Unsubscribe<WarningRemovedEvent>(OnWarningRemoved);
        }

        private void OnWarning(WarningEvent warningEvent)
        {
            Vector3 warningUIPosition = warningEvent.Position;
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(warningUIPosition);
            GameObject spawnWarningUI = ResourceManager.Instance.Spawn(warningUIPrefab, parent: transform);
            spawnWarningUI.transform.localScale = warningEvent.Size;
            spawnWarningUI.transform.position = screenPosition;
            
            _warningUIs[warningEvent.Id] = spawnWarningUI;
        }

        private void OnWarningRemoved(WarningRemovedEvent warningRemovedEvent)
        {
            if (_warningUIs.TryGetValue(warningRemovedEvent.Id, out GameObject warningUI))
            {
                ResourceManager.Instance.Destroy(warningUI);
                _warningUIs.Remove(warningRemovedEvent.Id);
            }
        }
    }
}