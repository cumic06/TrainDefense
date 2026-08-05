using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game.RunSave
{
    /// <summary>
    /// 런(한 판) 세이브 DTO. PlayerPrefs 1키에 JSON으로 저장한다. (SkillTreeSaveData 선례)
    /// 본문은 <see cref="SectionEntry"/> 목록이라 저장 대상이 늘어도 이 클래스는 바뀌지 않는다 —
    /// 새 데이터는 새 섹션으로 붙고, 모르는 섹션은 무시된다.
    /// schemaVersion은 v1부터 명시해 이후 구조가 바뀌어도 마이그레이션 체인을 걸 수 있게 한다.
    /// </summary>
    [Serializable]
    public class RunSaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;

        /// <summary>저장 시각(Unix 초). 로비 이어하기 카드에 "언제 저장됐는지" 표기용.</summary>
        public long savedAtUnixSeconds;

        /// <summary>섹션을 파싱하지 않고도 로비에서 바로 읽을 수 있는 요약값.</summary>
        public RunSaveSummary summary = new();

        public List<SectionEntry> sections = new();

        [Serializable]
        public class SectionEntry
        {
            public string id;
            public string json;
        }

        /// <summary>로비 이어하기 UI 표기용 요약. 여기 값이 틀려도 복원 자체에는 영향이 없다.</summary>
        [Serializable]
        public class RunSaveSummary
        {
            public string stageId;
            public int playerLevel;
            public int stationPassedCount;
            public int score;
            public int trainCount;
        }

        public string GetSection(string sectionId)
        {
            if (string.IsNullOrEmpty(sectionId) || sections == null)
                return null;

            for (int i = 0; i < sections.Count; i++)
            {
                if (sections[i] != null && sections[i].id == sectionId)
                    return sections[i].json;
            }

            return null;
        }

        public void SetSection(string sectionId, string json)
        {
            if (string.IsNullOrEmpty(sectionId) || string.IsNullOrEmpty(json))
                return;

            sections ??= new List<SectionEntry>();

            for (int i = 0; i < sections.Count; i++)
            {
                if (sections[i] != null && sections[i].id == sectionId)
                {
                    sections[i].json = json;

                    return;
                }
            }

            sections.Add(new SectionEntry { id = sectionId, json = json });
        }

        public string ToJson() => JsonUtility.ToJson(this);

        /// <summary>파싱 실패·빈 문자열은 null을 반환한다 (세이브 로드는 외부 경계 — 예외를 삼키되 로그).</summary>
        public static RunSaveData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            RunSaveData data = null;
            try
            {
                data = JsonUtility.FromJson<RunSaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (data == null)
                return null;

            // 미래 버전 세이브는 구버전 로직으로 건드리지 않는다(반쯤 해석한 값으로 덮어쓰면 데이터가 영구 파괴된다).
            if (data.schemaVersion > CurrentSchemaVersion)
            {
                Debug.LogWarning($"[RunSave] 세이브 스키마 v{data.schemaVersion}이 현재 지원 버전 v{CurrentSchemaVersion}보다 높아 무시합니다.");

                return null;
            }

            data.sections ??= new List<SectionEntry>();
            data.summary ??= new RunSaveSummary();

            data._Migrate();

            return data;
        }

        // schemaVersion이 낮은 세이브를 현재 구조로 끌어올리는 자리. (v1이 최초 버전이라 아직 체인 없음)
        // 단계 함수는 한 번 배포되면 수정하지 않고, 잘못됐으면 다음 단계를 추가해 고친다.
        private void _Migrate()
        {
            // 예: if (schemaVersion < 2) { ...v1 → v2 변환... }
            schemaVersion = CurrentSchemaVersion;
        }
    }
}
