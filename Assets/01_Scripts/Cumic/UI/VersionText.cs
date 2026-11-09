using TMPro;
using UnityEngine;

namespace Cumic.UI
{
    /// <summary>
    /// 붙은 텍스트에 현재 앱 버전(Application.version)을 표시한다. 타이틀 화면 구석 버전 표기용.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class VersionText : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<TMP_Text>().text = $"v{Application.version}";
        }
    }
}
