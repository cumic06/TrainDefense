namespace TrainDefense.Game
{
    /// <summary>
    /// 액티브/패시브 스킬 공통 베이스. Owner 보유, Tick 라이프사이클, 이벤트 Subscribe/Unsubscribe 훅 제공.
    /// 하위:
    ///   - TrainSkillAction (액티브: 쿨다운/TryUse)
    ///   - TrainPassiveSkill (패시브: Subscribe로 owner 이벤트 구독)
    /// </summary>
    public abstract class TrainSkill
    {
        protected Train Owner;

        public virtual void Tick(float deltaTime) { }

        /// <summary>owner의 이벤트/등록 API에 자기 핸들러 부착. 패시브가 주로 사용.</summary>
        public virtual void Subscribe() { }

        /// <summary>owner의 이벤트/등록 API에서 핸들러 분리. Train 사망/재초기화 시 호출.</summary>
        public virtual void Unsubscribe() { }
    }
}
