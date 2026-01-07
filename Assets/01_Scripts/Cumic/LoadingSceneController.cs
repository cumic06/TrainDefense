using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cumic
{
   public class LoadingSceneController : MonoBehaviour
   {
      #region Variables

      #region Fields
      [SerializeField]
      private GameObject loadingScreen;
      [SerializeField]
      private float fadeDuration = 0.8f;
      [SerializeField]
      private float fadeInScale = 2.5f;
      [SerializeField]
      private float fadeOutScale = 0f;
      #endregion

      private static int _targetSceneIndex = -1;
      #endregion

      public static void SetTargetSceneIndex(int sceneIndex)
      {
         _targetSceneIndex = sceneIndex;
      }

      private void Start()
      {
         if (_targetSceneIndex < 0)
         {
            Debug.LogError("LoadingSceneController: Target scene index is not set");
            return;
         }

         StartCoroutine(LoadTargetSceneAsync());
      }

      private IEnumerator LoadTargetSceneAsync()
      {
         // 최소한 한 프레임 대기하여 LoadingScene이 완전히 로드되도록 함
         yield return null;

         AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(_targetSceneIndex);
         asyncOperation.allowSceneActivation = false;

         loadingScreen.transform.DOScale(fadeInScale, fadeDuration).SetEase(Ease.OutBack).SetUpdate(true);

         yield return new WaitForSeconds(fadeDuration);

         // 로딩 진행률을 90%까지 기다림
         while (asyncOperation.progress < 0.9f)
         {
            yield return null;
         }

         loadingScreen.transform.DOScale(fadeOutScale, fadeDuration).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
         {
            loadingScreen.SetActive(false);
         });

         yield return new WaitForSeconds(fadeDuration);

         // 씬 활성화
         asyncOperation.allowSceneActivation = true;

         // 씬이 완전히 로드될 때까지 대기
         while (!asyncOperation.isDone)
         {
            yield return null;
         }

         // 목표 씬 인덱스 초기화
         _targetSceneIndex = -1;
      }
   }
}