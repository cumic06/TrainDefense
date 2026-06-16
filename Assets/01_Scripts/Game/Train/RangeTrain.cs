namespace TrainDefense.Game
{
    /// <summary>
    /// (전환 과도기) 범위 기차 마커. 모든 로직은 Train + RangeAttackModule이 담당한다.
    /// 범위 기차는 직렬화 설정이 없어 마이그레이션은 컴포넌트 추가만 하면 된다.
    /// 검증 완료 후 이 클래스(컴포넌트)는 Train으로 교체해 제거 예정(Step 3b 후속).
    /// </summary>
    public class RangeTrain : Train
    {
    }
}
