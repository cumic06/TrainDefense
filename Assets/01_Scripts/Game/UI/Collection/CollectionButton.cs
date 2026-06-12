using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI.Collection
{
    /// <summary>
    /// 로비 하단 메뉴 바의 도감 버튼. 클릭하면 도감 팝업을 현재 Canvas에 띄운다.
    /// UIManager 인스턴스 유무에 의존하지 않도록 RatingPopupManager와 동일하게 Canvas에 직접 인스턴스화한다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CollectionButton : MonoBehaviour
    {
        private const string CollectionPopupResourceName = "Popup_Collection";

        [SerializeField] private Button button;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
                button.onClick.AddListener(_OnClick);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(_OnClick);
        }

        private void _OnClick()
        {
            GameObject popupPrefab = Resources.Load<GameObject>(CollectionPopupResourceName);

            if (popupPrefab == null)
            {
                Debug.LogError($"[Collection] Resources에서 {CollectionPopupResourceName} 프리팹을 찾을 수 없습니다.");

                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();

            if (canvas == null)
                canvas = Object.FindFirstObjectByType<Canvas>();

            if (canvas == null)
            {
                Debug.LogError("[Collection] Canvas를 찾을 수 없습니다.");

                return;
            }

            Instantiate(popupPrefab, canvas.transform);
        }
    }
}
