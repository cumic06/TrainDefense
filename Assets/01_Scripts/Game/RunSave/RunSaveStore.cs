using UnityEngine;

namespace TrainDefense.Game.RunSave
{
    /// <summary>
    /// 런 세이브의 저장 매체 담당(PlayerPrefs 1키). 순수 정적 유틸이라 씬·수명과 무관하다.
    /// 매체를 파일/클라우드로 바꾸더라도 이 클래스만 갈아끼우면 된다.
    /// </summary>
    public static class RunSaveStore
    {
        private const string RUN_SAVE_KEY = "RunSaveData";

        public static bool HasSave()
        {
            return !string.IsNullOrEmpty(PlayerPrefs.GetString(RUN_SAVE_KEY, string.Empty));
        }

        public static RunSaveData Load()
        {
            string json = PlayerPrefs.GetString(RUN_SAVE_KEY, string.Empty);
            var data = RunSaveData.FromJson(json);

            // 파싱 불가·미래 버전 세이브는 남겨두면 매번 실패하므로 정리한다. (조용한 초기화가 아니라 로드 실패가 이미 로그됨)
            if (data == null && !string.IsNullOrEmpty(json))
                Delete();

            return data;
        }

        public static void Save(RunSaveData data)
        {
            if (data == null)
                return;

            PlayerPrefs.SetString(RUN_SAVE_KEY, data.ToJson());
            PlayerPrefs.Save();
        }

        public static void Delete()
        {
            PlayerPrefs.DeleteKey(RUN_SAVE_KEY);
            PlayerPrefs.Save();
        }
    }
}
