using TrainDefense.Game.Datas;

namespace TrainDefense.Game.Events
{
    // MapSelectUI -> MapManager : 선택 결과 전달
    public class MapSelectEvent
    {
        public MapData SelectedMapData { get; }

        public MapSelectEvent(MapData selectedMapData)
        {
            SelectedMapData = selectedMapData;
        }
    }
}
