using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense
{
    public class OptionUI : MonoBehaviour
    {
        #region Fields
        #endregion

        public void ShowOptionUI()
        {
            gameObject.SetActive(true);
        }

        public void HideOptionUI()
        {
            gameObject.SetActive(false);
        }
    }
}
