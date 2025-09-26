using UnityEngine;

namespace TrainDefense.Game.UI
{
    public class StageResultUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject clearResultUI;
        [SerializeField]
        private GameObject failResultUI;

        public void ShowResult(bool isClear)
        {
            if (isClear)
            {
                if (clearResultUI != null)
                {
                    clearResultUI.SetActive(true);
                }
                if (failResultUI != null)
                {
                    failResultUI.SetActive(false);
                }
            }
            else
            {
                if (failResultUI != null)
                {
                    failResultUI.SetActive(true);
                }
                if (clearResultUI != null)
                {
                    clearResultUI.SetActive(false);
                }
            }
        }
    }
}