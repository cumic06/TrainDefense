using Cumic;
using Cumic.Events;
using Sirenix.OdinInspector;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense.Game.Manager
{
   public class ScoreManager : Singleton<ScoreManager>
   {
      private ScoreData _scoreData;

      [ShowInInspector, ReadOnly]
      public int CurrentScore { get; private set; }

      private void Start()
      {
         _scoreData = DatabaseManager.Instance.GetScoreData();

         GameEventSystem.Subscribe<GameEnterEvent>(_OnGameEnter);
         GameEventSystem.Subscribe<MonsterDeadEvent>(_OnMonsterDead);
      }

      private void OnDestroy()
      {
         GameEventSystem.Unsubscribe<GameEnterEvent>(_OnGameEnter);
         GameEventSystem.Unsubscribe<MonsterDeadEvent>(_OnMonsterDead);
      }

      private void _OnGameEnter(GameEnterEvent gameEnterEvent)
      {
         CurrentScore = 0;
      }

      private void _OnMonsterDead(MonsterDeadEvent monsterDeadEvent)
      {
         if (_scoreData == null)
            return;
         var beforeScore = CurrentScore;
         CurrentScore += monsterDeadEvent.IsElite ? _scoreData.eliteKillScore : _scoreData.normalKillScore;
         var afterScore = CurrentScore;
         GameEventSystem.Publish<ChangeScoreUIEvent>(new(beforeScore, afterScore));
      }
   }
}
