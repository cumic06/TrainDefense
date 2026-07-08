using System.Collections.Generic;
using Cumic;
using Cumic.Events;
using DG.Tweening;
using TrainDefense;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.Stats;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// MainTrain 주무기 전용 경량 포탑.
    /// TurretTrain(Train 상속: 체력·스킬·콜라이더·이벤트)을 통째로 올리는 대신,
    /// 발사에 필요한 비주얼(회전 pivot·모델·스폰포인트)만 선택 포탑 프리팹에서 복제해 입히고
    /// aim(터치) 방향으로 발사한다. "모델만 씌우면 작동"하는 구조.
    /// </summary>
    public class Turret : MonoBehaviour
    {
        #region Field
        private TurretTrainData _data;
        private TurretTrainStatus _status;
        private Projectile _projectilePrefab;
        private IProjectileTarget _owner;

        private Transform _pivot;          // 조준 회전 pivot (복제된 turret 노드)
        private Transform _model;          // 발사 펀치 애니메이션 대상
        private Transform[] _spawnPoints;  // 투사체 스폰 위치
        private Vector3 _modelBaseScale = Vector3.one;
        private bool _isRotateTurret = true;
        private bool _isReady;
        #endregion

        // 공격 간격(초). MainTrain이 연속 발사 쿨다운으로 사용한다.
        public float AttackInterval => _status.AttackInterval;
        public bool IsReady => _isReady;

        #region Setup
        /// <summary>
        /// 선택 포탑 데이터로 비주얼을 입히고 발사를 준비한다. owner는 투사체 소유자(MainTrain).
        /// </summary>
        public void Setup(TurretTrainData data, IProjectileTarget owner)
        {
            if (data == null || data.Prefab == null)
            {
                Debug.LogWarning("Turret.Setup: data 또는 Prefab이 null");

                return;
            }

            _data = data;
            _owner = owner;
            _status = data.TurretTrainStatus;
            _projectilePrefab = data.TurretProjectilePrefab != null
               ? data.TurretProjectilePrefab.GetComponent<Projectile>()
               : null;

            if (!data.Prefab.TryGetComponent(out TurretTrain source))
            {
                Debug.LogWarning($"Turret.Setup: Prefab [{data.Id}]에 TurretTrain 컴포넌트가 없음");

                return;
            }

            _isRotateTurret = source.IsRotateTurret;
            _BuildVisual(source);
            _isReady = _projectilePrefab != null;

            // 메인 터렛도 편성 포탑과 동일하게 상점 업그레이드(공격력·공속·치명타 등)를 받는다.
            // 장착 시점에 이미 구매돼 있는 상점 레벨을 소급 적용(MainTrain.ApplyExistingUpgradesToTrain와 동일 방식).
            _ApplyExistingShopUpgrades();

            // 장착 시점의 플레이어 레벨까지 레벨 성장을 소급 적용하고, 이후 레벨업을 구독한다.
            _SyncLevelGrowth();
            GameEventSystem.Subscribe<LevelUpEvent>(_OnPlayerLevelUp);
        }

        // 선택 포탑 프리팹의 turret(회전 pivot) 서브트리만 복제해 자식으로 붙이고,
        // 복제본에서 모델·스폰포인트를 원본과 동일한 자식 인덱스 경로로 다시 찾는다.
        private void _BuildVisual(TurretTrain source)
        {
            GameObject visualRoot = source.TurretVisualRoot;
            if (visualRoot == null)
            {
                Debug.LogWarning($"Turret._BuildVisual: [{_data.Id}] turret 비주얼 루트가 없음");

                return;
            }

            GameObject clone = Instantiate(visualRoot, transform);
            _pivot = clone.transform;
            _pivot.localPosition = Vector3.zero;
            _pivot.localRotation = Quaternion.identity;

            Transform originalRoot = visualRoot.transform;
            Transform modelNode = source.TurretModelNode != null ? source.TurretModelNode.transform : null;
            _model = _MapToClone(originalRoot, modelNode, _pivot);
            if (_model != null)
                _modelBaseScale = _model.localScale;

            Transform[] spawnNodes = source.ProjectileSpawnPointNodes;
            if (spawnNodes != null && spawnNodes.Length > 0)
            {
                _spawnPoints = new Transform[spawnNodes.Length];
                for (int i = 0; i < spawnNodes.Length; i++)
                    _spawnPoints[i] = _MapToClone(originalRoot, spawnNodes[i], _pivot);
            }
        }

        // originalRoot 서브트리에서 target까지의 자식 인덱스 경로를 구해, 같은 경로로 clone 측 Transform을 찾는다.
        private static Transform _MapToClone(Transform originalRoot, Transform target, Transform cloneRoot)
        {
            if (target == null || originalRoot == null || cloneRoot == null)
                return null;

            if (target == originalRoot)
                return cloneRoot;

            List<int> path = new();
            Transform cursor = target;
            while (cursor != null && cursor != originalRoot)
            {
                path.Add(cursor.GetSiblingIndex());
                cursor = cursor.parent;
            }

            if (cursor != originalRoot)
                return null;

            Transform result = cloneRoot;
            for (int i = path.Count - 1; i >= 0; i--)
            {
                int index = path[i];
                if (index < 0 || index >= result.childCount)
                    return null;

                result = result.GetChild(index);
            }

            return result;
        }
        #endregion

        #region Fire
        // 조준점이 피벗과 수치상 겹칠 때(방향 벡터 ≈ 0)만 회전을 갱신하지 않는 최소 안전값.
        private const float AimDeadZoneRadius = 0.05f;

        /// <summary>
        /// 조준 지점을 바라보도록 포탑을 회전시킨다. (MainTrain이 조준 중 매 프레임 호출)
        /// 발사 순간에만 돌면 공속이 느릴 때 회전이 끊겨 보여서, 누르는 동안 계속 따라 돈다.
        /// </summary>
        public void AimAt(Vector2 aimPosition)
        {
            if (!_isRotateTurret || _pivot == null)
                return;

            if ((aimPosition - (Vector2)_pivot.position).sqrMagnitude < AimDeadZoneRadius * AimDeadZoneRadius)
                return;

            _pivot.LookAt2D(aimPosition);
        }

        /// <summary>
        /// aim(월드 좌표) 방향으로 1회 발사한다. MainTrain이 쿨다운마다 호출한다.
        /// </summary>
        public void Fire(Vector2 aimPosition)
        {
            if (!_isReady)
                return;

            TurretCombatFx.PlayAttackPunch(_model, _modelBaseScale);
            _PlayFireSound();

            ProjectileData baseData = _projectilePrefab.GetData();
            float spreadAngle = baseData != null ? baseData.SpreadAngle : 0f;
            int count = Mathf.Max(1, _status.AttackCount);

            // 방향은 하나뿐: 피벗→마우스. 포탑도 이 방향을 보고(AimAt), 총알도 이 방향으로 나간다.
            // 마우스 위치를 "통과해야 할 지점"으로 취급하지 않는다 — 방향 지시자일 뿐.
            Vector2 aimDirection = aimPosition - (Vector2)(_pivot != null ? _pivot.position : transform.position);
            Quaternion fireRotation = aimDirection.sqrMagnitude > AimDeadZoneRadius * AimDeadZoneRadius
                ? Quaternion.Euler(0f, 0f, Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg)
                : (_pivot != null ? _pivot.rotation : Quaternion.identity);

            for (int i = 0; i < count; i++)
            {
                Projectile projectile = _SpawnProjectile(i);
                if (projectile == null)
                    continue;

                projectile.transform.rotation = fireRotation;

                TurretCombatFx.ApplySpread(projectile.transform, i, count, spreadAngle);
            }
        }

        private Projectile _SpawnProjectile(int index)
        {
            Transform spawnPoint = _GetSpawnPoint(index);
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;

            Projectile projectile = ResourceManager.Instance.Spawn(_projectilePrefab, spawnPosition, Quaternion.identity);
            if (projectile == null)
                return null;

            ProjectileData projData = projectile.GetData();
            bool isNonMovement = projData != null && projData.MovementType == MovementType.NonMovement;

            // NonMovement(빔·화염 등)는 스폰포인트에 부착해 포탑을 따라가게 한다. Linear/TargetPos는 자유 비행.
            if (isNonMovement && spawnPoint != null)
            {
                projectile.transform.SetParent(spawnPoint);
                projectile.transform.localPosition = Vector3.zero;
                projectile.transform.localRotation = Quaternion.identity;
            }

            // target이 없어도 Init을 호출해 _movementStrategy를 생성한다 → 발사 방향으로 실제 비행.
            // (Init이 누락되면 투사체가 초기화되지 않아 제자리에 멈추고 데미지도 0이 된다.)
            TurretCombatFx.InitProjectile(projectile, _status, _owner, null);

            return projectile;
        }

        private Transform _GetSpawnPoint(int index)
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0)
                return _pivot;

            if (index < 0 || index >= _spawnPoints.Length)
                return _spawnPoints[0];

            return _spawnPoints[index];
        }

        private void _PlayFireSound()
        {
            if (_data == null || _data.AttackSoundType == SoundType.None || SoundManager.Instance == null)
                return;

            SoundManager.Instance.PlaySFX(_data.AttackSoundType);
        }
        #endregion

        #region ShopUpgrade
        // 상점 업그레이드를 편성 포탑(TurretTrain.ApplyStatLevelAware)과 동일한 정규화 곱셈으로 _status에 반영한다.
        // 메인 터렛은 Train을 상속하지 않아 편성 강화 파이프라인 밖이므로 같은 계산을 여기서 미러링한다.
        // (풀링 side-effect는 없음 — 매 발사 투사체를 새로 스폰해 _status를 즉시 반영한다.)

        /// <summary>
        /// 상점 업그레이드 스탯을 현재 레벨 기준으로 _status에 적용한다. (장착 시 소급 / MainTrain.ApplyUpgrade가 구매 시 호출)
        /// prevLevel→newLevel 정규화로 반복 구매 시 이중 적용을 막는다.
        /// </summary>
        public void ApplyShopStats(IStat[] stats, int newLevel, int prevLevel = 0)
        {
            if (stats == null || stats.Length == 0)
                return;

            foreach (var stat in stats)
                _ApplyShopStat(stat, newLevel, prevLevel);
        }

        private void _ApplyShopStat(IStat stat, int newLevel, int prevLevel)
        {
            if (stat == null)
                return;

            TurretTrainStatus baseStatus = _data.TurretTrainStatus;
            float percent = stat.Value / 100f;
            int times = newLevel - prevLevel;

            switch (stat.Type)
            {
                case StatType.AttackRange:
                    _status.AttackRange = _status.AttackRange / (1f + percent * prevLevel) * (1f + percent * newLevel);
                    break;

                case StatType.AttackArea:
                    _status.AttackArea = _status.AttackArea / (1f + percent * prevLevel) * (1f + percent * newLevel);
                    break;

                case StatType.AttackDamage:
                    _status.AttackDamage = _status.AttackDamage / (1f + percent * prevLevel) * (1f + percent * newLevel);
                    break;

                case StatType.AttackCount:
                    _status.AttackCount += Mathf.RoundToInt(baseStatus.AttackCount * percent * newLevel)
                                         - Mathf.RoundToInt(baseStatus.AttackCount * percent * prevLevel);
                    break;

                case StatType.AttackInterval:
                    _status.AttackInterval = _status.AttackInterval * (1f + (-percent) * prevLevel) / (1f + (-percent) * newLevel);
                    break;

                case StatType.TargetCount:
                    _status.TargetCount += Mathf.RoundToInt(baseStatus.TargetCount * percent * newLevel)
                                         - Mathf.RoundToInt(baseStatus.TargetCount * percent * prevLevel);
                    break;

                case StatType.CriticalChance:
                    _status.CriticalChance += stat.Value * times;
                    break;

                case StatType.CriticalDamage:
                    _status.CriticalDamage += stat.Value * times;
                    break;
            }
        }

        // 장착 시점에 이미 구매돼 있는 상점 업그레이드(UserDataManager._upgradeLevels)를 소급 적용한다.
        private void _ApplyExistingShopUpgrades()
        {
            if (UserDataManager.Instance == null || DatabaseManager.Instance == null)
                return;

            var upgradeIds = UserDataManager.Instance.GetAllUpgradeIds();
            if (upgradeIds == null)
                return;

            foreach (var upgradeId in upgradeIds)
            {
                if (string.IsNullOrEmpty(upgradeId))
                    continue;

                int level = UserDataManager.Instance.GetUpgradeLevel(upgradeId);
                if (level <= 0)
                    continue;

                UpgradeData upgradeData = DatabaseManager.Instance.GetUpgradeData(upgradeId);
                if (upgradeData == null || upgradeData.UpgradeDataType != UpgradeDataType.TrainUpgrade)
                    continue;

                ApplyShopStats(upgradeData.Stats, level);
            }
        }
        #endregion

        #region LevelGrowth
        // 플레이어 레벨 성장: 레벨당 공격력 +5% 가산(lv1=×1.0, base×(1+0.05×(lv-1))).
        // 상점 배율과 곱으로 중첩된다. 편성 포탑은 레벨업 카드로 성장하지만 메인 터렛은 카드가 없어 이 축이 대신한다.
        private const float LevelUpDamagePercent = 0.05f;
        private int _appliedPlayerLevel = 1;

        private void _OnPlayerLevelUp(LevelUpEvent _) => _SyncLevelGrowth();

        // 현재 플레이어 레벨까지의 성장 배율을 정규화 곱셈으로 _status에 반영한다.
        // _appliedPlayerLevel 기준 재계산이라 이벤트가 한 번에 여러 번 발행돼도 이중 적용이 없다.
        private void _SyncLevelGrowth()
        {
            if (_data == null || UserDataManager.Instance == null)
                return;

            int currentLevel = UserDataManager.Instance.CurrentLevel;
            if (currentLevel == _appliedPlayerLevel)
                return;

            _status.AttackDamage = _status.AttackDamage
                / (1f + LevelUpDamagePercent * (_appliedPlayerLevel - 1))
                * (1f + LevelUpDamagePercent * (currentLevel - 1));
            _appliedPlayerLevel = currentLevel;
        }
        #endregion

        #region LifeCycle
        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<LevelUpEvent>(_OnPlayerLevelUp);

            if (_model != null)
                _model.DOKill();
        }
        #endregion
    }
}
