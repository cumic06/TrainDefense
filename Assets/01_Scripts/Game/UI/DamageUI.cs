using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;
using Cumic;
using TrainDefense;

namespace TrainDefense.Game.UI
{
    public class DamageUI : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private TextMeshProUGUI damageText;

        [SerializeField]
        private float moveDuration = 0.5f;
        [SerializeField]
        private float moveVDistance = 100f;

        [SerializeField]
        private float fadeDuration = 0.5f;

        [Header("색상")]
        [SerializeField]
        private Color normalColor = Color.white;
        [SerializeField]
        private Color criticalColor = Color.red;

        [Header("치명타 강조 연출")]
        [Tooltip("치명타 시 글자 크기. 기본 1.0보다 크게 팝업한다.")]
        [SerializeField]
        private float criticalScale = 1.45f;
        [Tooltip("커지는(팝업) 시간.")]
        [SerializeField]
        private float criticalPopDuration = 0.2f;
        [Tooltip("좌/우로 기울며 흔들리는 각도(도).")]
        [SerializeField]
        private float criticalShakeAngle = 10f;
        [Tooltip("좌우 흔들림 시간.")]
        [SerializeField]
        private float criticalShakeDuration = 0.45f;
        [Tooltip("좌우 흔들림 횟수. 클수록 더 빠르게 떨린다.")]
        [SerializeField]
        private int criticalShakeVibrato = 10;
        [Tooltip("치명타 상승 거리. 기본보다 크게.")]
        [SerializeField]
        private float criticalMoveVDistance = 130f;
        #endregion
        private RectTransform _rectTransform;
        private bool _isCritical;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            // 풀링 객체이므로 재사용 전 상태를 초기화한다(이전 치명타 연출의 스케일/회전 잔존 방지).
            damageText.alpha = 1f;
            _rectTransform.localScale = Vector3.one;
            _rectTransform.localRotation = Quaternion.identity;
            StartCoroutine(FadeOut());
        }

        private void OnDisable()
        {
            _rectTransform.DOKill();
            damageText.DOKill();
            _rectTransform.localScale = Vector3.one;
            _rectTransform.localRotation = Quaternion.identity;
        }

        public void SetDamage(float damage, bool isCritical = false)
        {
            _isCritical = isCritical;
            damageText.text = $"{damage:N1}";
            damageText.color = isCritical ? criticalColor : normalColor;
        }

        public void SetPosition(Vector3 position)
        {
            _rectTransform.position = position;
            PlaySpawnAnimation();
        }

        public void SetLocalPosition(Vector2 localPosition)
        {
            _rectTransform.anchoredPosition = localPosition;
            PlaySpawnAnimation();
        }

        private void PlaySpawnAnimation()
        {
            _rectTransform.DOKill();

            float currentPosition = _rectTransform.anchoredPosition.y;
            float distance = _isCritical ? criticalMoveVDistance : moveVDistance;
            _rectTransform.DOLocalMoveY(currentPosition + distance, moveDuration);

            if (_isCritical)
                PlayCriticalEmphasis();
        }

        /// <summary>
        /// 치명타 강조: 기본 애니메이션(상승+페이드) 위에
        /// (1) 기본보다 크게 팝업하고, (2) 좌/우로 기울며 더 세게 흔들리는 연출을 더한다.
        /// 스케일·회전·위치가 서로 다른 속성이라 기본 상승 이동과 충돌하지 않는다.
        /// </summary>
        private void PlayCriticalEmphasis()
        {
            // 기본보다 더 크게: 오버슈트하며 큰 크기로 팝업.
            _rectTransform.localScale = Vector3.one;
            _rectTransform.DOScale(Vector3.one * criticalScale, criticalPopDuration)
                .SetEase(Ease.OutBack);

            // 좌나 우로 기울며 더 흔들림: 시작 방향을 랜덤(좌/우)으로 정한다.
            float direction = Random.value < 0.5f ? 1f : -1f;
            _rectTransform.DOPunchRotation(
                new Vector3(0f, 0f, criticalShakeAngle * direction),
                criticalShakeDuration,
                criticalShakeVibrato,
                elasticity: 1f);
        }

        private IEnumerator FadeOut()
        {
            damageText.DOFade(0f, fadeDuration).SetEase(Ease.InOutSine);

            yield return new WaitForSeconds(fadeDuration);

            ResourceManager.Instance.Destroy(gameObject);
        }
    }
}
