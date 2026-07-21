using UnityEngine;

namespace TrainDefense.Game.Datas
{
   // 엘리트 타입별 변형 정의. 스탯 배율 + 불 이펙트 색 + 타입 고유 능력 파라미터를 담는다.
   // EliteData.variants 에 4종(빨강/초록/파랑/노랑)으로 등록되며, 스폰 시 weight 가중치로 1종이 선택된다.
   // (Unity 직렬화 데이터 클래스라 EliteData 와 동일하게 public camelCase 필드를 쓴다.)
   [System.Serializable]
   public class EliteVariant
   {
      public EliteType type = EliteType.Red;

      [Tooltip("이 변형이 뽑힐 가중치. 클수록 자주 등장한다. 0이면 등장하지 않음.")]
      public float weight = 1f;

      [Header("스탯 배율")]
      public float hpMultiplier = 3f;
      public float damageMultiplier = 2f;
      public float moveSpeedMultiplier = 1f;
      public float sizeScale = 1.5f;
      public float dropExpMultiplier = 2.5f;
      public float dropMoneyMultiplier = 2.5f;

      [Header("CC 저항")]
      [Tooltip("0~1. 슬로우/스턴/넉백 효과를 (1-저항)배로 줄인다. 1이면 완전 면역.")]
      [Range(0f, 1f)]
      public float ccResistance = 0f;

      [Header("불 이펙트 색")]
      [Tooltip("엘리트 불 이펙트(eliteEffect)의 SpriteRenderer 색. 타입을 한눈에 구분한다.")]
      public Color tintColor = Color.white;

      [Header("파랑 전용 — 주기적 원거리 공격")]
      [Tooltip("원거리 투사체 발사 간격(초). Blue 타입에서만 사용.")]
      public float rangedAttackInterval = 3f;
      [Tooltip("발사할 투사체. 비우면 몬스터 데이터의 원거리 투사체를 사용한다.")]
      public Projectile rangedProjectile;

      [Header("노랑 전용 — 주변 이속 오라")]
      [Tooltip("오라 반경. Yellow 타입에서만 사용.")]
      public float auraRadius = 4f;
      [Tooltip("주변 몬스터에 적용할 이동속도 배율(1.4 = +40%).")]
      public float auraSpeedMultiplier = 1.4f;
      [Tooltip("오라 갱신 간격(초).")]
      public float auraInterval = 1f;
   }
}
