namespace TrainDefense.Game
{
    /// <summary>
    /// 블랙홀 포탑 개조(BlackHolePassive)가 포탑에 심는 블랙홀 변형 값. 착탄한 블랙홀 장판(BlackHoleField)이 소유 포탑에서 읽어 간다.
    /// 기본값(모두 0, 당김 배율 1, 당기기)이 개조 없는 기본 블랙홀이다.
    /// </summary>
    public class BlackHoleModifier
    {
        // 장판이 착탄 후 발사 방향으로 흘러가는 속도(유닛/초). 0이면 제자리 고정. (떠도는 블랙홀)
        public float DriftSpeed { get; set; }
        // 장판 유지 시간(초) 덮어쓰기. 0이면 프리팹 기본값.
        public float LifetimeOverride { get; set; }
        // 사라질 때 터지는 폭발 피해 = 공격력 × 이 배율. 0이면 폭발 없음. (붕괴)
        public float CollapseDamageMultiplier { get; set; }
        // 당기는(밀어내는) 힘 배율. (중력 증폭)
        public float PullMultiplier { get; set; } = 1f;
        // true면 당기지 않고 포탑 반대쪽으로 밀어낸다. (화이트홀)
        public bool IsRepulse { get; set; }

        public void Reset()
        {
            DriftSpeed = 0f;
            LifetimeOverride = 0f;
            CollapseDamageMultiplier = 0f;
            PullMultiplier = 1f;
            IsRepulse = false;
        }
    }
}
