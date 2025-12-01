using UnityEngine;

namespace TrainDefense.Game.UI
{
    public class StageResultUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private GameObject clearResultUI;
        [SerializeField]
        private GameObject failResultUI;
        #endregion

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