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
        private float moveDuration = 0.2f;
        #endregion
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void SetDamage(int damage)
        {
            damageText.text = $"{damage}";
        }

        public void SetPosition(Vector3 position)
        {
            _rectTransform.position = position;
            MoveToTargetPosition().Forget();
        }

        private async UniTask MoveToTargetPosition()
        {
            _rectTransform.DOKill();

            float currentPosition = _rectTransform.anchoredPosition.y;
            float targetPosition = currentPosition + 50f;

            await _rectTransform.DOLocalMoveY(targetPosition, moveDuration);
            ResourceManager.Instance.Destroy(gameObject);
        }
    }
}