using Cumic.Events;
using TMPro;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense
{
    // 인게임 HUD에 누적 처치 수(일반 + 엘리트)를 표시한다. (이전: 스코어 표시 → 처치 수 표시로 변경)
    public class ScoreTextUI : MonoBehaviour
    {
      #region Field
      [SerializeField]
      private TextMeshProUGUI scoreText;
      #endregion

      #region LifeCycle
      private void Start()
      {
         _SubscribeEvents();
         _Setup();
      }

      private void OnDestroy()
      {
         _UnsubscribeEvents();
      }
      #endregion

      #region Sub/UnSub
      private void _SubscribeEvents()
      {
         GameEventSystem.Subscribe<ChangeKillCountUIEvent>(_OnChangeKillCount);
      }

      private void _UnsubscribeEvents()
      {
         GameEventSystem.Unsubscribe<ChangeKillCountUIEvent>(_OnChangeKillCount);
      }
      #endregion

      private void _Setup()
      {
         scoreText.text = "0";
      }

      private void _OnChangeKillCount(ChangeKillCountUIEvent changeKillCountEvent)
      {
         scoreText.text = changeKillCountEvent.KillCount.ToString();
      }
   }
}
