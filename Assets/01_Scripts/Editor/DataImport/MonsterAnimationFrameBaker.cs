#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using TrainDefense.Game.Datas;

namespace TrainDefense.Editor.DataImport
{
    /// <summary>
    /// 각 몬스터 프리팹의 기본(Run) 애니메이션 클립에서 스프라이트 프레임을 추출해
    /// MonsterData.animationFrames에 저장한다. 도감 그리드는 첫 프레임만, 상세는 전체 프레임을 재생한다.
    /// AnimationClip의 스프라이트 키프레임은 에디터 전용 API로만 읽히므로, 런타임이 아닌 베이크 단계에서 처리한다.
    /// </summary>
    public static class MonsterAnimationFrameBaker
    {
        private const string DatabaseAssetPath = "Assets/Resources/Data/DB.asset";
        private const string MonsterPrefabFormat = "Assets/Resources/Prefabs/Monsters/{0}.prefab";

        private static readonly FieldInfo AnimationFramesField =
            typeof(MonsterData).GetField("animationFrames", BindingFlags.Instance | BindingFlags.NonPublic);

        [MenuItem("Tools/Bake Monster Animation Frames")]
        public static void BakeFromMenu()
        {
            var db = AssetDatabase.LoadAssetAtPath<DB>(DatabaseAssetPath);
            if (db == null)
            {
                Debug.LogError($"[MonsterAnimationFrameBaker] DB를 찾을 수 없습니다: {DatabaseAssetPath}");
                return;
            }

            int baked = Bake(db);
            AssetDatabase.SaveAssets();
            Debug.Log($"[MonsterAnimationFrameBaker] 몬스터 애니메이션 프레임 베이크 완료: {baked}/{db.monsterDataList.Count}");
        }

        /// <summary>
        /// DB 안의 모든 MonsterData에 프레임을 베이크한다. db를 dirty 처리하지만 SaveAssets 호출은 호출자 책임이다.
        /// </summary>
        public static int Bake(DB db)
        {
            if (db == null || AnimationFramesField == null) return 0;

            int baked = 0;
            foreach (MonsterData data in db.monsterDataList)
            {
                if (data == null) continue;

                Sprite[] frames = ExtractFrames(data.PrefabId);
                if (frames == null || frames.Length == 0)
                {
                    Debug.LogWarning($"[MonsterAnimationFrameBaker] '{data.Id}' (prefab '{data.PrefabId}') 프레임 추출 실패 — 건너뜀");
                    continue;
                }

                AnimationFramesField.SetValue(data, frames);
                baked++;
            }

            EditorUtility.SetDirty(db);
            return baked;
        }

        private static Sprite[] ExtractFrames(string prefabId)
        {
            if (string.IsNullOrEmpty(prefabId)) return null;

            string path = string.Format(MonsterPrefabFormat, prefabId);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return null;

            var animator = prefab.GetComponentInChildren<Animator>(true);
            if (animator == null) return null;

            AnimationClip clip = ResolveDefaultClip(animator.runtimeAnimatorController);
            if (clip == null) return null;

            return ExtractSprites(clip);
        }

        /// <summary>Animator의 기본 상태(보통 Run/걷기)에 연결된 클립을 반환한다.</summary>
        private static AnimationClip ResolveDefaultClip(RuntimeAnimatorController rac)
        {
            var controller = rac as AnimatorController;
            if (controller == null && rac is AnimatorOverrideController aoc)
                controller = aoc.runtimeAnimatorController as AnimatorController;
            if (controller == null || controller.layers.Length == 0) return null;

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            if (sm == null || sm.defaultState == null) return null;

            return sm.defaultState.motion as AnimationClip;
        }

        /// <summary>클립의 SpriteRenderer.m_Sprite 키프레임에서 순서대로 스프라이트를 뽑는다.</summary>
        private static Sprite[] ExtractSprites(AnimationClip clip)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            foreach (EditorCurveBinding binding in bindings)
            {
                if (binding.propertyName != "m_Sprite") continue;

                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                Sprite[] sprites = keys
                    .Select(k => k.value as Sprite)
                    .Where(s => s != null)
                    .ToArray();

                if (sprites.Length > 0) return sprites;
            }
            return null;
        }
    }
}
#endif
