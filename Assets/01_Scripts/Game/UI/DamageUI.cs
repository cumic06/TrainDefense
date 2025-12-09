using System.Collections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

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
        private float fadeDuration = 0.5f;
        #endregion
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            damageText.alpha = 1f;
            StartCoroutine(FadeOut());
        }

        private void OnDisable()
        {
            _rectTransform.DOKill();
            damageText.DOKill();
        }

        public void SetDamage(int damage)
        {
            damageText.text = $"{damage}";
        }

        public void SetPosition(Vector3 position)
        {
            _rectTransform.position = position;
            MoveToTargetPosition();
        }

        private void MoveToTargetPosition()
        {
            _rectTransform.DOKill();

            float currentPosition = _rectTransform.anchoredPosition.y;
            float targetPosition = currentPosition + 50f;

            _rectTransform.DOLocalMoveY(targetPosition, moveDuration);
        }

        private IEnumerator FadeOut()
        {
            damageText.DOFade(0f, fadeDuration).SetEase(Ease.InOutSine);

            yield return new WaitForSeconds(fadeDuration);

            ResourceManager.Instance.Destroy(gameObject);
        }
    }
}