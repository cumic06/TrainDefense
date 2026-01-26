using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Events;
using TrainDefense.Game.Datas;

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
            GameEventSystem.Subscribe<MapSelectEvent>(OnMapSelected);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<InspectionEvent>(OnInspection);
            GameEventSystem.Unsubscribe<MapSelectEvent>(OnMapSelected);
        }

        private void OnInspection(InspectionEvent inspectionEvent)
        {
            _inspectionCount++;
            
            if (_inspectionCount % changeInterval == 0)
            {
                ShowMapSelection();
            }
        }

        private void ShowMapSelection()
        {
            var mapDatas = DatabaseManager.Instance.GetMapDatas(StageManager.Instance.CurrentStageData);
            
            if (mapDatas == null || mapDatas.Length < 2)
            {
                Debug.LogWarning("MapData가 2개 미만입니다. 맵 선택을 건너뜁니다.");
                return;
            }

            // 랜덤으로 2개 선택
            var shuffled = mapDatas.OrderBy(x => Random.value).ToArray();
            var mapData1 = shuffled[0];
            var mapData2 = shuffled[1];

            // 후보 2개 전달 (MapManager -> UI)
            GameEventSystem.Publish(new RandomMapOptionsEvent(mapData1, mapData2));
        }

        private void OnMapSelected(MapSelectEvent mapSelectedEvent)
        {
            if (mapSelectedEvent.SelectedMapData == null)
            {
                Debug.LogWarning("선택된 MapData가 null입니다.");
                return;
            }

            var prefab = mapSelectedEvent.SelectedMapData.Prefab;
            if (prefab == null)
            {
                Debug.LogWarning($"MapData [{mapSelectedEvent.SelectedMapData.Id}]: Prefab을 찾을 수 없습니다.");
                return;
            }

            SetMap(prefab);
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
