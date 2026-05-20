using Cumic.Sequence;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    public class PauseUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private GameObject pausePanel;

        [SerializeField]
        private float uiActiveDelay;
        #endregion

        private bool _isPauseUIActive = false;

        public void OnClickPauseButton()
        {
            if (_isPauseUIActive)
            {
                HidePauseUI();
            }
            else
            {
                ShowPauseUI();
            }
        }

        /// <summary>
        /// 버튼 눌렀을 때
        /// 일시정지 UI 표시 
        /// </summary>
        private void ShowPauseUI()
        {
            gameObject.SetActive(true);

            if (InGameSequence.Instance != null)
                InGameSequence.Instance.PushOverlay(OverlayPhase.MenuPause);
            else
                TimeManager.Instance.Pause();

            pausePanel.SetActive(true);

            pausePanel.transform.localScale = Vector3.zero;

            pausePanel.transform.DOScale(1, uiActiveDelay).SetEase(Ease.InBack).OnComplete(() =>
            {
                pausePanel.transform.localScale = Vector3.one;
            }).SetUpdate(true);

            _isPauseUIActive = true;
        }

        /// <summary>
        /// 버튼 눌렀을 때
        /// 일시정지 UI 숨기기
        /// </summary>
        private void HidePauseUI()
        {
            pausePanel.transform.localScale = Vector3.one;

            pausePanel.transform.DOScale(0, uiActiveDelay).SetEase(Ease.OutBack).OnComplete(() =>
            {
                pausePanel.transform.localScale = Vector3.zero;
                if (InGameSequence.Instance != null)
                    InGameSequence.Instance.PopOverlay(OverlayPhase.MenuPause);
                else
                    TimeManager.Instance.Resume();
                pausePanel.SetActive(false);
                _isPauseUIActive = false;
            }).SetUpdate(true);
        }
    }
}