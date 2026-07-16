using System.Collections.Generic;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game.SkillTree
{
    /// <summary>
    /// 스킬트리 판정·계산 코어 (순수 C# — Unity 없이 테스트 가능).
    /// 노드 레벨 상태를 소유하고 습득 가능 판정·비용·환급 계산만 담당한다.
    /// 재화 차감·저장·이벤트 발행은 SkillTreeManager(어댑터)의 몫이다.
    /// </summary>
    public class SkillTreeCore
    {
        private readonly Dictionary<string, SkillNodeData> _nodes = new();
        private readonly Dictionary<string, int> _levels = new();

        public SkillTreeCore(IEnumerable<SkillNodeData> nodes)
        {
            if (nodes == null) return;

            foreach (var node in nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.Id)) continue;

                _nodes[node.Id] = node;
            }
        }

        public IReadOnlyDictionary<string, int> Levels => _levels;
        public IReadOnlyCollection<SkillNodeData> Nodes => _nodes.Values;

        public SkillNodeData GetNode(string nodeId)
            => !string.IsNullOrEmpty(nodeId) && _nodes.TryGetValue(nodeId, out var node) ? node : null;

        public int GetLevel(string nodeId)
            => !string.IsNullOrEmpty(nodeId) && _levels.TryGetValue(nodeId, out int level) ? level : 0;

        /// <summary>세이브 로드 시 레벨을 직접 주입한다. 정의에 없는 노드 id는 무시한다 (밸런스 패치로 삭제된 노드 대비).</summary>
        public void SetLevel(string nodeId, int level)
        {
            var node = GetNode(nodeId);
            if (node == null || level <= 0) return;

            _levels[nodeId] = node.MaxLevel > 0 ? System.Math.Min(level, node.MaxLevel) : level;
        }

        public bool IsMaxLevel(string nodeId)
        {
            var node = GetNode(nodeId);
            if (node == null || node.MaxLevel <= 0) return false;

            return GetLevel(nodeId) >= node.MaxLevel;
        }

        /// <summary>선행 노드가 전부 1레벨 이상인지. 선행 목록이 비어 있으면 통과, 정의에 없는 선행 id가 있으면 불통.</summary>
        public bool ArePrerequisitesMet(string nodeId)
        {
            var node = GetNode(nodeId);
            if (node == null) return false;
            if (node.Prerequisites == null || node.Prerequisites.Length == 0) return true;

            foreach (string prerequisiteId in node.Prerequisites)
            {
                if (string.IsNullOrEmpty(prerequisiteId)) continue;
                if (GetNode(prerequisiteId) == null) return false;
                if (GetLevel(prerequisiteId) <= 0) return false;
            }

            return true;
        }

        /// <summary>현재 레벨 기준 다음 습득 비용. 정의에 없는 노드는 int.MaxValue (자연히 습득 불가).</summary>
        public int GetNextCost(string nodeId)
        {
            var node = GetNode(nodeId);
            if (node == null) return int.MaxValue;

            return node.GetCostAtLevel(GetLevel(nodeId));
        }

        public bool CanAcquire(string nodeId, int availablePoints)
        {
            var node = GetNode(nodeId);
            if (node == null) return false;
            if (IsMaxLevel(nodeId)) return false;
            if (!ArePrerequisitesMet(nodeId)) return false;

            return GetNextCost(nodeId) <= availablePoints;
        }

        /// <summary>판정 통과 시 레벨 +1 하고 차감할 비용을 돌려준다. 포인트 차감·저장·이벤트는 호출자 몫.</summary>
        public bool TryLevelUp(string nodeId, int availablePoints, out int cost)
        {
            cost = 0;

            if (!CanAcquire(nodeId, availablePoints)) return false;

            cost = GetNextCost(nodeId);
            _levels[nodeId] = GetLevel(nodeId) + 1;

            return true;
        }

        /// <summary>지금까지 습득에 지출한 포인트 총합 (리스펙 전액 환급용).</summary>
        public int GetTotalSpentPoints()
        {
            int total = 0;
            foreach (var pair in _levels)
            {
                var node = GetNode(pair.Key);
                if (node == null) continue;

                for (int level = 0; level < pair.Value; level++)
                    total += node.GetCostAtLevel(level);
            }

            return total;
        }

        public void ResetAllLevels() => _levels.Clear();

        /// <summary>현재 포인트로 습득(레벨업) 가능한 노드가 하나라도 있는지. (레드닷 판정용)</summary>
        public bool HasAcquirableNode(int availablePoints)
        {
            foreach (string nodeId in _nodes.Keys)
            {
                if (CanAcquire(nodeId, availablePoints)) return true;
            }

            return false;
        }
    }
}
