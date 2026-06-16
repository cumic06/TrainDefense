using UnityEditor;
using UnityEngine;
using TrainDefense.Game;

namespace TrainDefense.EditorTools
{
    /// <summary>
    /// 상속 → 컴포지션 전환용 일회성 프리팹 마이그레이션.
    /// 1) Range/Turret 기차 프리팹에 대응 AttackModule 컴포넌트를 추가한다.
    /// 2) TurretTrain(shell) 마커의 직렬화 설정(spawn point/turret/model/flag)을 TurretAttackModule로 복사한다.
    ///    (Range는 직렬화 설정이 없어 컴포넌트 추가만 하면 됨)
    ///
    /// 사용법: Unity 메뉴 → TrainDefense/Migration/Add AttackModules To Train Prefabs
    /// 실행 후 변경된 프리팹을 커밋할 것. (Step 3b 후속: 마커 컴포넌트를 Train으로 교체해 제거)
    /// </summary>
    public static class TrainCompositionMigration
    {
        // TurretTrain 마커 → TurretAttackModule 로 복사할 직렬화 필드(동일 이름).
        private static readonly string[] TurretConfigFields =
        {
            "turretProjectileSpawnPoints", "useParticleProjectile", "isTargeting", "turret", "turretModel"
        };

        [MenuItem("TrainDefense/Migration/Add AttackModules To Train Prefabs")]
        public static void AddAttackModules()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            int rangeChanged = 0, turretChanged = 0, configCopied = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool dirty = false;
                try
                {
                    if (root.GetComponent<RangeTrain>() != null && root.GetComponent<RangeAttackModule>() == null)
                    {
                        root.AddComponent<RangeAttackModule>();
                        rangeChanged++;
                        dirty = true;
                        Debug.Log($"[TrainCompositionMigration] +RangeAttackModule → {path}");
                    }

                    var turretShell = root.GetComponent<TurretTrain>();
                    if (turretShell != null)
                    {
                        var module = root.GetComponent<TurretAttackModule>();
                        if (module == null)
                        {
                            module = root.AddComponent<TurretAttackModule>();
                            turretChanged++;
                            dirty = true;
                            Debug.Log($"[TrainCompositionMigration] +TurretAttackModule → {path}");
                        }

                        if (CopyTurretConfig(turretShell, module))
                        {
                            configCopied++;
                            dirty = true;
                            Debug.Log($"[TrainCompositionMigration] config copied → {path}");
                        }
                    }

                    if (dirty)
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[TrainCompositionMigration] 완료. RangeModule +{rangeChanged}, TurretModule +{turretChanged}, config 복사 {configCopied}건.");
        }

        // TurretTrain 마커의 직렬화 필드를 같은 이름의 TurretAttackModule 필드로 복사(배열/오브젝트 참조 포함).
        private static bool CopyTurretConfig(TurretTrain shell, TurretAttackModule module)
        {
            var src = new SerializedObject(shell);
            var dst = new SerializedObject(module);
            bool any = false;

            foreach (var field in TurretConfigFields)
            {
                var sp = src.FindProperty(field);
                if (sp == null) continue;
                if (dst.FindProperty(field) == null) continue;
                // 같은 propertyPath의 값을 src → dst로 복사 (CopyFromSerializedProperty가 배열/참조 처리).
                dst.CopyFromSerializedProperty(sp);
                any = true;
            }

            if (any) dst.ApplyModifiedPropertiesWithoutUndo();
            return any;
        }
    }
}
