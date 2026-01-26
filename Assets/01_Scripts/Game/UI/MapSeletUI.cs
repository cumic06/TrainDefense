using UnityEngine;
using UnityEngine.UI;
using Cumic.Events;
using TrainDefense.Game.Events;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game.UI
{
    public class MapSelectUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private GameObject mapSelectPanel;
        
        [SerializeField]
        private Button map1Button;
        
        [SerializeField]
        private Button map2Button;
        #endregion

        private MapData _mapData1;
        private MapData _mapData2;
        private bool _isSelecting = false;

        private void Start()
        {
            GameEventSystem.Subscribe<RandomMapOptionsEvent>(OnMapSelect);
            
            if (map1Button != null)
            {
                map1Button.onClick.AddListener(() => OnMapSelected(_mapData1));
            }
            
            if (map2Button != null)
            {
                map2Button.onClick.AddListener(() => OnMapSelected(_mapData2));
            }

            if (mapSelectPanel != null)
            {
                mapSelectPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<RandomMapOptionsEvent>(OnMapSelect);
        }

        private void OnMapSelect(RandomMapOptionsEvent mapSelectEvent)
        {
            if (_isSelecting) return;

            _mapData1 = mapSelectEvent.MapData1;
            _mapData2 = mapSelectEvent.MapData2;

            if (_mapData1 == null || _mapData2 == null)
            {
                Debug.LogWarning("RandomMapOptionsEvent에 MapData가 null입니다.");
                return;
            }

            ShowMapSelection();
        }

        private void ShowMapSelection()
        {
            _isSelecting = true;

            if (mapSelectPanel != null)
            {
                mapSelectPanel.SetActive(true);
            }
        }

        private void OnMapSelected(MapData selectedMapData)
        {
            if (!_isSelecting || selectedMapData == null) return;

            _isSelecting = false;

            if (mapSelectPanel != null)
            {
                mapSelectPanel.SetActive(false);
            }

            // 선택 결과 전달 (UI -> MapManager)
            GameEventSystem.Publish(new MapSelectEvent(selectedMapData));
        }
    }
}