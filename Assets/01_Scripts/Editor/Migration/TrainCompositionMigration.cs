using UnityEditor;
using UnityEngine;
using TrainDefense.Game;

namespace TrainDefense.EditorTools
{
    /// <summary>
    /// 상속 → 컴포지션 전환(Step 2)용 일회성 프리팹 마이그레이션.
    /// Range/Turret 기차 프리팹에 대응 AttackModule 컴포넌트를 추가한다.
    /// (직렬화 설정은 *Train shell에 그대로 남아 모듈이 접근자로 읽으므로, 컴포넌트 추가만으로 충분 — 필드 값 복사 불필요)
    ///
    /// 사용법: Unity 메뉴 → TrainDefense/Migration/Add AttackModules To Train Prefabs
    /// 실행 후 변경된 프리팹을 커밋할 것.
    /// </summary>
    public static class TrainCompositionMigration
    {
        [MenuItem("TrainDefense/Migration/Add AttackModules To Train Prefabs")]
        public static void AddAttackModules()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            int rangeScanned = 0, rangeChanged = 0, turretScanned = 0, turretChanged = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool dirty = false;
                try
                {
                    if (root.GetComponent<RangeTrain>() != null)
                    {
                        rangeScanned++;
                        if (root.GetComponent<RangeAttackModule>() == null)
                        {
                            root.AddComponent<RangeAttackModule>();
                            rangeChanged++;
                            dirty = true;
                            Debug.Log($"[TrainCompositionMigration] Added RangeAttackModule → {path}");
                        }
                    }

                    if (root.GetComponent<TurretTrain>() != null)
                    {
                        turretScanned++;
                        if (root.GetComponent<TurretAttackModule>() == null)
                        {
                            root.AddComponent<TurretAttackModule>();
                            turretChanged++;
                            dirty = true;
                            Debug.Log($"[TrainCompositionMigration] Added TurretAttackModule → {path}");
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
            Debug.Log($"[TrainCompositionMigration] 완료. Range {rangeScanned}개중 {rangeChanged}개, Turret {turretScanned}개중 {turretChanged}개에 모듈 추가.");
        }
    }
}
