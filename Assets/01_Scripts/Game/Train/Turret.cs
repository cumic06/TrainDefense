using System.Collections.Generic;
using Cumic;
using DG.Tweening;
using TrainDefense;
using TrainDefense.Game.Datas;
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
        /// <summary>
        /// aim(월드 좌표) 방향으로 1회 발사한다. MainTrain이 쿨다운마다 호출한다.
        /// </summary>
        public void Fire(Vector2 aimPosition)
        {
            if (!_isReady)
                return;

            if (_isRotateTurret && _pivot != null)
                _pivot.LookAt2D(aimPosition);

            _PlayPunch();
            _PlayFireSound();

            ProjectileData baseData = _projectilePrefab.GetData();
            float spreadAngle = baseData != null ? baseData.SpreadAngle : 0f;
            int count = Mathf.Max(1, _status.AttackCount);

            for (int i = 0; i < count; i++)
            {
                Projectile projectile = _SpawnProjectile(i);
                if (projectile == null)
                    continue;

                projectile.transform.LookAt2D(aimPosition);

                if (spreadAngle > 0f && count > 1)
                {
                    float offset = (i - (count - 1) * 0.5f) * spreadAngle;
                    projectile.transform.Rotate(0f, 0f, offset);
                }
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
            bool scaleByArea = projectile.IsScaleByArea();
            projectile.Init(
               _status.AttackDamage,
               _owner,
               null,
               scaleByArea ? _status.AttackArea : 0f,
               _status.CriticalChance,
               _status.CriticalDamage,
               scaleByArea ? _status.AttackRange : 0f);

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

        private void _PlayPunch()
        {
            if (_model == null)
                return;

            _model.DOKill();
            // SetUpdate(true): 발사 직후 timeScale 변동(상점·일시정지 진입)에도 펀치가 원복되게 한다.
            _model.DOScale(_modelBaseScale * 0.9f, 0.1f).SetEase(Ease.OutBack).SetUpdate(true).OnComplete(() =>
            {
                _model.DOScale(_modelBaseScale, 0.1f).SetEase(Ease.InBack).SetUpdate(true);
            });
        }

        private void _PlayFireSound()
        {
            if (_data == null || _data.AttackSoundType == SoundType.None || SoundManager.Instance == null)
                return;

            SoundManager.Instance.PlaySFX(_data.AttackSoundType);
        }
        #endregion

        #region LifeCycle
        private void OnDestroy()
        {
            if (_model != null)
                _model.DOKill();
        }
        #endregion
    }
}
