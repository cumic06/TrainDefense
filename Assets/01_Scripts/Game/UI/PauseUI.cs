using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    public class PauseUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private Image pauseImage;

        [SerializeField]
        private float uiActiveDelay;
        #endregion

        /// <summary>
        /// 버튼 눌렀을 때
        /// 일시정지 UI 표시 
        /// </summary>
        public void ShowPauseUI()
        {
            pauseImage.gameObject.SetActive(true);

            pauseImage.transform.localScale = Vector3.zero;

            pauseImage.transform.DOScale(1, uiActiveDelay).SetEase(Ease.InBack).OnComplete(() =>
            {
                pauseImage.transform.localScale = Vector3.one;
            }).SetUpdate(true);
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
            }).SetUpdate(true);

            pauseImage.gameObject.SetActive(false);
        }
    }
}