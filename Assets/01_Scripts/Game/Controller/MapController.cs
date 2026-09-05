using UnityEngine;

namespace TrainDefense.Game.Controller
{
    /// <summary>
    /// 배경 타일 2장을 왼쪽으로 흘려보내며 무한 스크롤을 만든다.
    /// 타일 위치는 매 프레임 카메라 기준으로 랩핑해, 생성 위치가 카메라와 어긋나 있거나
    /// 카메라가 순간이동해도(맵 전환 연출 등) 화면이 검게 비지 않고 스스로 복구된다.
    /// </summary>
    public class MapController : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] _maps;

        [SerializeField]
        private float speed;

        private float width;

        private Camera _cam;

        private void Start()
        {
            _cam = Camera.main;

            var sr = _maps[0].GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                width = sr.bounds.size.x;
            }

            _WrapTilesAroundCamera();
        }

        // 배경은 물리와 무관한 순수 비주얼이라 렌더 프레임마다 옮긴다.
        // FixedUpdate(50Hz)로 옮기면 렌더 프레임(60Hz+)과 박자가 어긋나 이동량이 0/한 틱으로 들쭉날쭉해져
        // 속도가 빠를수록 배경이 드르륵 떨리고, 그 위에 가만히 있는 기차가 떨려 보인다.
        private void Update()
        {
            transform.Translate(Vector3.left * Time.deltaTime * speed);
        }

        private void LateUpdate()
        {
            if (_cam == null)
                _cam = Camera.main;

            _WrapTilesAroundCamera();
        }

        // 각 타일의 x를 카메라 기준 [-width, +width) 구간으로 랩핑한다.
        // 두 타일은 서로 width 간격을 유지한 채 2*width 주기로만 이동하므로 이음새가 항상 맞고,
        // 화면(가로 반폭 < width/2 전제)은 언제나 두 타일로 덮인다. 화면에 보이는 타일은
        // 랩핑 범위 밖으로 나갈 수 없어 눈에 띄는 순간이동도 발생하지 않는다.
        private void _WrapTilesAroundCamera()
        {
            if (_cam == null || width <= 0f)
                return;

            float camX = _cam.transform.position.x;
            float period = width * 2f;

            foreach (var map in _maps)
            {
                if (map == null) continue;

                Vector3 position = map.transform.position;
                float offsetX = Mathf.Repeat(position.x - camX + width, period) - width;
                map.transform.position = new Vector3(camX + offsetX, position.y, position.z);
            }
        }
    }
}
