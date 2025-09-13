using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    public class TriChoiceSelectUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private Image iconImage;
        [SerializeField]
        private TextMeshProUGUI descriptionText;
        #endregion

        private Button _selectButton;

        private void Awake()
        {
            _selectButton = GetComponent<Button>();
        }

        private void Start()
        {
            _selectButton.onClick.AddListener(OnSelectButtonClick);
        }

        public void SetData()
        {

        }

        private void OnSelectButtonClick()
        {

        }
    }
}