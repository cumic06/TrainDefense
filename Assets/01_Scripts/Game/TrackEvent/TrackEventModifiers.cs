namespace TrainDefense.Game.TrackEvent
{
    /// <summary>
    /// 선로 이벤트가 전투 수치에 거는 전역 배율. 기본값은 모두 1(효과 없음).
    /// 효과(ITrackEventEffect)는 Begin에서 값을 바꾸고 End에서 반드시 Reset 계열로 되돌린다.
    /// 읽는 쪽(Monster·Train 등)은 기존 계산식에 곱하기만 한다 — 이벤트 종류를 알 필요가 없다.
    /// 한 구간에 이벤트는 최대 1개라 중첩 관리는 하지 않는다. 판이 끝나거나 역에 도착하면 TrackEventManager가 효과를 End시켜 원복된다.
    /// </summary>
    public static class TrackEventModifiers
    {
        #region Variables

        /// <summary>몬스터 처치 골드 배율 (골드 러시).</summary>
        public static float KillGoldMultiplier { get; set; } = 1f;

        /// <summary>몬스터 처치 경험치 배율 (경험치 순풍).</summary>
        public static float KillExpMultiplier { get; set; } = 1f;

        /// <summary>몬스터 이동속도 배율 (한파).</summary>
        public static float MonsterMoveSpeedMultiplier { get; set; } = 1f;

        /// <summary>포탑 공격 속도 배율 (과부하 운행). 2면 공격 간격이 절반.</summary>
        public static float TurretAttackSpeedMultiplier { get; set; } = 1f;

        /// <summary>포탑 사거리 배율 (모래폭풍).</summary>
        public static float TurretRangeMultiplier { get; set; } = 1f;

        #endregion

        /// <summary>모든 배율을 기본값으로 되돌린다. 씬 재시작·판 종료 안전망.</summary>
        public static void ResetAll()
        {
            KillGoldMultiplier = 1f;
            KillExpMultiplier = 1f;
            MonsterMoveSpeedMultiplier = 1f;
            TurretAttackSpeedMultiplier = 1f;
            TurretRangeMultiplier = 1f;
        }
    }
}
