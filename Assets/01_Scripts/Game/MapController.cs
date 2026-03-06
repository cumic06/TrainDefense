using UnityEngine;

namespace TrainDefense
{
    public class MapController : MonoBehaviour
    {
        private float _halfWidth;
        
        private Camera _cam;

        private void Start()
        {
            _cam = Camera.main;
            
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                _halfWidth = sr.bounds.size.x * 0.5f;
            }       
        }

        private void Update()
        {
            // 오른쪽 끝이 화면 왼쪽(0)을 벗어났는지 확인
            if (_cam.WorldToViewportPoint(transform.position + Vector3.right * _halfWidth).x < 0)
            {
                // 화면 오른쪽 끝(1) 좌표를 기준으로 이동
                float rightScreenX = _cam.ViewportToWorldPoint(Vector3.right * 0.95f).x;
                transform.position = new Vector3(rightScreenX + _halfWidth, transform.position.y, transform.position.z);
            }
        }
    }
}
