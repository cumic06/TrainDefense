using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 엘리트 패시브 스킬 베이스. owner의 이벤트(OnAttacked 등) 또는
    /// 등록 API(RegisterProjectileOverride)에 핸들러를 부착하여 동작한다.
    /// 구체 패시브는 Subscribe/Unsubscribe만 override.
    /// </summary>
    public abstract class TrainPassiveSkill : TrainSkill
    {
        public virtual void Initialize(Train owner)
        {
            Owner = owner;
            Subscribe();
        }

        // 데이터 숫자는 기기 언어와 상관없이 '.'을 소수점으로 읽는다. (쉼표 소수점 언어에서 "1.3"이 13이나 실패로 읽히는 것 방지)
        protected static bool TryParseFloat(string text, out float value)
            => float.TryParse(text?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        private const string PROJECTILE_PREFAB_PATH = "Prefabs/Projectiles/TrainProjectile/";
        // 프리팹 id → 불러온 투사체(없으면 null). 실패도 기억해 매 공격마다 다시 찾지 않는다.
        private static readonly Dictionary<string, Projectile> _projectilePrefabCache = new();

        // 데이터에 적힌 투사체 프리팹을 Resources에서 한 번만 불러 쓴다.
        protected Projectile LoadProjectilePrefab(string prefabId)
        {
            if (string.IsNullOrEmpty(prefabId)) return null;
            if (_projectilePrefabCache.TryGetValue(prefabId, out Projectile cached)) return cached;

            Projectile prefab = null;
            var go = Resources.Load<GameObject>(PROJECTILE_PREFAB_PATH + prefabId);
            if (go == null)
                Debug.LogWarning($"{GetType().Name}: prefab not found at '{PROJECTILE_PREFAB_PATH}{prefabId}'");
            else
                go.TryGetComponent(out prefab);

            _projectilePrefabCache[prefabId] = prefab;
            return prefab;
        }
    }
}
