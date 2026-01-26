using TrainDefense.Game.Datas;

namespace TrainDefense.Game.Events
{
    // MapManager -> MapSelectUI : 후보 2개 전달
    public class RandomMapOptionsEvent
    {
        public MapData MapData1 { get; }
        public MapData MapData2 { get; }

        public RandomMapOptionsEvent(MapData mapData1, MapData mapData2)
        {
            MapData1 = mapData1;
            MapData2 = mapData2;
        }
    }
}
