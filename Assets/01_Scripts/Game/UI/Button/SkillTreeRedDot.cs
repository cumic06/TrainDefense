using Cumic.Events;
using TrainDefense.Game;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense
{
    /// <summary>
    /// 스킬트리 버튼의 레드닷. 현재 보유 재화로 습득 가능한 노드가 하나라도 있으면 켜진다.
    /// 재화 변동/습득/리스펙 이벤트에 맞춰 갱신된다. 버튼 GameObject에 붙이고 redDot에 뱃지를 연결한다.
    /// </summary>
    public class SkillTreeRedDot : MonoBehaviour
    {
        [Tooltip("레드닷으로 켜고 끌 뱃지 GameObject.")]
        [SerializeField]
        private GameObject redDot;

        private void OnEnable()
        {
            GameEventSystem.Subscribe<EliteCoinChangedEvent>(_OnCoinChanged);
            GameEventSystem.Subscribe<SkillNodeAcquiredEvent>(_OnNodeAcquired);
            GameEventSystem.Subscribe<SkillTreeResetEvent>(_OnTreeReset);
            _Refresh();
        }

        private void OnDisable()
        {
            GameEventSystem.Unsubscribe<EliteCoinChangedEvent>(_OnCoinChanged);
            GameEventSystem.Unsubscribe<SkillNodeAcquiredEvent>(_OnNodeAcquired);
            GameEventSystem.Unsubscribe<SkillTreeResetEvent>(_OnTreeReset);
        }

        private void Start()
        {
            // 씬 로드 직후에는 매니저 Awake 순서에 따라 OnEnable 시점에 Instance가 없을 수 있어 한 번 더 갱신한다.
            _Refresh();
        }

        private void _OnCoinChanged(EliteCoinChangedEvent _) => _Refresh();
        private void _OnNodeAcquired(SkillNodeAcquiredEvent _) => _Refresh();
        private void _OnTreeReset(SkillTreeResetEvent _) => _Refresh();

        private void _Refresh()
        {
            if (redDot == null)
                return;

            var manager = SkillTreeManager.Instance;
            redDot.SetActive(manager != null && manager.HasAcquirableNode());
        }
    }
}
