using UnityEngine;

namespace TrainDefense
{
    // 스폰된 이펙트가 lifetime 후 스스로 풀에 반환(ResourceManager.Destroy)되게 한다.
    // 파티클이 아닌(자동 소멸이 없는) 이펙트 프리팹에 붙여서 사용.
    public class AutoReleaseEffect : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("생성 후 제거되기까지 시간(초).")]
        private float lifetime = 1f;

        private void OnEnable()
        {
            CancelInvoke();
            Invoke(nameof(_Release), lifetime);
        }

        private void OnDisable()
        {
            CancelInvoke();
        }

        private void _Release()
        {
            if (ResourceManager.Instance != null)
                ResourceManager.Instance.Destroy(gameObject);
            else
                Destroy(gameObject);
        }
    }
}
