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
        // 다음 프레임 저장이 이미 예약됐는지(한 프레임에 역 도착·구매 등 여러 신호가 와도 한 번만 쓴다).
        private bool _isSaveScheduled;

        /// <summary>이번 게임 씬 진입이 "이어하기"인지. 포탑 선택창 스킵·첫 삼중택일 스킵 판단에 쓴다.</summary>
        public bool IsContinuePending => _pendingRestore != null;

        /// <summary>이어할 판이 저장된 맵. 게임 씬이 첫 맵을 깔았다가 복원 때 바꿔 끼우며 깜빡이지 않게, 처음부터 이 맵을 깐다. 이어하기가 아니면 null.</summary>
        public string PendingStageId => _pendingRestore?.summary?.stageId;

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
            GameEventSystem.Subscribe<ShopStateChangedEvent>(_OnShopStateChanged);
            GameEventSystem.Subscribe<GameOverStartEvent>(_OnGameOverStart);
            GameEventSystem.Subscribe<GameEndEvent>(_OnGameEnd);
        }

        private void _UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<GameEnterEvent>(_OnGameEnter);
            GameEventSystem.Unsubscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Unsubscribe<ShopStateChangedEvent>(_OnShopStateChanged);
            GameEventSystem.Unsubscribe<GameOverStartEvent>(_OnGameOverStart);
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
            // 상점 진열은 복원된 편성·강화 레벨로 만든 후보 풀에서 되찾으므로 그 둘 뒤에 둔다.
            RegisterSection(new ShopOfferSection());
            RegisterSection(new EliteCoinSection());
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

            _RequestSaveNextFrame();
        }

        // 상점 안의 구매·리롤도 저장 지점이다 — 이어하기가 플레이어가 마지막으로 본 상점 그대로(산 것·남은 코인·진열·리롤 상태)로 돌아오게.
        private void _OnShopStateChanged(ShopStateChangedEvent shopStateChangedEvent)
        {
            if (_isRestoring || _isLobbyRun)
                return;

            _RequestSaveNextFrame();
        }

        // 이 매니저는 앱 부팅 때 구독해 역 도착 이벤트를 가장 먼저 받는다. 그 자리에서 바로 저장하면
        // 역 도착이 하는 부활·체력 회복(MainTrain.OnInspectionStart)보다 앞서 "죽은 칸"이 그대로 저장되고,
        // 이어하기가 그 칸을 다시 죽이면서 첫 칸이 죽은 세이브는 복원 직후 게임오버가 됐다.
        // 역 도착 처리가 모두 끝난 다음 프레임에 저장해 상점에서 보이는 상태 그대로를 남긴다.
        // 상점 구매도 같다 — 포탑 구매가 스폰한 칸은 다음 프레임 Start()가 돌아야 편성 캡처가 온전하다.
        private void _RequestSaveNextFrame()
        {
            if (_isSaveScheduled)
                return;

            _isSaveScheduled = true;
            StartCoroutine(_SaveNextFrame());
        }

        private IEnumerator _SaveNextFrame()
        {
            yield return null;

            _isSaveScheduled = false;

            if (_isRestoring || _isLobbyRun)
                yield break;

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
                summary.stationPassedCount = stageManager.TotalInspectionPassedCount;
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

        private void _OnGameOverStart(GameOverStartEvent gameOverStartEvent)
        {
            // 패배는 슬로모션 연출이 끝나야 GameEndEvent가 나온다. 그 몇 초 사이에 앱을 끄면 세이브가 살아남아
            // 직전 역으로 되돌아가는(패배 회피) 구멍이 되므로, 패배가 확정되는 순간 지운다.
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
            // 이전 판의 복원이 연출 도중 끊겨 끝맺지 못했더라도 새 판의 저장까지 막히면 안 된다(DDOL이라 플래그가 앱 종료까지 남는다).
            _isRestoring = false;

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

            // 준비가 안 됐으면 복원만 건너뛰고 아래 상점 열기는 그대로 탄다. 첫 포탑 선택은 이미 이어하기로 건너뛰었기 때문에
            // 여기서 멈추면 선택창도 없고 시간도 멈춘 채(일시정지 버튼까지 잠김) 판이 얼어붙는다. 기본 편성으로 상점에서 시작하게 둔다.
            if (!_IsSceneReadyForRestore())
            {
                Debug.LogWarning("[RunSave] 복원에 필요한 매니저가 준비되지 않아 이어하기를 중단합니다. 새 판으로 진행합니다."
                                 + $" (UserDataManager={UserDataManager.Instance != null}, StageManager={Manager.StageManager.Instance != null},"
                                 + $" TrainManager={TrainManager.Instance != null}, MainTrain={TrainManager.Instance != null && TrainManager.Instance.MainTrain != null})");
            }
            else
            {
                foreach (var section in _sections)
                {
                    string json = data.GetSection(section.SectionId);

                    // 이 세이브에 없는 섹션(구버전 세이브 + 신규 시스템)은 건너뛴다 — 나머지 복원은 그대로 진행한다.
                    if (string.IsNullOrEmpty(json))
                        continue;

                    // 한 섹션이 예외를 던져도(삭제된 데이터 id·손상된 값) 나머지 섹션과 상점 열기는 진행한다.
                    // 코루틴이 예외로 끊기면 _isRestoring이 영영 true로 남아 이후 저장이 전부 막히고 화면도 멈춘 채가 된다.
                    try
                    {
                        section.Restore(json);
                    }
                    catch (System.Exception exception)
                    {
                        Debug.LogError($"[RunSave] 섹션 '{section.SectionId}' 복원 중 예외가 나 그 섹션을 건너뜁니다.");
                        Debug.LogException(exception);
                    }
                }
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
            // TrainManager.MainTrain은 스폰 전엔 프리팹 에셋을 가리켜 null이 아니다. 씬에 실제로 놓인 인스턴스인지까지 본다.
            return UserDataManager.Instance != null
                   && Manager.StageManager.Instance != null
                   && TrainManager.Instance != null
                   && TrainManager.Instance.MainTrain != null
                   && TrainManager.Instance.MainTrain.gameObject.scene.IsValid();
        }

        #endregion
    }
}
