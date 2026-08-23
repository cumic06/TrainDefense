using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TrainDefense.Game;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class MapData : IPrefabData
    {
        #region Fields
        [SerializeField]
        private string id;
        [SerializeField]
        private string prefabId;
        private GameObject prefab;
        [SerializeField]
        [Tooltip("비어 있으면 씬 MonsterSpawner의 공통 규칙(카메라 화면 기준 또는 씬 기본 영역) 사용. 채우면 이 맵 전용 스폰 영역(기차 기준 오프셋, 스폰 모드와 무관하게 우선).")]
        private List<MonsterSpawner.SpawnAreaInfo> customSpawnAreas;
        [SerializeField]
        [Tooltip("이 맵에서 몬스터 스폰 시 생성할 이펙트. 비면 이펙트 없음.")]
        private GameObject spawnEffectPrefab;
        #endregion

        #region IData
        public string Id => id;
        public List<MonsterSpawner.SpawnAreaInfo> CustomSpawnAreas => customSpawnAreas;
        public GameObject SpawnEffectPrefab => spawnEffectPrefab;
        #endregion

        #region IPrefabData
        public string PrefabId => prefabId;
        [ShowInInspector, ReadOnly]
        public GameObject Prefab
        {
            get
            {
                if (prefab == null && !string.IsNullOrEmpty(prefabId))
                {
                    prefab = Resources.Load<GameObject>($"Prefabs/Maps/{prefabId}");
                    if (prefab == null)
                    {
                        Debug.LogWarning($"MapData [{id}]: Prefab not found at 'Prefabs/Maps/{prefabId}'");
                    }
                }
                return prefab;
            }
        }
        #endregion
    }
}
