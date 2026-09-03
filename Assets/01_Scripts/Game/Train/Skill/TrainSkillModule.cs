using System.Collections.Generic;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game
{
    /// <summary>
    /// Train의 패시브 스킬 책임을 분리한 컴포지션 모듈.
    /// </summary>
    public class TrainSkillModule
    {
        private Train _owner;
        private readonly List<TrainPassiveSkill> _passives = new();

        public void Initialize(Train owner)
        {
            // 재초기화 시 기존 구독 해제
            for (int i = 0; i < _passives.Count; i++) _passives[i].Unsubscribe();
            _passives.Clear();

            _owner = owner;
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _passives.Count; i++)
            {
                _passives[i].Tick(deltaTime);
            }
        }

        public void RegisterPassiveFromData(Datas.TrainPassiveSkillData data)
        {
            if (data == null) return;
            RegisterPassivesFromRaw(data.ToDsl());
        }

        public void RegisterPassivesFromRaw(string raw)
        {
            var skills = TrainPassiveSkillFactory.ParseAll(raw);
            foreach (var p in skills)
            {
                p.Initialize(_owner);
                _passives.Add(p);
            }
        }

        /// <summary>모든 스킬 Unsubscribe + 클리어. Train.OnDestroy에서 호출.</summary>
        public void Dispose()
        {
            for (int i = 0; i < _passives.Count; i++) _passives[i].Unsubscribe();
            _passives.Clear();
        }
    }
}
