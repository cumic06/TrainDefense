using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense.Game
{
    public class Boss : Monster
    {
        protected override void OnDead()
        {
            // 보스 사망 이벤트 발행
            GameEventSystem.Publish(new BossDeadEvent(this));
            
            // 부모 OnDead 호출
            base.OnDead();
        }
    }
}
