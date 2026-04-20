namespace TrainDefense.Game
{
    /// <summary>
    /// 엘리트 패시브 스킬 베이스. owner의 이벤트(OnAttacked/OnTargetPosAttacked/...) 또는
    /// 등록 API(RegisterProjectileOverride)에 핸들러를 부착하여 동작한다.
    /// 구체 패시브는 Subscribe/Unsubscribe만 override.
    /// </summary>
    public abstract class TrainPassiveSkill : TrainSkill
    {
        public virtual void Initialize(Train owner)
        {
            Owner = owner;
            Subscribe();
        }
    }
}
