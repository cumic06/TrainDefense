using TrainDefense.Game.Datas;
using UnityEngine;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 지원 폭격 — 발동 즉시 화면 안 무작위 3~5곳에 포격을 떨어뜨린다. 포탄은 몬스터만 때리고 기차는 맞지 않는다(PlayerBombardManager와 동일).
    /// 즉발형이라 권장 데이터 duration은 0 이하(역까지)면 충분하다. Tick/End에서 할 일이 없어 길이는 의미가 없고,
    /// 짧게 잡으면 배너만 일찍 사라진다. 피해량은 플레이어 포격 기준에 magnitude를 곱한다(0 이하면 1배).
    /// </summary>
    public class SupportBombardTrackEventEffect : ITrackEventEffect
    {
        private const int MIN_SHELL_COUNT = 3;
        private const int MAX_SHELL_COUNT = 5;
        // 화면 가장자리에 걸쳐 잘리지 않도록 뷰포트 안쪽으로 들이는 여백 비율.
        private const float VIEWPORT_MARGIN = 0.1f;

        public void Begin(TrackEventData data)
        {
            PlayerBombardManager bombard = PlayerBombardManager.Instance;
            Camera mainCamera = Camera.main;
            if (bombard == null || mainCamera == null)
                return;

            float damageMultiplier = data != null && data.Magnitude > 0f ? data.Magnitude : 1f;
            int shellCount = Random.Range(MIN_SHELL_COUNT, MAX_SHELL_COUNT + 1);
            float depth = Mathf.Abs(mainCamera.transform.position.z);

            for (int i = 0; i < shellCount; i++)
            {
                Vector3 viewport = new Vector3(
                    Random.Range(VIEWPORT_MARGIN, 1f - VIEWPORT_MARGIN),
                    Random.Range(VIEWPORT_MARGIN, 1f - VIEWPORT_MARGIN),
                    depth);
                Vector3 world = mainCamera.ViewportToWorldPoint(viewport);

                bombard.FireAt(new Vector2(world.x, world.y), damageMultiplier);
            }
        }

        public void Tick(float deltaTime)
        {
        }

        public void End(bool isInterrupted)
        {
        }
    }
}
