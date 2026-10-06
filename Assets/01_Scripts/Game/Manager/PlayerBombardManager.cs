using Cumic;
using Cumic.Events;
using Cumic.Sequence;
using TrainDefense;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.Tutorial;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TrainDefense.Game
{
    /// <summary>
    /// 플레이어 개입 포격. 화면을 탭하면 그 지점에 포탄 한 발을 떨어뜨린다.
    /// 어떤 유닛에도 속하지 않으므로 발사 시 owner를 넘기지 않는다(플레이어 = 화면 밖 사령관).
    /// 기본 스탯은 PlayerBombardConfig에서 오고, 레벨업 강화(포격 피해·대기시간·범위)가 발사 시점에 배율로 얹힌다.
    /// </summary>
    public class PlayerBombardManager : Singleton<PlayerBombardManager>
    {
        private const string ConfigResourcePath = "Data/PlayerBombardConfig";
        private const string ProjectilePrefabPath = "Prefabs/Projectiles/TrainProjectile/";

        // 레벨업 강화 UpgradeData id (UpgradeValue = 레벨당 퍼센트, 대기시간은 음수 = 감소)
        private const string DAMAGE_UPGRADE_ID = "110008";
        private const string COOLDOWN_UPGRADE_ID = "110009";
        private const string AREA_UPGRADE_ID = "110010";

        private PlayerBombardConfig _config;
        private Projectile _projectilePrefab;
        private float _cooldownRemaining;

        // 발사마다 새로 만들지 않도록 하나를 재사용하고, 강화 배율만 발사 직전에 갱신한다.
        private TurretTrainStatus _status;

        // 탭 지점 UI 검사용 — 탭마다 새로 만들지 않도록 재사용한다.
        private PointerEventData _pointerEventData;
        private EventSystem _pointerEventSystem;
        private readonly System.Collections.Generic.List<RaycastResult> _uiRaycastResults = new System.Collections.Generic.List<RaycastResult>();

        public bool IsReady => _cooldownRemaining <= 0f;

        protected override void Awake()
        {
            base.Awake();

            _config = Resources.Load<PlayerBombardConfig>(ConfigResourcePath);
            if (_config == null)
            {
                // 에셋이 없으면 PlayerBombardConfig의 기본값으로 동작한다. (수치는 아직 임시)
                _config = ScriptableObject.CreateInstance<PlayerBombardConfig>();
            }

            _status = new TurretTrainStatus();
        }

        private void Update()
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= Time.deltaTime;

            if (_TryGetTapPosition(out Vector2 worldPosition))
                _TryFire(worldPosition);
        }

        /// <summary>
        /// 탭 지점에 포탄을 떨어뜨린다. 쿨다운 중이거나 전투 중이 아니면 아무것도 하지 않는다.
        /// </summary>
        private void _TryFire(Vector2 worldPosition)
        {
            if (_cooldownRemaining > 0f)
                return;

            // 전투 진행(Engage) 중에만 발사. 상점·일시정지·삼중택일 중에는 막는다.
            // 단, 공격 튜토리얼 중에는 게임이 멈춰 있어도 허용한다(발사로 튜토리얼 완료 → 게임 재개).
            bool tutorialFiringAllowed = TutorialManager.Instance != null
                && TutorialManager.Instance.CurrentSequenceId == "mainTrainAttackTutorial";

            if (InGameSequence.Instance != null && !InGameSequence.Instance.IsRunning && !tutorialFiringAllowed)
                return;

            Projectile prefab = _GetProjectilePrefab();
            if (prefab == null)
                return;

            Projectile projectile = ResourceManager.Instance.Spawn(prefab);
            if (projectile == null)
                return;

            projectile.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);

            // 몬스터 체력이 역마다 오르므로 포격 피해도 같은 배율을 따라가야 판 내내 "약한 몹 한 방" 역할이 유지된다.
            _status.AttackDamage = _config.Damage * _GetEnemyHpScale() * _GetUpgradeMultiplier(DAMAGE_UPGRADE_ID);
            _status.AttackArea = _config.AttackArea * _GetUpgradeMultiplier(AREA_UPGRADE_ID);

            // owner를 넘기지 않는다. Projectile의 아군 판정은 "몬스터가 쏜 게 아니면 기차를 안 때린다"라서
            // owner 없이도 내 기차는 맞지 않는다.
            TurretCombatFx.InitProjectile(projectile, _status, null, null);

            if (_config.KnockbackPower > 0f)
                projectile.SetRuntimeShove(_config.KnockbackPower, _config.KnockbackDuration);

            _PlayFireSound();

            _cooldownRemaining = _config.Cooldown * _GetUpgradeMultiplier(COOLDOWN_UPGRADE_ID);

            // 구 메인 터렛(MainTrain.Weapon)이 발행하던 이벤트를 개입 포격이 이어받는다.
            // 공격 튜토리얼이 이 이벤트로 완료 처리되므로 끊기면 튜토리얼이 영영 안 끝난다.
            GameEventSystem.Publish(new MainTrainFiredEvent());
        }

        private static float _GetEnemyHpScale()
        {
            return Manager.StageManager.Instance != null ? Manager.StageManager.Instance.GetHPScale() : 1f;
        }

        // 레벨업 강화 배율 = 1 + 레벨당 퍼센트 × 레벨. 데이터가 없으면 1 (골드 획득 강화와 같은 방식)
        private float _GetUpgradeMultiplier(string upgradeId)
        {
            if (DatabaseManager.Instance == null || UserDataManager.Instance == null)
                return 1f;

            UpgradeData upgradeData = DatabaseManager.Instance.GetUpgradeData(upgradeId);
            if (upgradeData == null)
                return 1f;

            int level = UserDataManager.Instance.GetUpgradeLevel(upgradeId);

            return 1f + upgradeData.UpgradeValue / 100f * level;
        }

        private Projectile _GetProjectilePrefab()
        {
            if (_projectilePrefab != null)
                return _projectilePrefab;

            if (string.IsNullOrEmpty(_config.ProjectilePrefabId))
                return null;

            var prefabObject = Resources.Load<GameObject>($"{ProjectilePrefabPath}{_config.ProjectilePrefabId}");
            if (prefabObject == null)
            {
                Debug.LogWarning($"PlayerBombardManager: Projectile Prefab not found at '{ProjectilePrefabPath}{_config.ProjectilePrefabId}'");

                return null;
            }

            if (!prefabObject.TryGetComponent(out _projectilePrefab))
                Debug.LogWarning($"PlayerBombardManager: '{_config.ProjectilePrefabId}'에 Projectile 컴포넌트가 없음");

            return _projectilePrefab;
        }

        private void _PlayFireSound()
        {
            if (_config.AttackSoundType == SoundType.None || SoundManager.Instance == null)
                return;

            SoundManager.Instance.PlaySFX(_config.AttackSoundType);
        }

        /// <summary>
        /// 이번 프레임에 탭이 시작됐고 그 누름이 UI 위가 아니면 true와 월드 좌표를 반환한다.
        /// 개입은 한 번 누를 때 한 발이므로 누름 시작 프레임만 본다. (메인 터렛의 누르는 동안 연사와 다르다)
        /// </summary>
        private bool _TryGetTapPosition(out Vector2 worldPosition)
        {
            worldPosition = default;

            Camera cam = Camera.main;
            if (cam == null)
                return false;

            Vector3 screenPosition;
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase != TouchPhase.Began)
                    return false;

                if (_IsPointerOverUI(touch.position))
                    return false;

                screenPosition = touch.position;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                if (_IsPointerOverUI(Input.mousePosition))
                    return false;

                screenPosition = Input.mousePosition;
            }
            else
            {
                return false;
            }

            Vector3 world = cam.ScreenToWorldPoint(screenPosition);
            worldPosition = new Vector2(world.x, world.y);

            return true;
        }

        // 탭 지점에 그 프레임 UI 레이캐스트를 직접 쏜다.
        // IsPointerOverGameObject(fingerId)는 새 Input System 모듈이 touchId(1부터)로 찾아 옛 fingerId(0부터)와 안 맞고,
        // 직전 프레임 상태라 누른 순간의 터치를 못 본다 — 폰에서 일시정지·포탑 정보를 눌러도 포격이 나갔다.
        private bool _IsPointerOverUI(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            if (_pointerEventData == null || _pointerEventSystem != eventSystem)
            {
                _pointerEventData = new PointerEventData(eventSystem);
                _pointerEventSystem = eventSystem;
            }

            _pointerEventData.position = screenPosition;
            _uiRaycastResults.Clear();
            eventSystem.RaycastAll(_pointerEventData, _uiRaycastResults);

            return _uiRaycastResults.Count > 0;
        }
    }
}
