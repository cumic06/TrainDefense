using UnityEngine;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 일시정지(timeScale 0) 중에도 움직여야 하는 UI 셰이더 효과(카드 외곽선 광택)가 쓰는 전역 시간 _GlobalUnscaledTime을 갱신한다.
    /// 화면(레벨업 창·상점 카드)마다 따로 갱신하던 것을 한 곳으로 모았다. UI를 그리기 직전에 프레임당 한 번 쓴다.
    /// </summary>
    public static class ShaderUnscaledTimeUpdater
    {
        private static readonly int GlobalUnscaledTime = Shader.PropertyToID("_GlobalUnscaledTime");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void _Register()
        {
            // 도메인 리로드를 끈 에디터에서 중복 등록되지 않게 먼저 뺀다.
            Canvas.willRenderCanvases -= _UpdateTime;
            Canvas.willRenderCanvases += _UpdateTime;
        }

        private static void _UpdateTime() => Shader.SetGlobalFloat(GlobalUnscaledTime, Time.unscaledTime);
    }
}
