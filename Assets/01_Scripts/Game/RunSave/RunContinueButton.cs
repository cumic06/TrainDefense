using Cumic;
using TMPro;
using TrainDefense.Localize;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.RunSave
{
    /// <summary>
    /// 로비의 "이어하기" 버튼. 저장된 런이 있을 때만 스스로를 켜고, 누르면 그 세이브를 실은 채 게임 씬으로 들어간다.
    /// UI 오브젝트는 프리팹/씬에 정적으로 배치하고 이 컴포넌트를 붙여 쓴다(런타임 생성 금지).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class RunContinueButton : MonoBehaviour
    {
        #region Fields

        [SerializeField]
        [Tooltip("세이브가 없을 때 통째로 숨길 루트. 비우면 이 오브젝트 자신을 숨긴다.")]
        private GameObject rootToHide;

        [SerializeField]
        [Tooltip("버튼 라벨 텍스트. 로컬라이즈 키 UI_Continue로 채운다.")]
        private TMP_Text labelText;

        [SerializeField]
        [Tooltip("선택 — 저장 지점 요약(스테이지·레벨·역 수)을 표시할 텍스트.")]
        private TMP_Text summaryText;

        [SerializeField]
        [Tooltip("이어하기로 진입할 게임 씬의 빌드 인덱스. (기본값 2 = 02_GameScene)")]
        private int gameSceneIndex = 2;

        [SerializeField]
        [Tooltip("씬 전환 시 로딩 씬을 거칠지 여부.")]
        private bool useLoadingScene = true;

        #endregion

        #region Variables

        private Button _button;

        #endregion

        #region LifeCycle

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(_OnClickContinue);
            _SubscribeEvents();
            _Refresh();
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(_OnClickContinue);
            _UnsubscribeEvents();
        }

        #endregion

        #region Sub/UnSub

        // 정적 프리팹의 TMP는 LocalizeText 없이는 언어 변경을 못 받으므로 직접 구독해 다시 채운다.
        private void _SubscribeEvents()
        {
            Localization.OnLanguageChanged += _Refresh;
            Localization.OnInitialized += _Refresh;
        }

        private void _UnsubscribeEvents()
        {
            Localization.OnLanguageChanged -= _Refresh;
            Localization.OnInitialized -= _Refresh;
        }

        #endregion

        private void _Refresh()
        {
            var summary = RunSaveManager.LoadSummary();
            bool hasSave = summary != null;
            var root = rootToHide != null ? rootToHide : gameObject;

            if (labelText != null)
                labelText.text = LocalizeHelper.GetByKey("UI_Continue", "이어하기");

            if (hasSave && summaryText != null)
            {
                string format = LocalizeHelper.GetByKey("UI_Continue_Summary", "Lv.{0} · {1}번째 역");
                summaryText.text = string.Format(format, summary.playerLevel, summary.stationPassedCount);
            }

            // 표시를 채운 뒤에 끈다. 자기 자신을 숨기는 경우 OnEnable이 다시 돌지 않으므로 켜질 때마다 세이브 유무를 다시 본다.
            root.SetActive(hasSave);
        }

        private void _OnClickContinue()
        {
            var runSaveManager = RunSaveManager.Instance;

            if (runSaveManager == null)
            {
                Debug.LogWarning("RunContinueButton: RunSaveManager가 없어 이어하기를 진행할 수 없습니다.");

                return;
            }

            if (!runSaveManager.BeginContinue())
            {
                // 세이브가 사라진 사이 눌린 경우. 표시를 최신 상태로 되돌리고 아무것도 하지 않는다.
                _Refresh();

                return;
            }

            SceneController.LoadScene(gameSceneIndex, useLoadingScene);
        }
    }
}
