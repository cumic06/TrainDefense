using TrainDefense.Game.Datas;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 블랙홀 반복음. 착탄 장판이 생성된 뒤 남아 있는 동안 계속 들리고, 마지막 하나가 사라지면 멈춘다.
    /// SoundManager는 같은 클립을 소스 하나로 다루므로, 블랙홀 여러 개가 겹쳐도 먼저 사라진 쪽이 소리를 끊지 않게 개수를 센다.
    /// 장판(BlackHoleField)이 Init에서 Acquire, 붕괴·비활성 때 Release를 한 번씩 짝지어 부른다.
    /// </summary>
    public static class BlackHoleSoundLoop
    {
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

            SoundManager.Instance.StopSFX(SoundType.BlackHoleLoop);
        }
    }
}
