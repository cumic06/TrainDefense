using System;
using UnityEngine;
using Sirenix.OdinInspector;

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
        #endregion

        #region IData
        public string Id => id;
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
