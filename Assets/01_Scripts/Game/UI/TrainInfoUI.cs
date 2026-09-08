using UnityEngine;
using Cumic.Events;
using TrainDefense;
using TrainDefense.Game.Events;

namespace TrainDefense.Game.UI
{
    public class TrainInfoUI : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private TrainInfoSlotUI trainInfoSlotUI;
        #endregion

        private int _lastScreenWidth;
        private int _lastScreenHeight;

        private void Awake()
        {
            ResourceManager.Instance.RegisterPersistent(gameObject);
            GameEventSystem.Subscribe<AddTrainEvent>(OnAddTrain);
            GameEventSystem.Subscribe<TrainFormationClearedEvent>(_OnFormationCleared);
        }

        private void Start()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
        }

        private void Update()
        {
            // 폴더블 접힘/펼침으로 화면 크기가 바뀌면 풀에서 재사용된 슬롯의 깊이(z)가 틀어져
            // Screen Space-Camera 평면을 벗어나 사라질 수 있다. 해상도 변화 시 슬롯 z를 0으로 복구한다.
            if (Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight)
                return;

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _RestoreSlotsDepth();
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<AddTrainEvent>(OnAddTrain);
            GameEventSystem.Unsubscribe<TrainFormationClearedEvent>(_OnFormationCleared);
        }

        // 편성이 비워지면 슬롯도 함께 비운다. 슬롯은 AddTrainEvent로 늘어나기만 하므로 여기서 정리하지 않으면
        // 이어하기 복원 후 예전 슬롯과 새 슬롯이 겹쳐 남는다.
        private void _OnFormationCleared(TrainFormationClearedEvent trainFormationClearedEvent)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }

        private void OnAddTrain(AddTrainEvent addTrainEvent)
        {
            TrainInfoSlotUI spawnTrainInfoSlotUI = ResourceManager.Instance.Spawn(trainInfoSlotUI, parent: transform);

            // 풀 재사용 시 SetParent(worldPositionStays=true)가 localScale과 깊이(z)를 캔버스 기준으로
            // 틀어놓는다. z가 0이 아니면 Screen Space-Camera 평면을 벗어나 슬롯이 화면에서 사라진다.
            Transform slotTransform = spawnTrainInfoSlotUI.transform;
            slotTransform.localScale = Vector3.one;
            slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, slotTransform.localPosition.y, 0f);

            spawnTrainInfoSlotUI.Init(addTrainEvent.Train);
            spawnTrainInfoSlotUI.SetIcon(addTrainEvent.Icon);
        }

        private void _RestoreSlotsDepth()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform slot = transform.GetChild(i);
                Vector3 localPos = slot.localPosition;

                if (localPos.z == 0f)
                    continue;

                slot.localPosition = new Vector3(localPos.x, localPos.y, 0f);
            }
        }
    }
}
