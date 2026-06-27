using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI.Achievement
{
    /// <summary>
    /// 로비 메뉴의 업적 버튼. 클릭하면 업적 팝업을 현재 Canvas에 띄운다.
    /// 도감 버튼과 동일하게 UIManager 유무에 의존하지 않고 Canvas에 직접 인스턴스화한다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class AchievementButton : MonoBehaviour
    {
        private const string AchievementPopupResourceName = "Popup_Achievement";

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
            GameObject popupPrefab = Resources.Load<GameObject>(AchievementPopupResourceName);

            if (popupPrefab == null)
            {
                Debug.LogError($"[Achievement] Resources에서 {AchievementPopupResourceName} 프리팹을 찾을 수 없습니다.");

                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();

            if (canvas == null)
                canvas = Object.FindFirstObjectByType<Canvas>();

            if (canvas == null)
            {
                Debug.LogError("[Achievement] Canvas를 찾을 수 없습니다.");

                return;
            }

            Instantiate(popupPrefab, canvas.transform);
        }
    }
}
