using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    public class PauseUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private Image backgroundImage;

        [SerializeField]
        private Image pauseImage;

        [SerializeField]
        private Button pauseButton;

        [SerializeField]
        private float uiActiveDelay;
        #endregion

        private bool _isPauseUIActive = false;

        private void Awake()
        {
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            pauseButton.onClick.AddListener(OnClickPauseButton);
        }

        private void UnsubscribeEvents()
        {
            pauseButton.onClick.RemoveListener(OnClickPauseButton);
        }

        private void OnClickPauseButton()
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
        public void ShowPauseUI()
        {
            TimeManager.Instance.Pause();

            pauseImage.gameObject.SetActive(true);
            backgroundImage.gameObject.SetActive(true);

            pauseImage.transform.localScale = Vector3.zero;

            pauseImage.transform.DOScale(1, uiActiveDelay).SetEase(Ease.InBack).OnComplete(() =>
            {
                pauseImage.transform.localScale = Vector3.one;
            }).SetUpdate(true);

            _isPauseUIActive = true;
        }

        /// <summary>
        /// 버튼 눌렀을 때
        /// 일시정지 UI 숨기기
        /// </summary>
        public void HidePauseUI()
        {
            pauseImage.transform.localScale = Vector3.one;

            pauseImage.transform.DOScale(0, uiActiveDelay).SetEase(Ease.OutBack).OnComplete(() =>
            {
                pauseImage.transform.localScale = Vector3.zero;
                TimeManager.Instance.Resume();
                pauseImage.gameObject.SetActive(false);
                backgroundImage.gameObject.SetActive(false);
                _isPauseUIActive = false;
            }).SetUpdate(true);
        }
    }
}