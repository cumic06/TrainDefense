using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game.SkillTree
{
    /// <summary>
    /// 스킬트리 세이브 DTO. PlayerPrefs 1키에 JSON으로 저장한다 (AchievementSaveData 선례).
    /// schemaVersion을 v1부터 명시해 이후 구조가 바뀌어도 마이그레이션 체인을 걸 수 있게 한다.
    /// </summary>
    [Serializable]
    public class SkillTreeSaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public List<NodeLevelEntry> nodeLevels = new();

        [Serializable]
        public class NodeLevelEntry
        {
            public string nodeId;
            public int level;
        }

        public string ToJson() => JsonUtility.ToJson(this);

        /// <summary>파싱 실패·빈 문자열은 새 세이브로 폴백한다 (세이브 로드는 외부 경계 — 예외를 삼키되 로그).</summary>
        public static SkillTreeSaveData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return new SkillTreeSaveData();

            SkillTreeSaveData data = null;
            try
            {
                data = JsonUtility.FromJson<SkillTreeSaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (data == null) return new SkillTreeSaveData();
            if (data.nodeLevels == null) data.nodeLevels = new List<NodeLevelEntry>();

            data._Migrate();

            return data;
        }

        // schemaVersion이 낮은 세이브를 현재 구조로 끌어올리는 자리. (v1이 최초 버전이라 아직 체인 없음)
        private void _Migrate()
        {
            // 예: if (schemaVersion < 2) { ...v1 → v2 변환... }
            schemaVersion = CurrentSchemaVersion;
        }
    }
}
