using Cumic;
using Cumic.Events;
using DG.Tweening;
using TMPro;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense
{
    public class ScoreTextUI : MonoBehaviour
    {
      #region Field
      [SerializeField]
      private TextMeshProUGUI scoreText;
      [SerializeField]
      private float tweenDuration = 1f;
      #endregion

      private void Start()
      {
         GameEventSystem.Subscribe<ChangeScoreUIEvent>(OnChangeScore);
         Setup();
      }

      private void OnDestroy()
      {
         GameEventSystem.Unsubscribe<ChangeScoreUIEvent>(OnChangeScore);
      }

      private void Setup()
      {
         scoreText.text = $"0";
      }

      private void OnChangeScore(ChangeScoreUIEvent changeScoreEvent)
      {
         DOTween.To(() => changeScoreEvent.BeforeScore, x => scoreText.text = x.ToCommaString(), changeScoreEvent.AfterScore, tweenDuration)
         .SetEase(Ease.InOutSine)
         .SetUpdate(true);
      }
   }
}
