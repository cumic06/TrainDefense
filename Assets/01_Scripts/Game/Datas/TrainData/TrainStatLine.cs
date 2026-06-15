namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 트라이초이스 카드에 표시할 스탯 한 줄(표시 전용 데이터).
    /// 실제 지역화/정렬 포맷은 UI(TriChoiceSelectUI)가 키로 처리한다.
    /// </summary>
    public struct TrainStatLine
    {
        /// <summary>변화 표시(현재→다음)용 지역화 키. 베이스 스탯 표시는 null.</summary>
        public string UpgradeKey;
        /// <summary>스탯 값 표시용 지역화 키.</summary>
        public string StatKey;
        public float CurrentValue;
        /// <summary>변화량. 0이면 변화 없음(베이스 표시).</summary>
        public float Delta;

        // 베이스 스탯 한 줄(값만).
        public TrainStatLine(string statKey, float currentValue)
        {
            UpgradeKey = null;
            StatKey = statKey;
            CurrentValue = currentValue;
            Delta = 0f;
        }

        // 업그레이드 미리보기 한 줄(현재값 + 변화량).
        public TrainStatLine(string upgradeKey, string statKey, float currentValue, float delta)
        {
            UpgradeKey = upgradeKey;
            StatKey = statKey;
            CurrentValue = currentValue;
            Delta = delta;
        }

        // 공격 간격(초) → 공격 속도(초당 횟수). 표시 전용.
        public static float ToAttackSpeed(float interval) => interval > 0f ? 1f / interval : 0f;
    }
}
