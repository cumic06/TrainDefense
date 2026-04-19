using Cumic.Events;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    public class ShopButtonUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private Button shopButton;
        #endregion

        public Action OnClickShopButton;

        private void Start()
        {
            shopButton.onClick.AddListener(_OnShopButtonClick);
            GameEventSystem.Subscribe<EngageStartEvent>(_OnEngageStart);
            GameEventSystem.Subscribe<StageEndEvent>(_OnStageEnd);
            GameEventSystem.Subscribe<GameEndEvent>(_OnGameEnd);
        }

        private void OnDestroy()
        {
            shopButton.onClick.RemoveAllListeners();
            GameEventSystem.Unsubscribe<EngageStartEvent>(_OnEngageStart);
            GameEventSystem.Unsubscribe<StageEndEvent>(_OnStageEnd);
            GameEventSystem.Unsubscribe<GameEndEvent>(_OnGameEnd);
        }

        private void _OnEngageStart(EngageStartEvent engageStartEvent)
        {
            gameObject.SetActive(false);
        }

        private void _OnStageEnd(StageEndEvent stageEndEvent)
        {
            gameObject.SetActive(false);
        }

        private void _OnGameEnd(GameEndEvent gameEndEvent)
        {
            gameObject.SetActive(false);
        }

        private void _OnShopButtonClick()
        {
            OnClickShopButton?.Invoke();
        }
    }
}
