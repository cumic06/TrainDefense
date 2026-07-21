using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense
{
    /// <summary>
    /// 스킬트리 팝업을 여는 버튼. PermanentUpgradeButton과 동일하게 Canvas에 직접 인스턴스화한다.
    /// (UIManager 인스턴스 유무에 의존하지 않으며, 버튼 GameObject에 붙이면 OnClick은 코드로 자동 연결된다.)
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SkillTreeButton : MonoBehaviour
    {
        private const string PopupResourceName = "Popup_SkillTree";

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
            GameObject popupPrefab = Resources.Load<GameObject>(PopupResourceName);

            if (popupPrefab == null)
            {
                Debug.LogError($"[SkillTree] Resources에서 {PopupResourceName} 프리팹을 찾을 수 없습니다.");

                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();

            if (canvas == null)
                canvas = Object.FindFirstObjectByType<Canvas>();

            if (canvas == null)
            {
                Debug.LogError("[SkillTree] Canvas를 찾을 수 없습니다.");

                return;
            }

            Instantiate(popupPrefab, canvas.transform);
        }
    }
}
