using Cumic.Achievement;
using Cumic.Events;
using UnityEngine;

namespace TrainDefense.Game.UI.Achievement
{
    /// <summary>
    /// 업적 버튼의 레드닷. 아직 확인하지 않은 달성 업적이 있으면 켜지고,
    /// 업적 팝업을 열어 확인하면(<see cref="AchievementRedDotState.MarkAllSeen"/>) 꺼진다.
    /// 버튼 GameObject에 붙이고 redDot에 뱃지 오브젝트를 연결한다.
    /// </summary>
    public class AchievementRedDot : MonoBehaviour
    {
        [Tooltip("레드닷으로 켜고 끌 뱃지 GameObject.")]
        [SerializeField]
        private GameObject redDot;

        private void OnEnable()
        {
            AchievementRedDotState.OnChanged += _Refresh;
            GameEventSystem.Subscribe<AchievementUnlockedEvent>(_OnAchievementUnlocked);
            _Refresh();
        }

        private void OnDisable()
        {
            AchievementRedDotState.OnChanged -= _Refresh;
            GameEventSystem.Unsubscribe<AchievementUnlockedEvent>(_OnAchievementUnlocked);
        }

        private void Start()
        {
            // 씬 로드 직후 저장 데이터/매니저 초기화 순서와 무관하게 한 번 더 확정한다.
            _Refresh();
        }

        private void _OnAchievementUnlocked(AchievementUnlockedEvent _) => _Refresh();

        private void _Refresh()
        {
            if (redDot == null)
                return;

            redDot.SetActive(AchievementRedDotState.HasUnseenUnlocked());
        }
    }
}
