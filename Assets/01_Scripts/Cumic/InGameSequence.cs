using Cumic.Events;
using Sirenix.OdinInspector;
using TrainDefense.Game;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using UnityEngine;

namespace Cumic.Sequence
{
    public class InGameSequence : MonoBehaviour
    {
        #region Variables

        public static InGameSequence Instance { get; private set; }

        public BasePhase CurrentBase { get; private set; }
        public OverlayPhase CurrentOverlays { get; private set; }

        public bool IsRunning =>
            CurrentBase == BasePhase.Engage
            && CurrentOverlays == OverlayPhase.None;

        #endregion

        #region Fields

        [SerializeField]
        [BoxGroup("Engage Start")]
        private GameObject engageStartUI;

        #endregion

        #region LifeCycle

        private void Awake()
        {
            Instance = this;
            // TriChoiceUI 등 다른 컴포넌트의 Start()보다 먼저 이벤트를 구독해야
            // GameEnterEvent 처리 시 Pause가 TriChoice 배경 활성화보다 먼저 실행됨.
            _SubscribeEvents();
        }

        private void Start()
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayBGM(SoundType.BGM_Stage);
        }

        private void OnDestroy()
        {
            _UnsubscribeEvents();

            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Sub/UnSub

        private void _SubscribeEvents()
        {
            GameEventSystem.Subscribe<GameEnterEvent>(_OnGameEnter);
            GameEventSystem.Subscribe<EngageReadyEvent>(_OnEngageReady);
            GameEventSystem.Subscribe<EngageStartEvent>(_OnEngageStart);
            GameEventSystem.Subscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Subscribe<LevelUpEvent>(_OnLevelUp);
            GameEventSystem.Subscribe<TriChoiceSelectEvent>(_OnTriChoiceSelect);
            GameEventSystem.Subscribe<StageEndEvent>(_OnStageEnd);
            GameEventSystem.Subscribe<GameEndEvent>(_OnGameEnd);
        }

        private void _UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<GameEnterEvent>(_OnGameEnter);
            GameEventSystem.Unsubscribe<EngageReadyEvent>(_OnEngageReady);
            GameEventSystem.Unsubscribe<EngageStartEvent>(_OnEngageStart);
            GameEventSystem.Unsubscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Unsubscribe<LevelUpEvent>(_OnLevelUp);
            GameEventSystem.Unsubscribe<TriChoiceSelectEvent>(_OnTriChoiceSelect);
            GameEventSystem.Unsubscribe<StageEndEvent>(_OnStageEnd);
            GameEventSystem.Unsubscribe<GameEndEvent>(_OnGameEnd);
        }

        #endregion

        public void GameEnterHandler()
        {
            GameEventSystem.Publish(new GameEnterEvent());
        }

        public void PushOverlay(OverlayPhase overlay)
        {
            CurrentOverlays |= overlay;
            Debug.Log($"[InGameSequence] PushOverlay({overlay}) → Base={CurrentBase} Overlays={CurrentOverlays} IsRunning={IsRunning}");
            _Sync();
        }

        public void PopOverlay(OverlayPhase overlay)
        {
            CurrentOverlays &= ~overlay;
            Debug.Log($"[InGameSequence] PopOverlay({overlay}) → Base={CurrentBase} Overlays={CurrentOverlays} IsRunning={IsRunning}");
            _Sync();
        }

        private void _SetBase(BasePhase phase)
        {
            CurrentBase = phase;
            _UpdateEngageUI(phase == BasePhase.Engage);
            Debug.Log($"[InGameSequence] SetBase({phase}) → Overlays={CurrentOverlays} IsRunning={IsRunning}");
            _Sync();
        }

        private void _Sync()
        {
            Debug.Log($"[InGameSequence] Sync — Base={CurrentBase} Overlays={CurrentOverlays} IsRunning={IsRunning}");
            if (IsRunning) TimeManager.Instance?.Resume();
            else           TimeManager.Instance?.Pause();
        }

        private void _OnGameEnter(GameEnterEvent _)           => _SetBase(BasePhase.Idle);
        private void _OnEngageReady(EngageReadyEvent _)       => _SetBase(BasePhase.EngageReady);
        private void _OnEngageStart(EngageStartEvent _)       => _SetBase(BasePhase.Engage);
        private void _OnInspectionStart(InspectionStartEvent _) => _SetBase(BasePhase.Inspection);
        private void _OnLevelUp(LevelUpEvent _)               => PushOverlay(OverlayPhase.LevelUp);

        private void _OnTriChoiceSelect(TriChoiceSelectEvent e)
        {
            // 삼중택일이 여러 번 남아있는 동안에는 오버레이를 유지해 시간이 흐르지 않도록 한다.
            if (e != null && e.ChoiceLeftCount > 0)
                return;

            PopOverlay(OverlayPhase.LevelUp);

            // GameEnter 직후 첫 삼중택일(LevelUp 오버레이 없이 Idle 상태)이 완료되면 Engage를 시작한다.
            if (CurrentBase == BasePhase.Idle)
                GameEventSystem.Publish(new EngageStartEvent());
        }
        private void _OnStageEnd(StageEndEvent _)             => _SetBase(BasePhase.StageEnd);
        private void _OnGameEnd(GameEndEvent _)               => _SetBase(BasePhase.GameOver);

        private void _UpdateEngageUI(bool active)
        {
            if (engageStartUI != null)
                engageStartUI.SetActive(active);
        }
    }
}
