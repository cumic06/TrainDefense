using System.Collections.Generic;
using UnityEngine;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Events;

namespace TrainDefense.Game
{
    public class MapManager : Singleton<MapManager>
    {
        [SerializeField]
        private List<GameObject> mapPrefabs;
        
        [SerializeField]
        private int changeInterval = 1;

        private GameObject _currentMapInstance;
        private int _inspectionCount = 0;

        private void Start()
        {
            GameEventSystem.Subscribe<InspectionEvent>(OnInspection);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<InspectionEvent>(OnInspection);
        }

        private void OnInspection(InspectionEvent inspectionEvent)
        {
            _inspectionCount++;
            
            if (_inspectionCount % changeInterval == 0)
            {
                ChangeMap();
            }
        }

        private void ChangeMap()
        {
            if (mapPrefabs == null || mapPrefabs.Count == 0) return;

            if (_currentMapInstance != null)
            {
                Destroy(_currentMapInstance);
            }

            int randomIndex = Random.Range(0, mapPrefabs.Count);
            GameObject selectedMap = mapPrefabs[randomIndex];
            
            if (selectedMap != null)
            {
                // Instantiate at (0,0,0) or appropriate position
                _currentMapInstance = Instantiate(selectedMap, Vector3.zero, Quaternion.identity);
            }
        }

        public void SetMap(GameObject mapPrefab)
        {
            if (_currentMapInstance != null)
            {
                Destroy(_currentMapInstance);
            }
            
            _currentMapInstance = Instantiate(mapPrefab, Vector3.zero, Quaternion.identity);
        }
    }
}
