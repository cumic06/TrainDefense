using Cumic.Events;
using TrainDefense.Game;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense
{
    /// <summary>
    /// 영구 업그레이드(강화) 버튼의 레드닷. 현재 엘리트 재화로 구매 가능한 업그레이드가
    /// 하나라도 있으면 켜진다. 재화 변동/구매 이벤트에 맞춰 갱신된다.
    /// 버튼 GameObject에 붙이고 redDot에 뱃지 오브젝트를 연결한다.
    /// </summary>
    public class PermanentUpgradeRedDot : MonoBehaviour
    {
        [Tooltip("레드닷으로 켜고 끌 뱃지 GameObject.")]
        [SerializeField]
        private GameObject redDot;

        private void OnEnable()
        {
            GameEventSystem.Subscribe<EliteCoinChangedEvent>(_OnEliteCoinChanged);
            GameEventSystem.Subscribe<PermanentUpgradePurchasedEvent>(_OnUpgradePurchased);
            _Refresh();
        }

        private void OnDisable()
        {
            GameEventSystem.Unsubscribe<EliteCoinChangedEvent>(_OnEliteCoinChanged);
            GameEventSystem.Unsubscribe<PermanentUpgradePurchasedEvent>(_OnUpgradePurchased);
        }

        private void Start()
        {
            // 씬 로드 직후에는 매니저 Awake 순서에 따라 OnEnable 시점에 Instance가 없을 수 있어 한 번 더 갱신한다.
            _Refresh();
        }

        private void _OnEliteCoinChanged(EliteCoinChangedEvent _) => _Refresh();
        private void _OnUpgradePurchased(PermanentUpgradePurchasedEvent _) => _Refresh();

        private void _Refresh()
        {
            if (redDot == null)
                return;

            var manager = PermanentUpgradeManager.Instance;
            redDot.SetActive(manager != null && manager.HasAffordableUpgrade());
        }
    }
}
