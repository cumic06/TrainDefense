using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Cumic.Events;
using TrainDefense.Game.Events;
using DG.Tweening;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 새 몬스터 발견 UI View
    /// NewMonsterDiscoveredEvent를 구독하여 UI를 표시합니다.
    /// </summary>
    public class NewMonsterView : MonoBehaviour
    {
        #region Variables

        #region Fields
        [Header("UI References")]
        [SerializeField]
        private GameObject panel;
        [SerializeField]
        private Image iconImage;
        [SerializeField]
        private TMP_Text nameText;
        [SerializeField]
        private GameObject specialMonster;

        [Header("Settings")]
        [SerializeField]
        private float moveDuration = 0.7f;
        [SerializeField]
        private float hideDuration = 2f;
        [SerializeField]
        private float hidePosX = -350;
        [SerializeField]
        private float showPosX = 0;

        [SerializeField]
        private float specialShowDuration = 0.5f;

        [SerializeField]
        private bool autoHide = true;
        #endregion

        private NewMonsterViewModel _viewModel;
        private Coroutine _hideCoroutine;

        #endregion

        private void Start()
        {
            GameEventSystem.Subscribe<NewMonsterDiscoveredEvent>(OnNewMonsterDiscovered);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<NewMonsterDiscoveredEvent>(OnNewMonsterDiscovered);
        }

        private void OnNewMonsterDiscovered(NewMonsterDiscoveredEvent evt)
        {
            _viewModel = new NewMonsterViewModel(evt.MonsterId, evt.IsBoss);

            if (_viewModel.IsValid)
            {
                Show();
            }
        }

        private void Show()
        {
            UpdateUI();

            if (panel != null)
            {
                panel.transform.DOLocalMoveX(showPosX, moveDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
            }

            if (autoHide)
            {
                if (_hideCoroutine != null)
                {
                    StopCoroutine(_hideCoroutine);
                }
                _hideCoroutine = StartCoroutine(HideAfterDelay());
            }
        }

        private void UpdateUI()
        {
            if (_viewModel == null) return;

            if (iconImage != null)
            {
                iconImage.sprite = _viewModel.Icon;
                iconImage.gameObject.SetActive(_viewModel.Icon != null);
            }

            if (nameText != null)
            {
                nameText.text = _viewModel.MonsterName ?? "???";
            }

            if (specialMonster != null)
            {
                specialMonster.SetActive(_viewModel.IsBoss);

                specialMonster.transform.DOLocalMoveX(showPosX, specialShowDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
            }
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(hideDuration);
            Hide();
        }

        public void Hide()
        {
            if (panel != null)
            {
                panel.transform.DOLocalMoveX(hidePosX, moveDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true);
            }

            _viewModel = null;
        }
    }
}