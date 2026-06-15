using UnityEditor;
using UnityEngine;
using TrainDefense.Game;

namespace TrainDefense.EditorTools
{
    /// <summary>
    /// 상속 → 컴포지션 전환(Step 2)용 일회성 프리팹 마이그레이션.
    /// RangeTrain 프리팹에 RangeAttackModule 컴포넌트를 추가한다.
    /// (RangeTrain은 직렬화 참조가 없어 컴포넌트 추가만으로 충분 — 필드 값 복사 불필요)
    ///
    /// 사용법: Unity 메뉴 → TrainDefense/Migration/Add RangeAttackModule To Range Prefabs
    /// 실행 후 프리팹을 커밋할 것.
    /// </summary>
    public static class TrainCompositionMigration
    {
        [MenuItem("TrainDefense/Migration/Add RangeAttackModule To Range Prefabs")]
        public static void AddRangeModules()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            int scanned = 0, changed = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var range = root.GetComponent<RangeTrain>();
                    if (range == null) continue;

                    scanned++;
                    if (root.GetComponent<RangeAttackModule>() != null) continue;

                    root.AddComponent<RangeAttackModule>();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changed++;
                    Debug.Log($"[TrainCompositionMigration] Added RangeAttackModule → {path}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[TrainCompositionMigration] 완료. RangeTrain 프리팹 {scanned}개 중 {changed}개에 모듈 추가.");
        }
    }
}
