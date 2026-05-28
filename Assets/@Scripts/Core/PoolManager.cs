using System.Collections.Generic;
using UnityEngine;

public class PoolManager : SingletonBehaviour<PoolManager>
{
    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();
    private readonly Dictionary<GameObject, GameObject> _instanceToPrefab = new();
    private readonly Dictionary<GameObject, Transform> _poolRoots = new();

    public void Init()
    {
        Debug.Log("[PoolManager] Init");
    }

    public T Get<T>(GameObject prefab, Transform parent = null) where T : Component
    {
        return Get(prefab, parent).GetComponent<T>();
    }

    public GameObject Get(GameObject prefab, Transform parent = null)
    {
        if (!_pools.ContainsKey(prefab))
        {
            _pools[prefab] = new Queue<GameObject>();
            GameObject root = new($"Pool_{prefab.name}");
            _poolRoots[prefab] = root.transform;
        }

        GameObject obj;
        if (_pools[prefab].Count > 0)
        {
            obj = _pools[prefab].Dequeue();
            obj.transform.SetParent(parent ?? _poolRoots[prefab], false);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab, parent ?? _poolRoots[prefab]);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            _instanceToPrefab[obj] = prefab;
        }

        return obj;
    }

    public void Return(GameObject instance)
    {
        if (!_instanceToPrefab.TryGetValue(instance, out GameObject prefab))
        {
            Destroy(instance);
            return;
        }

        instance.SetActive(false);
        instance.transform.SetParent(_poolRoots[prefab]);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        _pools[prefab].Enqueue(instance);
    }

    protected override void Dispose()
    {
        _pools.Clear();
        _instanceToPrefab.Clear();
        _poolRoots.Clear();
        base.Dispose();
    }
}