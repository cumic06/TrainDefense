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

        [Header("치명타 강조 연출 (임팩트 슬램)")]
        [Tooltip("등장 시 시작 스케일. 크게 시작해 1.0으로 내리꽂힌다.")]
        [SerializeField]
        private float criticalStartScale = 2f;
        [Tooltip("슬램 안착 시간. 짧을수록 강한 타격감.")]
        [SerializeField]
        private float criticalSlamDuration = 0.18f;
        [Tooltip("착지 순간 아래로 내리찍는 거리(px).")]
        [SerializeField]
        private float criticalSlamPunch = 28f;
        [Tooltip("다운펀치 시간.")]
        [SerializeField]
        private float criticalPunchDuration = 0.25f;
        #endregion
        private RectTransform _rectTransform;
        private bool _isCritical;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            // 풀링 객체이므로 재사용 전 상태를 초기화한다(이전 치명타 슬램의 스케일 잔존 방지).
            damageText.alpha = 1f;
            _rectTransform.localScale = Vector3.one;
            StartCoroutine(FadeOut());
        }

        private void OnDisable()
        {
            _rectTransform.DOKill();
            damageText.DOKill();
            _rectTransform.localScale = Vector3.one;
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

            if (_isCritical)
            {
                PlayCriticalSlam();
                return;
            }

            float currentPosition = _rectTransform.anchoredPosition.y;
            _rectTransform.DOLocalMoveY(currentPosition + moveVDistance, moveDuration);
        }

        /// <summary>
        /// 치명타 전용 임팩트 슬램: 큰 글자가 제자리에서 1.0으로 빠르게 내리꽂히며(InBack) 안착하고,
        /// 착지 순간 아래로 내리찍는 펀치를 더해 묵직한 타격감을 낸다. 위로는 거의 뜨지 않는다.
        /// </summary>
        private void PlayCriticalSlam()
        {
            _rectTransform.localScale = Vector3.one * criticalStartScale;
            _rectTransform.DOScale(Vector3.one, criticalSlamDuration)
                .SetEase(Ease.InBack);

            // 슬램이 안착하는 순간(딜레이=슬램 시간) 아래로 내리찍는 충격 펀치.
            // anchoredPosition을 건드리는 유일한 트윈이라 이동 트윈과 충돌하지 않는다.
            _rectTransform.DOPunchAnchorPos(
                    new Vector2(0f, -criticalSlamPunch),
                    criticalPunchDuration,
                    vibrato: 1,
                    elasticity: 0.5f)
                .SetDelay(criticalSlamDuration);
        }

        private IEnumerator FadeOut()
        {
            damageText.DOFade(0f, fadeDuration).SetEase(Ease.InOutSine);

            yield return new WaitForSeconds(fadeDuration);

            ResourceManager.Instance.Destroy(gameObject);
        }
    }
}
