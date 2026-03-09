using UnityEngine;

namespace TrainDefense
{
    public class MapController : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] _maps;

        private float width;
        private int _currentIndex = 0;

        private Camera _cam;

        private void Start()
        {
            _cam = Camera.main;

            var sr = _maps[0].GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                width = sr.bounds.size.x;
            }
        }

        private void Update()
        {
            if (_cam.WorldToViewportPoint(_maps[_currentIndex].transform.position + Vector3.right * width * 0.5f).x < 0)
            {
                _maps[_currentIndex].transform.position = _maps[1 - _currentIndex].transform.position + Vector3.right * width;
                Debug.Log("아라라라라" + _maps[_currentIndex].transform.position);
                Debug.Log("아라라라라" + _maps[1 - _currentIndex].transform.position + Vector3.right * width);
                _currentIndex = 1 - _currentIndex;
            }
        }
    }
}
