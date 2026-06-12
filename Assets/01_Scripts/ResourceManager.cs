using System.Collections.Generic;
using UnityEngine;

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
    private readonly Dictionary<GameObject, string> _instanceKeys = new();
    private readonly HashSet<GameObject> _pooledObjects = new();
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

        var key = GetPrefabKey(prefab);

        if (!_pools.ContainsKey(key))
        {
            _pools.Add(key, new Stack<GameObject>());
        }

        GameObject result;
        GameObject popObject = null;
        while (_pools[key].Count > 0)
        {
            var candidate = _pools[key].Pop();
            _pooledObjects.Remove(candidate);
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

        _instanceKeys[result] = key;
        _activeObjects.Add(result);
        return result;
    }

    public T Spawn<T>(T prefab, Vector3 position = default, Quaternion rotation = default,
        Transform parent = null) where T : Component
    {
        if (prefab == null)
        {
            Debug.LogError("ResourceManager: Cannot spawn null prefab");
            return null;
        }

        GameObject spawnedObject = Spawn(prefab.gameObject, position, rotation, parent);
        if (spawnedObject == null)
            return null;

        if (spawnedObject.TryGetComponent(out T component))
            return component;

        // 풀이 섞여 T가 없는 오브젝트가 나온 경우(과거 이름 기반 키 잔재 등) 방어:
        // 잘못 꺼낸 오브젝트는 풀로 되돌리고, 풀을 거치지 않고 새 인스턴스를 생성한다.
        Debug.LogError($"ResourceManager: pool key collision — '{spawnedObject.name}' has no {typeof(T).Name} (prefab: {prefab.name})");
        Destroy(spawnedObject);

        GameObject created = CreateNewObject(prefab.gameObject, position, rotation, parent);
        _instanceKeys[created] = GetPrefabKey(prefab.gameObject);
        _activeObjects.Add(created);
        return created.GetComponent<T>();
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

        // persistent 오브젝트(터렛 소유 풀 등)는 공용 풀로 회수하지 않고 비활성화만 한다
        if (_persistentObjects.Contains(obj))
        {
            obj.SetActive(false);
            return;
        }

        // 이미 풀에 있는 오브젝트의 중복 push 방지 (같은 오브젝트가 두 곳에 대여되는 사고 차단)
        if (_pooledObjects.Contains(obj))
        {
            return;
        }

        // 스폰 기록이 있으면 프리팹 단위 키 사용, 외부에서 생성된 오브젝트는 이름 기반으로 폴백
        string key = _instanceKeys.TryGetValue(obj, out string spawnedKey) ? spawnedKey : GetPoolKey(obj);

        if (!_pools.ContainsKey(key))
        {
            _pools.Add(key, new Stack<GameObject>());
        }

        _activeObjects.Remove(obj);
        _pools[key].Push(obj);
        _pooledObjects.Add(obj);
        obj.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        obj.transform.SetParent(transform);
        obj.SetActive(false);
    }

    // 이름 기반 키는 동명의 다른 프리팹과 풀이 섞이므로, 프리팹 인스턴스 ID로 풀을 구분한다
    private string GetPrefabKey(GameObject prefab)
    {
        return $"{prefab.name}#{prefab.GetInstanceID()}";
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