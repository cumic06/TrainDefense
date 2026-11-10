using TrainDefense.Game.Datas;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 블랙홀 반복음. 착탄 장판이 생성된 뒤 남아 있는 동안 들리고, 마지막 하나가 사라질 때 함께 멈춘다.
    /// SoundManager는 같은 클립을 소스 하나로 다루므로, 블랙홀 여러 개가 겹쳐도 먼저 사라진 쪽이 소리를 끊지 않게 개수를 센다.
    /// 장판(BlackHoleField)이 Init에서 Acquire, 사라지기 STOP_FADE_SECONDS 전(또는 비활성) 때 Release를 한 번씩 짝지어 부른다.
    /// </summary>
    public static class BlackHoleSoundLoop
    {
        // 마지막 장판이 Release하면 이 시간 동안 줄여서 끈다. 장판은 사라지기 이만큼 전에 Release하므로 소리는 장판과 함께 끝나고,
        // 다음 장판까지(비행 시간만큼) 쉼이 생긴다. 들리는 페이드는 두지 않는다(1초 직선 페이드는 "사라지기 전에 소리가 먼저 사라진다").
        // 0.05초는 즉시 Stop할 때 나는 뚝(클릭)만 막는 길이다.
        public const float STOP_FADE_SECONDS = 0.05f;

        #region Fields
        private static int _holderCount;
        #endregion

        // 도메인 리로드 없이 플레이 모드에 들어가도 이전 판의 개수가 남지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void _ResetOnLoad()
        {
            _holderCount = 0;
        }

        public static void Acquire()
        {
            _holderCount++;

            if (SoundManager.Instance == null)
                return;

            // 상점·선택창의 효과음 억제나 씬 전환으로 소스가 꺼졌을 수 있어, 새로 잡을 때마다 재생 중인지 다시 확인한다.
            // 페이드아웃 중이면 재생 중이 아닌 것으로 잡혀 PlaySFX가 볼륨만 되살려 이어 간다.
            if (!SoundManager.Instance.IsSFXPlaying(SoundType.BlackHoleLoop))
                SoundManager.Instance.PlaySFX(SoundType.BlackHoleLoop, true);
        }

        public static void Release()
        {
            if (_holderCount <= 0)
                return;

            _holderCount--;

            if (_holderCount > 0 || SoundManager.Instance == null)
                return;

            SoundManager.Instance.StopSFX(SoundType.BlackHoleLoop, STOP_FADE_SECONDS);
        }
    }
}
