using System;

namespace TrainDefense.Game
{
    /// <summary>
    /// 전투 기록 종류. 엘리트 승격 조건 시트에서 이 이름 그대로 쓴다("Kills>=300;Stuns>=50").
    /// StatType과 이름이 겹치면 스탯으로 먼저 해석되므로 겹치지 않는 이름을 쓴다(StatType.AttackCount ≠ Attacks).
    /// </summary>
    public enum TrainCombatRecordType
    {
        Kills = 0,         // 적 처치 수
        Slows = 1,         // 적 둔화 횟수 (둔화가 안 걸려 있던 적에게 새로 건 횟수)
        Stuns = 2,         // 적 기절 횟수 (기절이 안 걸려 있던 적에게 새로 건 횟수)
        Attacks = 3,       // 공격(발사) 횟수
        CriticalHits = 4,  // 치명타 적중 횟수
        DamageDealt = 5,   // 준 피해 합계
    }

    /// <summary>
    /// 포탑 한 대가 이번 판에 쌓은 전투 기록. 엘리트 승격 조건(ElitePromotionCondition)이 읽는다.
    /// 이어하기 세이브(TrainFormationSection)에 통째로 직렬화되므로 필드는 JsonUtility가 읽을 수 있는 public 필드로 둔다.
    /// </summary>
    [Serializable]
    public class TrainCombatRecord
    {
        public int Kills;
        public int Slows;
        public int Stuns;
        public int Attacks;
        public int CriticalHits;
        public float DamageDealt;

        public float GetValue(TrainCombatRecordType recordType)
        {
            switch (recordType)
            {
                case TrainCombatRecordType.Kills: return Kills;
                case TrainCombatRecordType.Slows: return Slows;
                case TrainCombatRecordType.Stuns: return Stuns;
                case TrainCombatRecordType.Attacks: return Attacks;
                case TrainCombatRecordType.CriticalHits: return CriticalHits;
                case TrainCombatRecordType.DamageDealt: return DamageDealt;
                default: return 0f;
            }
        }

        public void CopyFrom(TrainCombatRecord source)
        {
            if (source == null)
                return;

            Kills = source.Kills;
            Slows = source.Slows;
            Stuns = source.Stuns;
            Attacks = source.Attacks;
            CriticalHits = source.CriticalHits;
            DamageDealt = source.DamageDealt;
        }
    }
}
