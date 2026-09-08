namespace TrainDefense.Game.RunSave
{
    /// <summary>
    /// 런 세이브의 확장 지점. 한 시스템이 자기 상태 조각(섹션) 하나를 책임진다.
    /// 새 저장 대상이 생기면 이 인터페이스 구현체를 만들어 <see cref="RunSaveManager.RegisterSection"/>에 등록하기만 하면 된다.
    /// 섹션끼리는 서로를 모르며, 알 수 없는 섹션 id는 로드 시 조용히 건너뛰므로 구/신 버전 세이브가 공존해도 깨지지 않는다.
    /// </summary>
    public interface IRunStateSection
    {
        /// <summary>세이브 안에서 이 섹션을 식별하는 고정 문자열. 한 번 정하면 바꾸지 않는다(바꾸면 구 세이브의 해당 조각이 유실된다).</summary>
        string SectionId { get; }

        /// <summary>현재 상태를 JSON 문자열로 만든다. 저장할 것이 없으면 null을 반환해 섹션을 생략한다.</summary>
        string Capture();

        /// <summary>Capture가 만든 JSON을 되돌린다. 파싱 실패는 각 구현이 흡수하고 게임을 막지 않는다.</summary>
        void Restore(string json);
    }
}
