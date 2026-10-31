using System;
using Cumic;
using TMPro;
using TrainDefense.Game.Difficulty;
using TrainDefense.Localize;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI.Difficulty
{
    /// <summary>
    /// 로비 시작 버튼을 누르면 뜨는 난이도 팝업. 카드 3장(보통·어려움·매우 어려움)을 몬스터 얼굴 + 이름으로 보여 주고,
    /// 열린 카드를 탭하면 그 단계로 바로 출발한다(별도 출발 버튼 없음). 잠긴 카드는 회색 판 + 얼굴 실루엣뿐이다(해금 조건 글자 없음 — 10-30 사용자 결정).
    /// 해금 상태는 <see cref="DifficultyStageSelection"/>이 들고 있고, 이 팝업은 보여 주고 고르는 일만 한다.
    /// </summary>
    public class DifficultySelectPopupUI : MonoBehaviour
    {
        private const string POPUP_RESOURCE_NAME = "Popup_DifficultySelect";
        private const string TITLE_KEY = "difficulty_title";
        private const string TITLE_FALLBACK = "난이도";

        // 인덱스 = 단계 - MIN_STAGE.
        private static readonly string[] STAGE_NAME_KEYS = { "difficulty_normal", "difficulty_hard", "difficulty_very_hard" };
        private static readonly string[] STAGE_NAME_FALLBACKS = { "보통", "어려움", "매우 어려움" };

        [Serializable]
        private class StageCard
        {
            public Button button;
            public Image faceImage;
            public TextMeshProUGUI nameText;
            public GameObject lockedGroup;
        }

        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleText;

        [Tooltip("단계 순서대로(보통·어려움·매우 어려움). 개수는 DifficultyStageSelection.MAX_STAGE와 같아야 한다")]
        [SerializeField] private StageCard[] stageCards;

        [Tooltip("잠긴 카드의 얼굴 색. 검정이면 실루엣")]
        [SerializeField] private Color lockedFaceColor = new Color(0f, 0f, 0f, 0.45f);

        [SerializeField] private Color lockedCardColor = new Color(0.72f, 0.69f, 0.64f, 1f);

        /// <summary>Resources의 프리팹을 캔버스 아래에 띄운다. 다른 Resources 팝업(SkillTreeButton)과 같은 방식.</summary>
        public static void Open(Transform canvasRoot)
        {
            GameObject popupPrefab = Resources.Load<GameObject>(POPUP_RESOURCE_NAME);

            if (popupPrefab == null)
            {
                Debug.LogError($"[Difficulty] Resources에서 {POPUP_RESOURCE_NAME} 프리팹을 찾을 수 없습니다.");

                return;
            }

            Instantiate(popupPrefab, canvasRoot);
        }

        private void OnEnable()
        {
            _Refresh();

            for (int i = 0; i < stageCards.Length; i++)
            {
                int stage = DifficultyStageSelection.MIN_STAGE + i;
                stageCards[i].button.onClick.AddListener(() => _OnCardClicked(stage));
            }

            closeButton.onClick.AddListener(_Close);

            PopupTween.PlayShow(gameObject);
        }

        private void OnDisable()
        {
            foreach (var card in stageCards)
                card.button.onClick.RemoveAllListeners();

            closeButton.onClick.RemoveListener(_Close);
        }

        private void _Refresh()
        {
            titleText.text = LocalizeHelper.GetByKey(TITLE_KEY, TITLE_FALLBACK);

            if (stageCards.Length != DifficultyStageSelection.MAX_STAGE)
                Debug.LogError($"[Difficulty] 카드 수 {stageCards.Length}가 단계 수 {DifficultyStageSelection.MAX_STAGE}와 다릅니다.");

            for (int i = 0; i < stageCards.Length; i++)
            {
                int stage = DifficultyStageSelection.MIN_STAGE + i;
                bool unlocked = DifficultyStageSelection.IsUnlocked(stage);
                var card = stageCards[i];

                card.nameText.text = LocalizeHelper.GetByKey(STAGE_NAME_KEYS[i], STAGE_NAME_FALLBACKS[i]);
                card.lockedGroup.SetActive(!unlocked);
                card.faceImage.color = unlocked ? Color.white : lockedFaceColor;
                card.button.targetGraphic.color = unlocked ? Color.white : lockedCardColor;
            }
        }

        private void _OnCardClicked(int stage)
        {
            if (!DifficultyStageSelection.IsUnlocked(stage))
                return;

            DifficultyStageSelection.SelectedStage = stage;

            // 시작 버튼이 원래 하던 일(ChangeSceneButton.OnClickNextScene)과 같다.
            SceneController.NextScene(false);
        }

        private void _Close()
        {
            PopupTween.PlayHide(gameObject, () => Destroy(gameObject));
        }
    }
}
