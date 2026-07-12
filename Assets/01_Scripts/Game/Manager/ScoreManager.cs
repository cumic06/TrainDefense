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
      #region Variables
      private ScoreData _scoreData;
      private bool _isLobby;
      #endregion

      #region Fields
      [ShowInInspector, ReadOnly]
      public int NormalKillCount { get; private set; }

      [ShowInInspector, ReadOnly]
      public int EliteKillCount { get; private set; }

      public int TotalKillCount => NormalKillCount + EliteKillCount;

      [ShowInInspector, ReadOnly]
      public int CurrentScore { get; private set; }

      public int NormalScore => _scoreData != null ? NormalKillCount * _scoreData.normalKillScore : 0;

      public int EliteScore => _scoreData != null ? EliteKillCount * _scoreData.eliteKillScore : 0;

      /// <summary>일반 몬스터 1마리 처치 점수(결산 내역 표기용).</summary>
      public int NormalKillUnitScore => _scoreData != null ? _scoreData.normalKillScore : 0;

      /// <summary>엘리트 몬스터 1마리 처치 점수(결산 내역 표기용).</summary>
      public int EliteKillUnitScore => _scoreData != null ? _scoreData.eliteKillScore : 0;
      #endregion

      #region LifeCycle
      private void Start()
      {
         if (DatabaseManager.Instance != null)
            _scoreData = DatabaseManager.Instance.GetScoreData();

         _SubscribeEvents();
      }

      private void OnDestroy()
      {
         _UnsubscribeEvents();
      }
      #endregion

      #region Sub/UnSub
      private void _SubscribeEvents()
      {
         GameEventSystem.Subscribe<GameEnterEvent>(_OnGameEnter);
         GameEventSystem.Subscribe<MonsterDeadEvent>(_OnMonsterDead);
      }

      private void _UnsubscribeEvents()
      {
         GameEventSystem.Unsubscribe<GameEnterEvent>(_OnGameEnter);
         GameEventSystem.Unsubscribe<MonsterDeadEvent>(_OnMonsterDead);
      }
      #endregion

      private void _OnGameEnter(GameEnterEvent gameEnterEvent)
      {
         NormalKillCount = 0;
         EliteKillCount = 0;
         CurrentScore = 0;
         _isLobby = gameEnterEvent.IsLobby;

         GameEventSystem.Publish<ChangeKillCountUIEvent>(new(TotalKillCount));
      }

      private void _OnMonsterDead(MonsterDeadEvent monsterDeadEvent)
      {
         if (_scoreData == null || _isLobby)
            return;

         if (monsterDeadEvent.IsElite)
         {
            EliteKillCount++;
            CurrentScore += _scoreData.eliteKillScore;
         }
         else
         {
            NormalKillCount++;
            CurrentScore += _scoreData.normalKillScore;
         }

         GameEventSystem.Publish<ChangeKillCountUIEvent>(new(TotalKillCount));
      }
   }
}
