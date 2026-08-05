using System.Collections;
using System.Collections.Generic;
using Cumic.Events;
using TrainDefense.Game.Events;
using TrainDefense.Game.RunSave.Sections;
using UnityEngine;

namespace TrainDefense.Game.RunSave
{
    /// <summary>
    /// 런 세이브 오케스트레이터. 역 도착(InspectionStartEvent) 때 등록된 섹션들의 상태를 모아 저장하고,
    /// 로비에서 "이어하기"로 들어온 판에 그 상태를 되돌린다.
    /// 상태 변경은 이벤트로 흘리지 않고 이 오케스트레이터가 각 시스템을 직접 호출한다(복원 순서를 보장해야 하므로).
    ///
    /// 저장 대상을 늘리려면 <see cref="IRunStateSection"/> 구현체를 만들어 <see cref="RegisterSection"/>만 호출하면 된다.
    /// </summary>
    public class RunSaveManager : MonoBehaviour
    {
        #region Variables

        public static RunSaveManager Instance { get; private set; }

        // 등록 순서 = 캡처/복원 순서. 앞 섹션이 뒤 섹션의 전제를 만든다
        // (예: 상점 업그레이드 레벨이 복원된 뒤에야 기차 편성에 그 업그레이드가 올바로 얹힌다).
        private readonly List<IRunStateSection> _sections = new();

        // 로비에서 이어하기를 눌러 실어둔 세이브. 게임 씬 진입 후 소비된다.
        private RunSaveData _pendingRestore;
        private bool _isRestoring;
        private bool _isLobbyRun;

        /// <summary>이번 게임 씬 진입이 "이어하기"인지. 포탑 선택창 스킵·첫 삼중택일 스킵 판단에 쓴다.</summary>
        public bool IsContinuePending => _pendingRestore != null;

        /// <summary>복원 절차가 진행 중인지. 복원 중 발행되는 이벤트로 자기 자신이 다시 저장되는 것을 막는다.</summary>
        public bool IsRestoring => _isRestoring;

        #endregion

        #region LifeCycle

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void _Bootstrap()
        {
            if (Instance != null)
                return;

            var go = new GameObject(nameof(RunSaveManager));
            go.AddComponent<RunSaveManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);

                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _RegisterDefaultSections();
            _SubscribeEvents();
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
            GameEventSystem.Subscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Subscribe<GameEndEvent>(_OnGameEnd);
        }

        private void _UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<GameEnterEvent>(_OnGameEnter);
            GameEventSystem.Unsubscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Unsubscribe<GameEndEvent>(_OnGameEnd);
        }

        #endregion

        #region Section Registry

        /// <summary>
        /// 저장 대상 섹션을 등록한다. 같은 SectionId가 이미 있으면 교체한다.
        /// 등록 순서가 곧 복원 순서이므로, 다른 섹션의 결과에 의존하는 섹션은 나중에 등록한다.
        /// </summary>
        public void RegisterSection(IRunStateSection section)
        {
            if (section == null || string.IsNullOrEmpty(section.SectionId))
                return;

            for (int i = 0; i < _sections.Count; i++)
            {
                if (_sections[i].SectionId == section.SectionId)
                {
                    _sections[i] = section;

                    return;
                }
            }

            _sections.Add(section);
        }

        public void UnregisterSection(string sectionId)
        {
            if (string.IsNullOrEmpty(sectionId))
                return;

            _sections.RemoveAll(section => section.SectionId == sectionId);
        }

        private void _RegisterDefaultSections()
        {
            RegisterSection(new UserProgressSection());
            RegisterSection(new StageProgressSection());
            RegisterSection(new ScoreSection());
            // 기차 편성은 상점 업그레이드(UserProgress)가 복원된 뒤에 재구성해야 스탯이 맞으므로 마지막에 둔다.
            RegisterSection(new TrainFormationSection());
        }

        #endregion

        #region Save

        /// <summary>세이브가 남아 있는지. 로비 이어하기 버튼 노출 판단용.</summary>
        public static bool HasSave() => RunSaveStore.HasSave();

        /// <summary>세이브 요약(스테이지·레벨·역 수 등)을 읽는다. 없으면 null.</summary>
        public static RunSaveData.RunSaveSummary LoadSummary() => RunSaveStore.Load()?.summary;

        private void _OnInspectionStart(InspectionStartEvent inspectionStartEvent)
        {
            // 복원 절차가 발행한 상점 진입은 다시 저장하지 않는다(방금 읽은 내용을 그대로 되쓰는 낭비).
            if (_isRestoring || _isLobbyRun)
                return;

            SaveNow();
        }

        /// <summary>
        /// 지금 상태를 세이브에 기록한다. 역 도착은 전투가 멈춰 몬스터·투사체가 정리된 유일한 안전 지점이라
        /// 휘발성 인스턴스를 직렬화하지 않고도 판을 재현할 수 있다.
        /// </summary>
        public void SaveNow()
        {
            var data = new RunSaveData
            {
                savedAtUnixSeconds = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };

            foreach (var section in _sections)
            {
                string json = section.Capture();

                if (!string.IsNullOrEmpty(json))
                    data.SetSection(section.SectionId, json);
            }

            _FillSummary(data.summary);
            RunSaveStore.Save(data);
        }

        private void _FillSummary(RunSaveData.RunSaveSummary summary)
        {
            var stageManager = Manager.StageManager.Instance;
            var scoreManager = Manager.ScoreManager.Instance;
            var userDataManager = UserDataManager.Instance;
            var mainTrain = TrainManager.Instance != null ? TrainManager.Instance.MainTrain : null;

            if (stageManager != null)
            {
                summary.stageId = stageManager.CurrentStageData != null ? stageManager.CurrentStageData.Id : string.Empty;
                summary.stationPassedCount = stageManager.TotalStationPassedCount;
            }

            if (scoreManager != null)
                summary.score = scoreManager.CurrentScore;

            if (userDataManager != null)
                summary.playerLevel = userDataManager.CurrentLevel;

            if (mainTrain != null)
                summary.trainCount = mainTrain.CurrentTrainCount;
        }

        /// <summary>세이브를 지운다. 게임오버·이어하기 소비 시 호출.</summary>
        public void DeleteSave() => RunSaveStore.Delete();

        private void _OnGameEnd(GameEndEvent gameEndEvent)
        {
            // 판이 끝났으면 이어할 대상이 없다. (이어하기로 시작한 판이 다시 끝나도 마찬가지)
            DeleteSave();
        }

        #endregion

        #region Restore

        /// <summary>
        /// 로비에서 이어하기를 시작한다. 세이브를 메모리에 싣고 즉시 디스크에서 지운다 —
        /// 불러온 뒤 앱을 강제 종료해 같은 지점을 반복하는 세이브 스커밍을 막기 위해서다(SPEC-001 승인 결정).
        /// </summary>
        /// <returns>실을 세이브가 있었으면 true.</returns>
        public bool BeginContinue()
        {
            var data = RunSaveStore.Load();

            if (data == null)
                return false;

            _pendingRestore = data;
            RunSaveStore.Delete();

            return true;
        }

        /// <summary>이어하기를 취소한다(씬 이동 실패 등). 이미 디스크에서 지운 세이브를 되돌려 놓는다.</summary>
        public void CancelContinue()
        {
            if (_pendingRestore == null)
                return;

            RunSaveStore.Save(_pendingRestore);
            _pendingRestore = null;
        }

        private void _OnGameEnter(GameEnterEvent gameEnterEvent)
        {
            _isLobbyRun = gameEnterEvent.IsLobby;

            // 로비 배경 시뮬레이션도 GameEnterEvent를 발행하므로 그 판은 저장·복원 대상이 아니다.
            if (_isLobbyRun)
                return;

            // 이어하기가 아닌 새 판이 시작됐다면 예전 세이브는 더 이상 유효하지 않다(두 판이 한 슬롯을 공유하지 않게).
            if (_pendingRestore == null)
            {
                DeleteSave();

                return;
            }

            StartCoroutine(_RestoreRoutine());
        }

        // GameEnterEvent 구독자들이 런 상태를 0으로 리셋하고, TrainManager가 MainTrain을 스폰한 "뒤"에 덮어써야 한다.
        // 스폰된 MainTrain의 Start()는 이번 프레임 뒤에 돌기 때문에 준비될 때까지 기다린다.
        private IEnumerator _RestoreRoutine()
        {
            _isRestoring = true;

            const int MAX_WAIT_FRAMES = 120;
            int waited = 0;

            while (waited < MAX_WAIT_FRAMES && !_IsSceneReadyForRestore())
            {
                waited++;

                yield return null;
            }

            // 한 프레임 더 둬서 MainTrain.Start()의 기본 편성 스폰까지 끝난 상태에서 편성을 갈아끼운다.
            yield return null;

            var data = _pendingRestore;
            _pendingRestore = null;

            if (data == null)
            {
                _isRestoring = false;

                yield break;
            }

            if (!_IsSceneReadyForRestore())
            {
                Debug.LogWarning("[RunSave] 복원에 필요한 매니저가 준비되지 않아 이어하기를 중단합니다. 새 판으로 진행합니다.");
                _isRestoring = false;

                yield break;
            }

            foreach (var section in _sections)
            {
                string json = data.GetSection(section.SectionId);

                // 이 세이브에 없는 섹션(구버전 세이브 + 신규 시스템)은 건너뛴다 — 나머지 복원은 그대로 진행한다.
                if (string.IsNullOrEmpty(json))
                    continue;

                section.Restore(json);
            }

            GameEventSystem.Publish(new RunRestoredEvent());

            // 저장 시점이 역 도착(상점 진입)이므로, 복원 직후에도 상점에서 재개하는 것이 저장 당시와 같은 화면이다.
            // 평소 역 도착과 같은 연출(StageManager._StartInspectionWithTimeline)을 그대로 태워야
            // 상점 배경 오브젝트 활성화·기차 슬라이드아웃까지 정상 상태가 된다.
            // _isRestoring 해제는 상점이 실제로 열린 뒤에 한다 — 연출 콜백이 발행하는 InspectionStartEvent를
            // 평소 역 도착으로 오인해 "방금 소비한 세이브"를 다시 써버리면 이어하기 1회 제한이 무력화된다.
            if (TimelineManager.Instance != null)
            {
                TimelineManager.Instance.StartShopEnterTimeline(() =>
                {
                    GameEventSystem.Publish(new InspectionStartEvent());
                    _isRestoring = false;
                });
            }
            else
            {
                GameEventSystem.Publish(new InspectionStartEvent());
                _isRestoring = false;
            }
        }

        private bool _IsSceneReadyForRestore()
        {
            return UserDataManager.Instance != null
                   && Manager.StageManager.Instance != null
                   && TrainManager.Instance != null
                   && TrainManager.Instance.MainTrain != null;
        }

        #endregion
    }
}
