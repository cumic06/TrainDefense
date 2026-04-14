using UnityEngine;
using UnityEngine.UI;
using Cumic.Events;
using TrainDefense.Game.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Tutorial;

namespace TrainDefense.Game.UI
{
    public class StageSelectUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private GameObject stageSelectPanel;
        
        [SerializeField]
        private Button stage1Button;
        
        [SerializeField]
        private Button stage2Button;
        #endregion

        private StageData _stageData1;
        private StageData _stageData2;
        private bool _isSelecting = false;

        private void Start()
        {
            GameEventSystem.Subscribe<RandomStageOptionsEvent>(OnStageSelect);
            
            if (stage1Button != null)
            {
                stage1Button.onClick.AddListener(() => OnStageSelected(_stageData1));
            }
            
            if (stage2Button != null)
            {
                stage2Button.onClick.AddListener(() => OnStageSelected(_stageData2));
            }

            if (stageSelectPanel != null)
            {
                stageSelectPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<RandomStageOptionsEvent>(OnStageSelect);
        }

        private void OnStageSelect(RandomStageOptionsEvent stageSelectEvent)
        {
            if (_isSelecting) return;

            _stageData1 = stageSelectEvent.StageData1;
            _stageData2 = stageSelectEvent.StageData2;

            if (_stageData1 == null || _stageData2 == null)
            {
                Debug.LogWarning("RandomStageOptionsEvent에 StageData가 null입니다.");
                return;
            }

            ShowStageSelection();
        }

        private void ShowStageSelection()
        {
            _isSelecting = true;

            // 스테이지 이미지 적용 (버튼의 Image 컴포넌트에 스프라이트 적용)
            ApplyStageSprite(stage1Button, _stageData1);
            ApplyStageSprite(stage2Button, _stageData2);

            if (stageSelectPanel != null)
            {
                stageSelectPanel.SetActive(true);
            }

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.StartTutorial("stageSelectTutorial");
            }
        }

        private void ApplyStageSprite(Button targetButton, StageData stageData)
        {
            if (targetButton == null || stageData == null) return;

            var buttonImage = targetButton.GetComponent<Image>();
            if (buttonImage != null && stageData.StageImage != null)
            {
                buttonImage.sprite = stageData.StageImage;
            }
        }

        private void OnStageSelected(StageData selectedStageData)
        {
            if (!_isSelecting || selectedStageData == null) return;

            _isSelecting = false;

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.SkipCurrent();
            }

            if (stageSelectPanel != null)
            {
                stageSelectPanel.SetActive(false);
            }

            // 선택 결과 전달 (UI -> StageManager)
            GameEventSystem.Publish(new StageSelectEvent(selectedStageData));
        }
    }
}
