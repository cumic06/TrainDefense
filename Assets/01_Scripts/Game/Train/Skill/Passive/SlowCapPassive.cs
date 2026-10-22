namespace TrainDefense.Game
{
    /// <summary>
    /// 둔화율 최대값(RangeTrain.MaxSlowRate)을 올린다. 둔화율은 강화로 최대값까지만 오르므로, 최대값을 올려야 그 위로 자랄 수 있다. (엘리트 냉기 혹한)
    /// DSL: "SlowCap:points" — 최대값에 더하는 %p
    /// </summary>
    public class SlowCapPassive : TrainPassiveSkill
    {
        public float Points { get; private set; }

        public static SlowCapPassive From(string[] parts)
        {
            if (parts.Length < 2) return null;
            if (!TryParseFloat(parts[1], out float points) || points <= 0f) return null;
            return new SlowCapPassive { Points = points };
        }

        public override void Subscribe()
        {
            if (Owner is RangeTrain rangeTrain) rangeTrain.MaxSlowRate += Points;
        }

        public override void Unsubscribe()
        {
            if (Owner is RangeTrain rangeTrain) rangeTrain.MaxSlowRate -= Points;
        }
    }
}
