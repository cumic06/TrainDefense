using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 팝업 공통 등장/퇴장 연출 헬퍼. 인디게임에서 흔한 "스케일 팝(OutBack) + 페이드인" 방식이다.
    /// 공통 베이스 클래스가 없으므로 각 팝업의 OnEnable(등장)과 닫기 메서드(퇴장)에서 호출한다.
    ///
    /// 핵심: 전체화면 검정 딤(배경)은 스케일하지 않고, 실제 팝업 박스(패널)만 톡 튀게 한다.
    ///   - 루트(딤/배경/컨테이너)에는 CanvasGroup 페이드만 적용한다(스케일 X).
    ///   - 루트의 직계 자식 중 '전체화면 stretch가 아닌'(= 고정 크기 패널 박스) 자식만 스케일한다.
    ///   이 프로젝트 팝업은 대부분 "루트 = 전체화면 딤(Image a=0.6) + 자식 = 고정 크기 패널" 구조라
    ///   딤(stretch)은 자동으로 제외되고 패널(고정 크기)만 스케일된다. 딤이 자식이어도 stretch라 함께 제외된다.
    ///
    /// 주의: 팝업은 옵션·삼중택일·일시정지처럼 Time.timeScale=0 상태에서 뜨는 경우가 많다.
    /// 모든 트윈에 SetUpdate(true)(unscaled)를 걸어야 멈추지 않고 연출이 보인다.
    /// (참조 lesson: DOTween 연출 트윈 SetUpdate(true) 누락 → timeScale=0 중 freeze)
    ///
    /// CanvasGroup은 런타임에 자동으로 보장(GetOrAdd)하므로 프리팹/인스펙터 수정이 필요 없다.
    /// </summary>
    public static class PopupTween
    {
        private const float ShowDuration = 0.28f;
        private const float HideDuration = 0.16f;
        private const float ShowStartScale = 0.8f;   // 패널 등장 시작 스케일 (작게 → 1.0으로 톡)
        private const float HideEndScale = 0.8f;      // 패널 퇴장 끝 스케일 (살짝 작아지며 사라짐)

        // 매번 할당하지 않도록 재사용하는 패널 수집 버퍼(동기 호출이라 재진입 없음).
        private static readonly List<Transform> _panelBuffer = new List<Transform>();

        /// <summary>
        /// 팝업을 등장시킨다. 루트(딤)는 페이드만, 실제 패널 박스(자식)는 작게서 OutBack으로 톡 튀어나온다.
        /// OnEnable에서 호출한다.
        /// </summary>
        public static void PlayShow(GameObject root)
        {
            if (root == null)
                return;

            CanvasGroup canvasGroup = _GetOrAddCanvasGroup(root);

            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.DOFade(1f, ShowDuration).SetEase(Ease.OutQuad).SetUpdate(true);

            foreach (Transform panel in _CollectPanels(root.transform))
            {
                panel.DOKill();
                panel.localScale = Vector3.one * ShowStartScale;
                panel.DOScale(1f, ShowDuration).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        /// <summary>
        /// 팝업을 퇴장시킨다. 패널은 살짝 작아지고 루트는 페이드아웃한 뒤 onComplete(보통 Destroy / SetActive(false))를 실행한다.
        /// 닫기 버튼 핸들러에서 기존의 즉시 Destroy/SetActive 대신 호출한다.
        /// </summary>
        public static void PlayHide(GameObject root, Action onComplete)
        {
            if (root == null)
            {
                onComplete?.Invoke();

                return;
            }

            CanvasGroup canvasGroup = _GetOrAddCanvasGroup(root);

            canvasGroup.DOKill();

            // 닫히는 동안 중복 입력(재클릭)을 막는다.
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            foreach (Transform panel in _CollectPanels(root.transform))
            {
                panel.DOKill();
                panel.DOScale(HideEndScale, HideDuration).SetEase(Ease.InBack).SetUpdate(true);
            }

            canvasGroup.DOFade(0f, HideDuration).SetEase(Ease.InQuad).SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }

        // 루트의 직계 자식 중 '전체화면 stretch가 아닌'(= 고정 크기 패널 박스) 자식만 모은다.
        // 전체화면 stretch 자식(딤·오버레이·터치영역)은 스케일에서 제외해 검정 배경이 작아졌다 커지지 않게 한다.
        private static List<Transform> _CollectPanels(Transform root)
        {
            _panelBuffer.Clear();

            int count = root.childCount;
            for (int i = 0; i < count; i++)
            {
                Transform child = root.GetChild(i);
                if (_IsFullscreenStretch(child))
                    continue;

                _panelBuffer.Add(child);
            }

            return _panelBuffer;
        }

        // RectTransform 앵커가 전체화면(min≈0, max≈1)으로 펼쳐졌는지 — 딤/배경/오버레이로 간주한다.
        private static bool _IsFullscreenStretch(Transform child)
        {
            if (!(child is RectTransform rect))
                return false;

            Vector2 min = rect.anchorMin;
            Vector2 max = rect.anchorMax;

            return min.x <= 0.05f && min.y <= 0.05f && max.x >= 0.95f && max.y >= 0.95f;
        }

        private static CanvasGroup _GetOrAddCanvasGroup(GameObject root)
        {
            if (!root.TryGetComponent(out CanvasGroup canvasGroup))
                canvasGroup = root.AddComponent<CanvasGroup>();

            return canvasGroup;
        }
    }
}
