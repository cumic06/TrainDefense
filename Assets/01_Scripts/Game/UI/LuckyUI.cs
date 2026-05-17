using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;
using TrainDefense;

namespace TrainDefense.Game.UI
{
    public class LuckyUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI luckyText;
        [SerializeField] private float moveDuration = 0.6f;
        [SerializeField] private float moveVDistance = 120f;
        [SerializeField] private float fadeDuration = 0.6f;

        private static readonly Color LuckyColor = new Color(1f, 0.87f, 0.2f); // L3 accent gold

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            luckyText.text = "Lucky!";
            luckyText.color = LuckyColor;
            luckyText.alpha = 1f;
            StartCoroutine(FadeOut());
        }

        private void OnDisable()
        {
            _rectTransform.DOKill();
            luckyText.DOKill();
        }

        public void SetLocalPosition(Vector2 localPosition)
        {
            _rectTransform.anchoredPosition = localPosition;

            _rectTransform.DOKill();
            float targetY = localPosition.y + moveVDistance;
            _rectTransform.DOLocalMoveY(targetY, moveDuration);
        }

        private IEnumerator FadeOut()
        {
            luckyText.DOFade(0f, fadeDuration).SetEase(Ease.InOutSine);
            yield return new WaitForSeconds(fadeDuration);
            ResourceManager.Instance.Destroy(gameObject);
        }
    }
}
