using System.Collections.Generic;
using UnityEngine;
using TrainDefense.Game;

namespace TrainDefense
{
public class ResourceManager : MonoBehaviour
{
    private static ResourceManager _instance;
    public static ResourceManager Instance => _instance;

    private void Awake()
    {
        _instance = this;
    }

    private readonly Dictionary<string, Stack<GameObject>> _pools = new();
    private readonly HashSet<GameObject> _activeObjects = new();
    private readonly HashSet<GameObject> _persistentObjects = new();

    public void RegisterPersistent(GameObject obj) => _persistentObjects.Add(obj);
    public void UnregisterPersistent(GameObject obj) => _persistentObjects.Remove(obj);

    public void ReturnAll()
    {
        foreach (var obj in new List<GameObject>(_activeObjects))
        {
            if (obj == null) continue;
            if (_persistentObjects.Contains(obj)) continue;
            if (_IsChildOfPersistent(obj)) continue;
            // 몬스터도 이 풀로 스폰되지만, 상점 진입 연출에서는 몬스터를 화면에 유지하므로 제외한다(몬스터는 DestroyAllMonsters가 따로 정리).
            if (obj.TryGetComponent<Monster>(out _)) continue;
            Destroy(obj);
        }
    }

    private bool _IsChildOfPersistent(GameObject obj)
    {
        Transform t = obj.transform.parent;
        while (t != null)
        {
            if (_persistentObjects.Contains(t.gameObject)) return true;
            t = t.parent;
        }
        return false;
    }

    public GameObject Spawn(GameObject prefab, Vector3 position = default, Quaternion rotation = default,
        Transform parent = null)
    {
        if (prefab == null)
        {
            Debug.LogError("ResourceManager: Cannot spawn null prefab");
            return null;
        }

        var key = GetPoolKey(prefab);

        if (!_pools.ContainsKey(key))
        {
            _pools.Add(key, new Stack<GameObject>());
        }

        GameObject result;
        GameObject popObject = null;
        while (_pools[key].Count > 0)
        {
            var candidate = _pools[key].Pop();
            if (candidate != null)
            {
                popObject = candidate;
                break;
            }
        }

        if (popObject != null)
        {
            popObject.SetActive(true);
            popObject.transform.SetPositionAndRotation(position, rotation);

            if (parent != null)
                popObject.transform.SetParent(parent);
            else
                popObject.transform.SetParent(transform);

            result = popObject;
        }
        else
        {
            result = CreateNewObject(prefab, position, rotation, parent);
        }

        _activeObjects.Add(result);
        return result;
    }

    public T Spawn<T>(T prefab, Vector3 position = default, Quaternion rotation = default,
        Transform parent = null) where T : Component
    {
        GameObject spawnedObject = Spawn(prefab.gameObject, position, rotation, parent);
        return spawnedObject.GetComponent<T>();
    }

    public GameObject SpawnPath(string path, Vector3 position = default, Quaternion rotation = default,
        Transform parent = null)
    {
        var spawnObject = Resources.Load<GameObject>(path);

        if (spawnObject == null)
        {
            Debug.LogWarning("path can't be found");
            return null;
        }

        return Spawn(spawnObject, position, rotation, parent);
    }

    public void Destroy(GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("ResourceManager: Cannot destroy null object");
            return;
        }

        string key = GetPoolKey(obj);

        if (!_pools.ContainsKey(key))
        {
            _pools.Add(key, new Stack<GameObject>());
        }

        _activeObjects.Remove(obj);
        _pools[key].Push(obj);
        obj.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        obj.transform.SetParent(transform);
        obj.SetActive(false);
    }

    private string GetPoolKey(GameObject obj)
    {
        string key = obj.name;
        key = key.Replace("(Clone)", "");
        return key;
    }

    private GameObject CreateNewObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject spawnObject;
        if (parent != null)
        {
            spawnObject = Instantiate(prefab, position, rotation, parent);
        }
        else
        {
            spawnObject = Instantiate(prefab, position, rotation, transform);
        }
        return spawnObject;
    }
}
}