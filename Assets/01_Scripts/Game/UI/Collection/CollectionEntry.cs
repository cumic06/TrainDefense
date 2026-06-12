using System.Collections.Generic;
using UnityEngine;
using TrainDefense.Game.Datas;
using TrainDefense.Localize;

namespace TrainDefense.Game.UI.Collection
{
    /// <summary>
    /// 도감 한 칸이 표시할 데이터를 트레인/몬스터 공통 형태로 담는 뷰모델.
    /// 그리드 슬롯과 상세 패널이 이 타입만 알면 되도록 트레인/몬스터 데이터 차이를 흡수한다.
    /// </summary>
    public class CollectionEntry
    {
        #region Variables
        private readonly string _id;
        private readonly Sprite _icon;
        private readonly string _name;
        private readonly string _description;
        private readonly bool _isDiscovered;
        private readonly List<CollectionStatLine> _statLines;
        #endregion

        public string Id => _id;
        public Sprite Icon => _icon;
        public string Name => _name;
        public string Description => _description;
        public bool IsDiscovered => _isDiscovered;
        public IReadOnlyList<CollectionStatLine> StatLines => _statLines;

        private CollectionEntry(string id, Sprite icon, string name, string description, bool isDiscovered, List<CollectionStatLine> statLines)
        {
            _id = id;
            _icon = icon;
            _name = name;
            _description = description;
            _isDiscovered = isDiscovered;
            _statLines = statLines;
        }

        public static CollectionEntry FromTrain(TrainData data, bool isDiscovered)
        {
            var statLines = new List<CollectionStatLine>
            {
                new CollectionStatLine(_Loc("Detail_HP", "체력"), _Format(data.TrainStatusData.MaxHp)),
            };

            if (data is RangeTrainData rangeData)
            {
                _AppendRangeStats(statLines, rangeData.RangeTrainStatus);
            }
            else if (data is TurretTrainData turretData)
            {
                _AppendTurretStats(statLines, turretData.TurretTrainStatus);
            }

            return new CollectionEntry(data.Id, data.Icon, data.Name, data.Description, isDiscovered, statLines);
        }

        public static CollectionEntry FromMonster(MonsterData data, bool isDiscovered)
        {
            MonsterStatusInfo status = data.MonsterStatusData;
            var statLines = new List<CollectionStatLine>
            {
                new CollectionStatLine(_Loc("Detail_HP", "체력"), _Format(status.MaxHp)),
                new CollectionStatLine(_Loc("Detail_Damage", "공격력"), _Format(status.Damage)),
                new CollectionStatLine(_Loc("Collection_MoveSpeed", "이동 속도"), _Format(status.MoveSpeed)),
                new CollectionStatLine(_Loc("Collection_AttackDelay", "공격 주기"), _Format(status.AttackDelay)),
                new CollectionStatLine(_Loc("Detail_Range", "사거리"), _Format(status.AttackRange)),
                new CollectionStatLine(_Loc("Collection_AttackType", "공격 타입"), status.AttackType == MonsterAttackType.Ranged ? _Loc("Collection_Ranged", "원거리") : _Loc("Collection_Melee", "근접")),
            };

            return new CollectionEntry(data.Id, data.Icon, data.Name, data.Description, isDiscovered, statLines);
        }

        private static void _AppendRangeStats(List<CollectionStatLine> statLines, RangeTrainStatus status)
        {
            statLines.Add(new CollectionStatLine(_Loc("Detail_Damage", "공격력"), _Format(status.AttackDamage)));
            statLines.Add(new CollectionStatLine(_Loc("Detail_Speed", "공격 속도"), _Format(status.AttackInterval)));
            statLines.Add(new CollectionStatLine(_Loc("Detail_Range", "사거리"), _Format(status.AttackRange)));
            statLines.Add(new CollectionStatLine(_Loc("Collection_AttackCount", "공격 횟수"), _Format(status.AttackCount)));

            if (status.AttackArea > 0f)
                statLines.Add(new CollectionStatLine(_Loc("Detail_Area", "공격 범위"), _Format(status.AttackArea)));

            statLines.Add(new CollectionStatLine(_Loc("Detail_CritChance", "치명타 확률"), _Format(status.CriticalChance)));
            statLines.Add(new CollectionStatLine(_Loc("Detail_CritDamage", "치명타 데미지"), _Format(status.CriticalDamage)));

            if (status.SlowRate > 0f)
                statLines.Add(new CollectionStatLine(_Loc("Collection_Slow", "둔화"), _Format(status.SlowRate)));
        }

        private static void _AppendTurretStats(List<CollectionStatLine> statLines, TurretTrainStatus status)
        {
            statLines.Add(new CollectionStatLine(_Loc("Detail_Damage", "공격력"), _Format(status.AttackDamage)));
            statLines.Add(new CollectionStatLine(_Loc("Detail_Speed", "공격 속도"), _Format(status.AttackInterval)));
            statLines.Add(new CollectionStatLine(_Loc("Detail_Range", "사거리"), _Format(status.AttackRange)));
            statLines.Add(new CollectionStatLine(_Loc("Collection_AttackCount", "공격 횟수"), _Format(status.AttackCount)));

            if (status.AttackArea > 0f)
                statLines.Add(new CollectionStatLine(_Loc("Detail_Area", "공격 범위"), _Format(status.AttackArea)));

            if (status.TargetCount > 0)
                statLines.Add(new CollectionStatLine(_Loc("Detail_Targets", "타겟 수"), _Format(status.TargetCount)));

            statLines.Add(new CollectionStatLine(_Loc("Detail_CritChance", "치명타 확률"), _Format(status.CriticalChance)));
            statLines.Add(new CollectionStatLine(_Loc("Detail_CritDamage", "치명타 데미지"), _Format(status.CriticalDamage)));
        }

        private static string _Loc(string key, string fallback)
        {
            return LocalizeHelper.GetByKey(key, fallback);
        }

        private static string _Format(float value)
        {
            return value.ToString("0.##");
        }
    }

    public readonly struct CollectionStatLine
    {
        public readonly string Label;
        public readonly string Value;

        public CollectionStatLine(string label, string value)
        {
            Label = label;
            Value = value;
        }
    }
}
