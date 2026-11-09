using System.Collections;
using System.Collections.Generic;
using Cumic.Events;
using Cumic.Sequence;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 첫 엘리트 적이 화면에 들어오면 엘리트 설명 튜토리얼(eliteMonsterTutorial)을 띄운다.
    /// 1) 나타난 엘리트를 가리키고 → 2) 같은 몬스터를 등장 가능한 엘리트 종류마다 카드 한 장씩 UI로 보여 주며 종류 수를 알리고
    /// → 3) 종류별 카드를 하나씩 짚으며 특징을 설명한다. 몬스터를 실제로 소환하지 않으므로 전투에 영향이 없고,
    /// 카드는 튜토리얼이 끝나면(스킵 포함) 바로 지운다.
    /// </summary>
    public class EliteMonsterTutorial : MonoBehaviour
    {
        #region Variables

        private const string SEQUENCE_ID = "eliteMonsterTutorial";
        private const string INTRO_STEP_ID = "22000";
        private const string COUNT_STEP_ID = "22001";
        // 종류별 설명 스텝 ID = 22100 + (int)EliteType (빨강 22100, 초록 22101, 파랑 22102, 노랑 22103)
        private const int VARIANT_STEP_ID_BASE = 22100;

        [Tooltip("엘리트가 화면 가장자리에서 이만큼(뷰포트 비율) 안쪽으로 들어와야 튜토리얼을 시작한다.")]
        [Range(0f, 0.4f)]
        [SerializeField] private float screenEnterMargin = 0.12f;

        [Header("종류 카드 UI")]
        [Tooltip("카드 배경(9-slice). 비우면 어두운 단색.")]
        [SerializeField] private Sprite cardBackground;
        [Tooltip("카드 줄 중심 위치(튜토리얼 캔버스 1920x1080 기준, 화면 중앙 = 0,0).")]
        [SerializeField] private Vector2 cardRowPosition = new Vector2(0f, 170f);

        #endregion

        #region Fields

        private Monster _pendingElite;
        private Coroutine _waitCoroutine;
        private TutorialSequenceData _sequence;
        private bool _isRunning;

        private readonly List<EliteVariant> _activeVariants = new();
        private EliteVariantShowcaseView _showcaseView;

        #endregion

        #region LifeCycle

        private void OnEnable()
        {
            _SubscribeEvents();
        }

        private void OnDisable()
        {
            _UnsubscribeEvents();
            _StopWaiting();
            _FinishTutorial();
        }

        #endregion

        #region Trigger

        private void _OnMonsterSpawned(MonsterSpawnedEvent monsterSpawnedEvent)
        {
            if (monsterSpawnedEvent == null || !monsterSpawnedEvent.IsElite)
                return;

            if (_isRunning || _pendingElite != null)
                return;

            TutorialManager tutorialManager = TutorialManager.Instance;

            if (tutorialManager == null || tutorialManager.IsTutorialCompleted(SEQUENCE_ID))
                return;

            Monster elite = _FindLatestSpawnedElite();

            if (elite == null)
                return;

            _pendingElite = elite;
            _waitCoroutine = StartCoroutine(_WaitEliteOnScreenThenStart());
        }

        // 스포너는 목록에 넣은 직후 MonsterSpawnedEvent를 발행하므로, 방금 나온 엘리트는 목록의 마지막이다.
        private Monster _FindLatestSpawnedElite()
        {
            MonsterSpawner spawner = MonsterSpawner.Instance;

            if (spawner == null)
                return null;

            IReadOnlyList<Monster> monsters = spawner.SpawnedMonsters;

            if (monsters.Count == 0)
                return null;

            Monster latest = monsters[monsters.Count - 1];

            return latest != null && latest.IsElite ? latest : null;
        }

        // 엘리트는 화면 밖에서 나오므로, 화면 안으로 들어와 보일 때까지 기다렸다가 시작한다.
        // 다른 튜토리얼·레벨업·역 도착 등으로 전투가 멈춰 있으면 풀릴 때까지 기다린다.
        private IEnumerator _WaitEliteOnScreenThenStart()
        {
            while (true)
            {
                if (!_IsPendingEliteAlive())
                {
                    // 화면에 들어오기 전에 죽거나 회수됐다 — 다음 엘리트 때 다시 시도한다.
                    _pendingElite = null;
                    _waitCoroutine = null;

                    yield break;
                }

                if (_CanStartNow())
                    break;

                yield return null;
            }

            _waitCoroutine = null;
            _StartTutorial();
        }

        private bool _IsPendingEliteAlive()
        {
            return _pendingElite != null && _pendingElite.IsActive && !_pendingElite.IsDead && _pendingElite.IsElite;
        }

        private bool _CanStartNow()
        {
            InGameSequence sequence = InGameSequence.Instance;

            if (sequence == null || sequence.CurrentBase != BasePhase.Engage || sequence.CurrentOverlays != OverlayPhase.None)
                return false;

            TutorialManager tutorialManager = TutorialManager.Instance;

            if (tutorialManager == null || tutorialManager.IsActive)
                return false;

            return _IsOnScreen(_pendingElite.transform.position);
        }

        private bool _IsOnScreen(Vector3 worldPosition)
        {
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
                return false;

            Vector3 viewport = mainCamera.WorldToViewportPoint(worldPosition);

            return viewport.x >= screenEnterMargin && viewport.x <= 1f - screenEnterMargin
                && viewport.y >= screenEnterMargin && viewport.y <= 1f - screenEnterMargin;
        }

        #endregion

        #region Tutorial Flow

        private void _StartTutorial()
        {
            TutorialManager tutorialManager = TutorialManager.Instance;

            if (tutorialManager == null || !tutorialManager.TryGetSequence(SEQUENCE_ID, out _sequence))
            {
                Debug.LogWarning($"[EliteMonsterTutorial] 시퀀스 '{SEQUENCE_ID}'가 TutorialManager에 등록되어 있지 않습니다.");
                _pendingElite = null;

                return;
            }

            _CollectActiveVariants();
            _PrepareSteps(_pendingElite.gameObject);

            tutorialManager.OnStepChanged += _OnStepChanged;
            tutorialManager.OnTutorialComplete += _OnTutorialComplete;
            _isRunning = true;

            if (!tutorialManager.StartTutorial(SEQUENCE_ID))
            {
                // 시작 실패(동시에 다른 튜토리얼 시작 등) — 되돌리고 다음 엘리트 때 다시 시도한다.
                _FinishTutorial();
            }
        }

        // 지금 실제로 나올 수 있는(가중치 > 0) 엘리트 종류. 같은 타입이 여러 번 있으면 처음 것만 쓴다.
        private void _CollectActiveVariants()
        {
            _activeVariants.Clear();

            EliteData eliteData = DatabaseManager.Instance != null ? DatabaseManager.Instance.GetEliteData() : null;

            if (eliteData == null)
                return;

            foreach (EliteVariant variant in eliteData.GetVariants())
            {
                if (variant == null || variant.weight <= 0f || _FindActiveVariant(variant.type) != null)
                    continue;

                _activeVariants.Add(variant);
            }
        }

        private EliteVariant _FindActiveVariant(EliteType type)
        {
            foreach (EliteVariant variant in _activeVariants)
            {
                if (variant.type == type)
                    return variant;
            }

            return null;
        }

        private void _PrepareSteps(GameObject introTarget)
        {
            foreach (TutorialStepData step in _sequence.Steps)
            {
                if (step.Id == INTRO_STEP_ID)
                {
                    step.SetRuntimeContext(introTarget);
                }
                else if (step.Id == COUNT_STEP_ID)
                {
                    step.SetRuntimeContext(null, false, _activeVariants.Count);
                }
                else if (_TryGetVariantType(step, out EliteType type))
                {
                    // 대상(종류 카드)은 종류 수 안내 스텝에서 카드를 만든 뒤 채운다. 나오지 않는 종류는 설명을 건너뛴다.
                    step.SetRuntimeContext(null, _FindActiveVariant(type) == null);
                }
            }
        }

        private bool _TryGetVariantType(TutorialStepData step, out EliteType type)
        {
            type = default;

            if (!int.TryParse(step.Id, out int stepId))
                return false;

            int typeIndex = stepId - VARIANT_STEP_ID_BASE;

            if (!System.Enum.IsDefined(typeof(EliteType), typeIndex))
                return false;

            type = (EliteType)typeIndex;

            return true;
        }

        private void _OnStepChanged(int index, TutorialStepData step)
        {
            if (step == null || step.Id != COUNT_STEP_ID || _showcaseView != null)
                return;

            _CreateShowcase(step);
        }

        // 처음 나타난 엘리트의 모습으로 종류별 카드를 만들어 오버레이(어둡게 덮는 패널 아래)에 붙인다.
        // 이 스텝은 화면에 그려지기 전에 호출되므로, 여기서 정한 대상(카드 줄·카드)이 곧바로 하이라이트된다.
        private void _CreateShowcase(TutorialStepData countStep)
        {
            var overlayView = FindAnyObjectByType<TutorialOverlayView>(FindObjectsInactive.Include);

            if (overlayView == null || _activeVariants.Count == 0)
                return;

            _showcaseView = EliteVariantShowcaseView.Create(_pendingElite, _activeVariants, cardBackground, cardRowPosition);
            overlayView.AttachBehindDimming(_showcaseView.Row);

            countStep.SetRuntimeContext(_showcaseView.Row.gameObject, false, _activeVariants.Count);

            foreach (EliteVariant variant in _activeVariants)
            {
                RectTransform card = _showcaseView.GetCard(variant.type);

                if (card != null)
                    _SetVariantStepTarget(variant.type, card.gameObject);
            }
        }

        private void _SetVariantStepTarget(EliteType type, GameObject target)
        {
            foreach (TutorialStepData step in _sequence.Steps)
            {
                if (_TryGetVariantType(step, out EliteType stepType) && stepType == type)
                    step.SetRuntimeContext(target);
            }
        }

        private void _OnTutorialComplete(string sequenceId)
        {
            if (sequenceId != SEQUENCE_ID)
                return;

            _FinishTutorial();
        }

        // 완료·스킵·시작 실패·비활성화 모두 이곳으로 정리한다.
        private void _FinishTutorial()
        {
            TutorialManager tutorialManager = TutorialManager.Instance;

            if (tutorialManager != null)
            {
                tutorialManager.OnStepChanged -= _OnStepChanged;
                tutorialManager.OnTutorialComplete -= _OnTutorialComplete;
            }

            _DestroyShowcase();

            if (_sequence != null)
            {
                foreach (TutorialStepData step in _sequence.Steps)
                    step.ClearRuntimeContext();
            }

            _sequence = null;
            _pendingElite = null;
            _isRunning = false;
        }

        private void _DestroyShowcase()
        {
            if (_showcaseView != null)
                Destroy(_showcaseView.gameObject);

            _showcaseView = null;
        }

        private void _StopWaiting()
        {
            if (_waitCoroutine == null)
                return;

            StopCoroutine(_waitCoroutine);
            _waitCoroutine = null;
        }

        #endregion

        #region Sub/UnSub

        private void _SubscribeEvents()
        {
            GameEventSystem.Subscribe<MonsterSpawnedEvent>(_OnMonsterSpawned);
        }

        private void _UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<MonsterSpawnedEvent>(_OnMonsterSpawned);
        }

        #endregion
    }
}
