using System.Collections.Generic;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Analytics;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.UI.Consent;
using UnityEngine;
#if FIREBASE_ANALYTICS
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
#endif

namespace TrainDefense.Game.Manager
{
    /// <summary>
    /// Firebase Analytics 통합 허브. EventBus(GameEventSystem) 이벤트를 구독해
    /// 플레이어 행동·선택·이탈 지점을 로깅한다.
    /// GDPR 옵트인 동의 게이트를 적용하며(동의 전 수집 정지),
    /// Firebase SDK 미import 상태에서도 컴파일되도록 FIREBASE_ANALYTICS 심볼로 가드한다.
    /// </summary>
    public class AnalyticsManager : Singleton<AnalyticsManager>
    {
        #region Variables
        // 동의 팝업 prefab. 첫 표시 때 ConsentCanvas(자식 Canvas) 아래에 1회 Instantiate(worldPositionStays=false)해서
        // 인스턴스를 만들고, 이후엔 SetActive로 재사용한다. (씬에 미리 배치할 필요 없음. ResourceManager.Spawn은
        // 내부 Instantiate가 world 위치를 강제해 RectTransform이 어긋나므로 쓰지 않고 직접 생성한다.)
        [SerializeField] private ConsentPopupUI _consentPopup;
        // _consentPopup 을 1회 생성한 런타임 인스턴스. 두 번째부터는 이 인스턴스를 재사용한다.
        private ConsentPopupUI _consentInstance;
#if FIREBASE_ANALYTICS
        private bool _firebaseReady;
#endif
        private float _gameStartTime;
        #endregion

        #region LifeCycle
        protected override void Awake()
        {
            base.Awake();

            if (Instance != this)
                return;

            _InitializeFirebase();
        }

        private void Start()
        {
            if (Instance != this)
                return;

            _SubscribeEvents();
            _PrepareConsentPopup();
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            _UnsubscribeEvents();
        }
        #endregion

        #region Sub/UnSub
        private void _SubscribeEvents()
        {
            GameEventSystem.Subscribe<LobbyEnterEvent>(_OnLobbyEnter);
            GameEventSystem.Subscribe<GameEnterEvent>(_OnGameEnter);
            GameEventSystem.Subscribe<GameOverStartEvent>(_OnGameOver);
            GameEventSystem.Subscribe<GameEndEvent>(_OnGameEnd);
            GameEventSystem.Subscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Subscribe<StageSelectEvent>(_OnStageSelect);
            GameEventSystem.Subscribe<TriChoiceSelectEvent>(_OnTriChoiceSelect);
            GameEventSystem.Subscribe<StatUpgradeSelectEvent>(_OnStatUpgradeSelect);
            GameEventSystem.Subscribe<ShopOfferPurchasedEvent>(_OnShopOfferPurchased);
            GameEventSystem.Subscribe<TrainRepairedEvent>(_OnTrainRepaired);
            GameEventSystem.Subscribe<PermanentUpgradePurchasedEvent>(_OnPermanentUpgrade);
            GameEventSystem.Subscribe<TrainSpawnedEvent>(_OnTrainSpawned);
            GameEventSystem.Subscribe<TutorialStartEvent>(_OnTutorialStart);
            GameEventSystem.Subscribe<TutorialCompleteEvent>(_OnTutorialComplete);
        }

        private void _UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<LobbyEnterEvent>(_OnLobbyEnter);
            GameEventSystem.Unsubscribe<GameEnterEvent>(_OnGameEnter);
            GameEventSystem.Unsubscribe<GameOverStartEvent>(_OnGameOver);
            GameEventSystem.Unsubscribe<GameEndEvent>(_OnGameEnd);
            GameEventSystem.Unsubscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Unsubscribe<StageSelectEvent>(_OnStageSelect);
            GameEventSystem.Unsubscribe<TriChoiceSelectEvent>(_OnTriChoiceSelect);
            GameEventSystem.Unsubscribe<StatUpgradeSelectEvent>(_OnStatUpgradeSelect);
            GameEventSystem.Unsubscribe<ShopOfferPurchasedEvent>(_OnShopOfferPurchased);
            GameEventSystem.Unsubscribe<TrainRepairedEvent>(_OnTrainRepaired);
            GameEventSystem.Unsubscribe<PermanentUpgradePurchasedEvent>(_OnPermanentUpgrade);
            GameEventSystem.Unsubscribe<TrainSpawnedEvent>(_OnTrainSpawned);
            GameEventSystem.Unsubscribe<TutorialStartEvent>(_OnTutorialStart);
            GameEventSystem.Unsubscribe<TutorialCompleteEvent>(_OnTutorialComplete);
        }
        #endregion

        #region Consent
        /// <summary>동의 결과를 영속화하고 즉시 Firebase 수집 상태에 반영한다. (동의 팝업/옵션 토글에서 호출)</summary>
        public void SetConsent(bool granted)
        {
            AnalyticsConsent.Set(granted ? AnalyticsConsentState.Granted : AnalyticsConsentState.Denied);
            _ApplyConsent();
        }

        private void _ApplyConsent()
        {
#if FIREBASE_ANALYTICS
            if (!_firebaseReady)
                return;

            bool granted = AnalyticsConsent.IsGranted;

            var consent = new Dictionary<ConsentType, ConsentStatus>
            {
                { ConsentType.AnalyticsStorage, granted ? ConsentStatus.Granted : ConsentStatus.Denied },
                { ConsentType.AdStorage, ConsentStatus.Denied },
                { ConsentType.AdUserData, ConsentStatus.Denied },
                { ConsentType.AdPersonalization, ConsentStatus.Denied },
            };

            FirebaseAnalytics.SetConsent(consent);
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(granted);
#endif
        }

        private void _OnLobbyEnter(LobbyEnterEvent lobbyEnterEvent)
        {
            if (AnalyticsConsent.IsDecided)
                return;

            _ShowConsentPopup();
        }

        // ConsentCanvas(AnalyticsManager 자식 Canvas) 아래에 동의 팝업을 미리 1회 소환해 두고 비활성으로 둔다.
        // worldPositionStays=false 로 생성해 prefab의 RectTransform(전체화면 stretch)을 그대로 보존한다.
        private void _PrepareConsentPopup()
        {
            if (_consentPopup == null || _consentInstance != null)
                return;

            var canvas = GetComponentInChildren<Canvas>(includeInactive: true);
            Transform parent = canvas != null ? canvas.transform : transform;
            _consentInstance = Instantiate(_consentPopup, parent, worldPositionStays: false);
            _consentInstance.gameObject.SetActive(false);
        }

        private void _ShowConsentPopup()
        {
            if (_consentInstance == null)
                _PrepareConsentPopup();

            if (_consentInstance == null)
            {
                Debug.LogWarning("[Analytics] 동의 팝업 prefab(_consentPopup)이 연결되지 않아 표시를 건너뜁니다.");

                return;
            }

            // 미리 만들어 둔 인스턴스를 켠다(Show 내부 SetActive(true)). 동의/거부 시 스스로 SetActive(false).
            _consentInstance.Show(SetConsent);
        }
        #endregion

        #region Firebase Init
        private void _InitializeFirebase()
        {
#if FIREBASE_ANALYTICS
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result != DependencyStatus.Available)
                {
                    Debug.LogError($"[Analytics] Firebase 의존성 오류: {task.Result}");

                    return;
                }

                _firebaseReady = true;
                _ApplyConsent();
            });
#endif
        }
        #endregion

        #region Event Handlers
        private void _OnGameEnter(GameEnterEvent gameEnterEvent)
        {
            if (gameEnterEvent.IsLobby)
                return;

            _gameStartTime = Time.unscaledTime;
            _Log("game_start", ("turret_id", _SelectedTurretId()));
        }

        private void _OnGameOver(GameOverStartEvent gameOverStartEvent)
        {
            _Log("game_over",
                ("stage", _CurrentStage()),
                ("play_time_sec", _PlayTimeSec()),
                ("kill_count", _KillCount()));
        }

        private void _OnGameEnd(GameEndEvent gameEndEvent)
        {
            _Log("game_end",
                ("is_clear", gameEndEvent.IsClear),
                ("survival_sec", _PlayTimeSec()),
                ("kill_normal", ScoreManager.Instance != null ? ScoreManager.Instance.NormalKillCount : 0),
                ("kill_elite", ScoreManager.Instance != null ? ScoreManager.Instance.EliteKillCount : 0),
                ("score", ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0),
                ("stage", _CurrentStage()),
                ("coin", _Coin()));
        }

        private void _OnInspectionStart(InspectionStartEvent inspectionStartEvent)
        {
            _Log("stage_reached",
                ("stage", _CurrentStage()),
                // 값은 누적 상점 수(역 도착 + 맵 선택). 난이도 축과 같은 값이라야 지표와 체감이 맞는다.
                ("total_station_passed", StageManager.Instance != null ? StageManager.Instance.TotalInspectionPassedCount : 0),
                ("play_time_sec", _PlayTimeSec()));
        }

        private void _OnStageSelect(StageSelectEvent stageSelectEvent)
        {
            _Log("stage_selected", ("stage_id", stageSelectEvent.SelectedStageData != null ? stageSelectEvent.SelectedStageData.Id : "none"));
        }

        private void _OnTriChoiceSelect(TriChoiceSelectEvent triChoiceSelectEvent)
        {
            var option = triChoiceSelectEvent.ChoiceOption;

            _Log("trichoice_selected",
                ("choice_type", _ChoiceType(option)),
                ("choice_id", option != null ? option.Id : "none"),
                ("choice_left", triChoiceSelectEvent.ChoiceLeftCount),
                ("stage", _CurrentStage()),
                ("play_time_sec", _PlayTimeSec()));
        }

        // 리워크 후 110xxx 스탯 업글은 레벨업 카드(무료)로 획득 — 이벤트 키도 의미에 맞게 교체.
        private void _OnStatUpgradeSelect(StatUpgradeSelectEvent statUpgradeSelectEvent)
        {
            _Log("stat_upgrade_select",
                ("upgrade_id", statUpgradeSelectEvent.UpgradeId),
                ("stage", _CurrentStage()),
                ("play_time_sec", _PlayTimeSec()));
        }

        // 역 상점 골드 구매(포탑/스탯 강화/엘리트 승격) — 옛 shop_purchase 로그의 후계.
        private void _OnShopOfferPurchased(ShopOfferPurchasedEvent shopOfferPurchasedEvent)
        {
            _Log("shop_purchase",
                ("offer_id", shopOfferPurchasedEvent.Option != null ? shopOfferPurchasedEvent.Option.Id : "none"),
                ("offer_type", _ChoiceType(shopOfferPurchasedEvent.Option)),
                ("cost", shopOfferPurchasedEvent.Cost),
                ("stage", _CurrentStage()),
                ("play_time_sec", _PlayTimeSec()));
        }

        private void _OnTrainRepaired(TrainRepairedEvent trainRepairedEvent)
        {
            _Log("train_repair",
                ("cost", trainRepairedEvent.Cost),
                ("total_station_passed", trainRepairedEvent.StationCount),
                ("stage", _CurrentStage()),
                ("play_time_sec", _PlayTimeSec()));
        }

        private void _OnPermanentUpgrade(PermanentUpgradePurchasedEvent permanentUpgradePurchasedEvent)
        {
            _Log("permanent_upgrade_purchase",
                ("upgrade_id", permanentUpgradePurchasedEvent.UpgradeId),
                ("cost", permanentUpgradePurchasedEvent.Cost),
                ("new_level", permanentUpgradePurchasedEvent.NewLevel));
        }

        private void _OnTrainSpawned(TrainSpawnedEvent trainSpawnedEvent)
        {
            _Log("train_spawned",
                ("train_id", trainSpawnedEvent.TrainId),
                ("stage", _CurrentStage()));
        }

        private void _OnTutorialStart(TutorialStartEvent tutorialStartEvent)
        {
            _Log("tutorial_begin", ("sequence_id", tutorialStartEvent.SequenceId));
        }

        private void _OnTutorialComplete(TutorialCompleteEvent tutorialCompleteEvent)
        {
            _Log("tutorial_complete", ("sequence_id", tutorialCompleteEvent.SequenceId));
        }
        #endregion

        #region Context Helpers
        private string _CurrentStage()
            => StageManager.Instance != null && StageManager.Instance.CurrentStageData != null
                ? StageManager.Instance.CurrentStageData.Id
                : "none";

        private int _PlayTimeSec()
            => _gameStartTime > 0f ? Mathf.RoundToInt(Time.unscaledTime - _gameStartTime) : 0;

        private int _KillCount()
            => ScoreManager.Instance != null ? ScoreManager.Instance.TotalKillCount : 0;

        private int _Coin()
            => UserDataManager.Instance != null ? UserDataManager.Instance.Coin : 0;

        private string _SelectedTurretId()
        {
            string id = UserDataManager.Instance != null ? UserDataManager.Instance.SelectedTurretId : null;

            return string.IsNullOrEmpty(id) ? "none" : id;
        }

        private string _ChoiceType(IChoiceOption option)
        {
            return option switch
            {
                null => "none",
                AddTrainChoice => "add_train",
                UpgradeTrainChoice => "upgrade_train",
                EliteTrainChoice => "elite_train",
                StatUpgradeChoice => "stat_upgrade",
                TrainStatUpgradeChoice => "train_stat_upgrade",
                GoldRewardChoice => "gold_reward",
                RewardChoiceBase => "reward",
                _ => option.GetType().Name,
            };
        }
        #endregion

        #region Logging
        private void _Log(string eventName, params (string key, object value)[] parameters)
        {
#if FIREBASE_ANALYTICS
            if (!_firebaseReady || !AnalyticsConsent.IsGranted)
                return;

            if (parameters == null || parameters.Length == 0)
            {
                FirebaseAnalytics.LogEvent(eventName);

                return;
            }

            var firebaseParams = new Parameter[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                firebaseParams[i] = _ToParameter(parameters[i].key, parameters[i].value);
            }

            FirebaseAnalytics.LogEvent(eventName, firebaseParams);
#endif
        }

#if FIREBASE_ANALYTICS
        private Parameter _ToParameter(string key, object value)
        {
            switch (value)
            {
                case int i: return new Parameter(key, (long)i);
                case long l: return new Parameter(key, l);
                case bool b: return new Parameter(key, b ? 1L : 0L);
                case float f: return new Parameter(key, (double)f);
                case double d: return new Parameter(key, d);
                default: return new Parameter(key, value != null ? value.ToString() : "");
            }
        }
#endif
        #endregion
    }
}
