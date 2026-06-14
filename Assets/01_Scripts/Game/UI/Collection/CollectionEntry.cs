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
        private readonly IReadOnlyList<Sprite> _frames;
        #endregion

        public string Id => _id;
        public Sprite Icon => _icon;
        public string Name => _name;
        public string Description => _description;
        public bool IsDiscovered => _isDiscovered;
        public IReadOnlyList<CollectionStatLine> StatLines => _statLines;

        /// <summary>기본 애니메이션 프레임. 그리드는 첫 프레임만, 상세는 전체를 순환 재생한다. 없으면 Icon을 쓴다.</summary>
        public IReadOnlyList<Sprite> Frames => _frames;
        public bool HasAnimation => _frames != null && _frames.Count > 0;

        private CollectionEntry(string id, Sprite icon, string name, string description, bool isDiscovered, List<CollectionStatLine> statLines, IReadOnlyList<Sprite> frames = null)
        {
            _id = id;
            _icon = icon;
            _name = name;
            _description = description;
            _isDiscovered = isDiscovered;
            _statLines = statLines;
            _frames = frames;
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
                _AppendTurretStats(statLines, turretData);
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
                new CollectionStatLine(_Loc("Detail_Speed", "공격 속도"), _Format(_ToAttackSpeed(status.AttackDelay))),
                new CollectionStatLine(_Loc("Detail_Range", "사거리"), _Format(status.AttackRange)),
                new CollectionStatLine(_Loc("Collection_AttackType", "공격 타입"), status.AttackType == MonsterAttackType.Ranged ? _Loc("Collection_Ranged", "원거리") : _Loc("Collection_Melee", "근접")),
            };

            return new CollectionEntry(data.Id, data.DisplaySprite, data.Name, string.Empty, isDiscovered, statLines, data.AnimationFrames);
        }

        private static void _AppendRangeStats(List<CollectionStatLine> statLines, RangeTrainStatus status)
        {
            statLines.Add(new CollectionStatLine(_Loc("Detail_Damage", "공격력"), _Format(status.AttackDamage)));
            statLines.Add(new CollectionStatLine(_Loc("Detail_Speed", "공격 속도"), _Format(_ToAttackSpeed(status.AttackInterval))));
            if (status.AttackCount > 1)
                statLines.Add(new CollectionStatLine(_Loc("Collection_AttackCount", "공격 횟수"), _Format(status.AttackCount)));

            if (status.AttackArea > 0f)
                statLines.Add(new CollectionStatLine(_Loc("Detail_Area", "공격 범위"), _Format(status.AttackArea)));

            if (status.SlowRate > 0f)
                statLines.Add(new CollectionStatLine(_Loc("Collection_Slow", "둔화"), _Format(status.SlowRate)));
        }

        private static void _AppendTurretStats(List<CollectionStatLine> statLines, TurretTrainData data)
        {
            TurretTrainStatus status = data.TurretTrainStatus;

            statLines.Add(new CollectionStatLine(_Loc("Detail_Damage", "공격력"), _Format(status.AttackDamage)));
            statLines.Add(new CollectionStatLine(_Loc("Detail_Speed", "공격 속도"), _Format(_ToAttackSpeed(status.AttackInterval))));
            statLines.Add(new CollectionStatLine(_Loc("Detail_Range", "사거리"), _Format(status.AttackRange)));
            // 공격 횟수가 대상 수와 같으면 중복이므로 대상 수만 표시
            if (status.AttackCount > 1 && status.AttackCount != status.TargetCount)
                statLines.Add(new CollectionStatLine(_Loc("Collection_AttackCount", "공격 횟수"), _Format(status.AttackCount)));

            // 범위(AttackArea)는 실제로 폭발 반경으로 쓰는 포탑만 표시
            if (status.AttackArea > 0f && data.UsesAttackArea)
                statLines.Add(new CollectionStatLine(_Loc("Detail_Area", "공격 범위"), _Format(status.AttackArea)));

            if (status.TargetCount > 1)
                statLines.Add(new CollectionStatLine(_Loc("Detail_Targets", "타겟 수"), _Format(status.TargetCount)));
        }

        private static string _Loc(string key, string fallback)
        {
            return LocalizeHelper.GetByKey(key, fallback);
        }

        private static string _Format(float value)
        {
            return value.ToString("0.##");
        }

        // 공격 간격(초)을 공격 속도(초당 횟수)로 변환. 0 이하면 0.
        private static float _ToAttackSpeed(float interval)
        {
            return interval > 0f ? 1f / interval : 0f;
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
