using TMPro;
using UnityEngine;

namespace TrainDefense.Localize
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizeFontTarget : MonoBehaviour
    {
        #region Fields
        #endregion

        #region Variables
        private TextMeshProUGUI _tmp;
        #endregion

        #region LifeCycle
        private void OnEnable()
        {
            LocalizeFontSwitcher.Register(_tmp);
        }

        private void OnDisable()
        {
            LocalizeFontSwitcher.Unregister(_tmp);
        }

        private void Awake()
        {
            _tmp = GetComponent<TextMeshProUGUI>();
        }
        #endregion

        #region Sub/UnSub
        #endregion
    }
}
