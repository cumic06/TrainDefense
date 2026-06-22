using Sirenix.OdinInspector;
using UnityEngine;

namespace TrainDefense.Game.Manager
{
    /// <summary>
    /// 저장된 색약 옵션을 읽어 ColorblindFeature(풀스크린 후처리)에 적용한다.
    /// 로비/게임 전역에 적용되도록 DontDestroyOnLoad 싱글톤으로 유지한다.
    /// (GameOverEffectController와 동일한 머티리얼 제어 패턴)
    /// </summary>
    public class ColorblindController : MonoBehaviour
    {
        public static ColorblindController Instance { get; private set; }

        [Required]
        [SerializeField]
        private Material material;

        private static readonly int _typeId     = Shader.PropertyToID("_ColorblindType");
        private static readonly int _strengthId = Shader.PropertyToID("_Strength");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            ColorblindFeature.Material = material;
        }

        private void Start()
        {
            int type = UserDataManager.Instance != null ? UserDataManager.Instance.ColorblindType : 0;
            Apply(type);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                ColorblindFeature.IsActive = false;
        }

        /// <summary>
        /// 색약 유형(0=없음, 1=적색맹, 2=녹색맹, 3=청색맹)을 즉시 적용한다.
        /// </summary>
        public void Apply(int type)
        {
            if (material == null)
                return;

            if (type <= 0)
            {
                ColorblindFeature.IsActive = false;
                return;
            }

            material.SetFloat(_typeId, type);
            material.SetFloat(_strengthId, 1f);
            ColorblindFeature.IsActive = true;
        }
    }
}
