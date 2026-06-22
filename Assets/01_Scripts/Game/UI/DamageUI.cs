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
        [Tooltip("치명타 시 상승 거리 (일반보다 크게)")]
        [SerializeField]
        private float criticalMoveVDistance = 170f;
        [Tooltip("등장 시 시작 스케일 (작게 시작해 크게 튀어나옴)")]
        [SerializeField]
        private float criticalStartScale = 0.5f;
        [Tooltip("정착 스케일 (오버슈트 후 안착)")]
        [SerializeField]
        private float criticalScale = 1.35f;
        [Tooltip("스케일 팝업 시간")]
        [SerializeField]
        private float criticalScaleDuration = 0.28f;
        [Tooltip("회전 펀치 각도 (좌우 흔들림)")]
        [SerializeField]
        private float criticalPunchRotation = 18f;
        [Tooltip("회전 펀치 시간")]
        [SerializeField]
        private float criticalPunchDuration = 0.3f;
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
            float targetPosition = currentPosition + distance;

            // 치명타는 위로 솟구치듯 감속(OutQuad), 일반은 기존과 동일.
            _rectTransform.DOLocalMoveY(targetPosition, moveDuration)
                .SetEase(_isCritical ? Ease.OutQuad : Ease.Linear);

            if (_isCritical)
                PlayCriticalEmphasis();
        }

        /// <summary>
        /// 치명타 전용 강조 연출: 작게 시작해 탄성 있게 튀어나오고(OutBack), 좌우로 회전 펀치를 주어 역동감을 살린다.
        /// </summary>
        private void PlayCriticalEmphasis()
        {
            _rectTransform.localScale = Vector3.one * criticalStartScale;
            _rectTransform.DOScale(Vector3.one * criticalScale, criticalScaleDuration)
                .SetEase(Ease.OutBack);

            _rectTransform.DOPunchRotation(
                new Vector3(0f, 0f, criticalPunchRotation),
                criticalPunchDuration,
                vibrato: 8,
                elasticity: 0.8f);
        }

        private IEnumerator FadeOut()
        {
            damageText.DOFade(0f, fadeDuration).SetEase(Ease.InOutSine);

            yield return new WaitForSeconds(fadeDuration);

            ResourceManager.Instance.Destroy(gameObject);
        }
    }
}
