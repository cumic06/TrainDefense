using UnityEngine;

namespace TrainDefense.Game.Datas
{
   public class ChoiceUIInfo
   {
      public Sprite Icon;
      public string Name;
      public string Description;
      public string PassiveName;
      public string PassiveDescription;
      // 강화 카드의 등급(1~5). 0이면 등급 없는 카드 — 카드 테두리에 등급 색을 입히지 않는다.
      public int Grade;
   }
}
