using UnityEngine;
using UnityEngine.UI;
using Cumic.Events;
using TrainDefense.Game.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Tutorial;
using TrainDefense.Game.Manager;
using System.Collections.Generic;

namespace TrainDefense.Game.UI
{
    public class StageSelectUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private GameObject stageSelectPanel;

        [SerializeField]
        private Image currentStageImage;

        [SerializeField]
        private Button stage1Button;

        [SerializeField]
        private Button stage2Button;
        #endregion

        private StageData _stageData1;
        private StageData _stageData2;
        private StageData _selectedStageData;
        private Animator animator;
        private Dictionary<Transform, Image[]> monsterIconImages = new();

        private bool _isSelecting = false;

        private const string firstStageSelectAniTrigger = "FirstStage";
        private const string secondStageSelectAniTrigger = "SecondStage";

        private void Awake()
        {
            animator = GetComponent<Animator>();
            CacheMonsterIconImages();
        }

        private void Start()
        {
            GameEventSystem.Subscribe<RandomStageOptionsEvent>(OnStageSelect);

            if (stage1Button != null)
            {
                stage1Button.onClick.AddListener(() => StageSelected(_stageData1));
            }

            if (stage2Button != null)
            {
                stage2Button.onClick.AddListener(() => StageSelected(_stageData2));
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

        private void CacheMonsterIconImages()
        {
            CacheMonsterIconImage(stage1Button.transform);
            CacheMonsterIconImage(stage2Button.transform);
            CacheMonsterIconImage(currentStageImage.transform);
        }

        private void CacheMonsterIconImage(Transform parent)
        {
            Image[] images = new Image[parent.childCount];
            for (int i = 0; i < parent.childCount; i++)
            {
                images[i] = parent.GetChild(i).GetComponent<Image>();
            }
            monsterIconImages.Add(parent, images);
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

            ApplyStageSprites();
            ShowMonsterIconImages();

            if (stageSelectPanel != null)
            {
                stageSelectPanel.SetActive(true);
            }

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.StartTutorial("stageSelectTutorial");
            }
        }

        private void ApplyStageSprites()
        {
            ApplyStageSprite(stage1Button.image, _stageData1);
            ApplyStageSprite(stage2Button.image, _stageData2);
            ApplyStageSprite(currentStageImage, StageManager.Instance.CurrentStageData);
        }

        private void ApplyStageSprite(Image image, StageData stageData)
        {
            if (image == null || stageData == null) return;

            if (stageData.StageImage != null)
            {
                image.sprite = stageData.StageImage;
            }
        }

        private void ShowMonsterIconImages()
        {
            ShowMonsterIconImage(stage1Button.transform, _stageData1);
            ShowMonsterIconImage(stage2Button.transform, _stageData2);
            ShowMonsterIconImage(currentStageImage.transform, StageManager.Instance.CurrentStageData);
        }

        private void ShowMonsterIconImage(Transform parent, StageData stageData)
        {
            if (parent == null || stageData == null) return;

            if (monsterIconImages.TryGetValue(parent, out Image[] images))
            {
                for (int i = 0; i < stageData.SpawnDatas.Length; i++)
                {
                    var monsterId = stageData.SpawnDatas[i].MonsterId;
                    images[i].sprite = DatabaseManager.Instance.GetMonsterData(monsterId).Icon;
                    images[i].gameObject.SetActive(true);
                }
            }
        }

        private void HideMonsterIconImages()
        {
            HideMonsterIconImage(stage1Button.transform);
            HideMonsterIconImage(stage2Button.transform);
            HideMonsterIconImage(currentStageImage.transform);
        }

        private void HideMonsterIconImage(Transform parent)
        {
            if (monsterIconImages.TryGetValue(parent, out Image[] images))
            {
                for (int i = 0; i < images.Length; i++)
                {
                    images[i].gameObject.SetActive(false);
                }
            }
        }

        private void StageSelected(StageData selectedStageData)
        {
            if (!_isSelecting || selectedStageData == null) return;

            _isSelecting = false;

            _selectedStageData = selectedStageData;
            if (_selectedStageData == _stageData1)
            {
                animator.SetTrigger(firstStageSelectAniTrigger);
            }
            else
            {
                animator.SetTrigger(secondStageSelectAniTrigger);
            }
        }

        // 애니메이션에서 실행
        private void OnStageSelected()
        {
            // 애니메이션 이벤트가 전이/루프 경계 타이밍에 따라 중복 발화될 수 있으므로 1회만 처리한다.
            // (중복 발행 시 맵 이동 연출이 이중 실행되어 기차 스케일 오염·EngageStart 조기 발행이 발생)
            if (_selectedStageData == null) return;
            var selectedStageData = _selectedStageData;
            _selectedStageData = null;

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.SkipCurrent();
            }

            if (stageSelectPanel != null)
            {
                stageSelectPanel.SetActive(false);
                HideMonsterIconImages();
            }

            // 선택 결과 전달 (UI -> StageManager)
            GameEventSystem.Publish(new StageSelectEvent(selectedStageData));
        }
    }
}
