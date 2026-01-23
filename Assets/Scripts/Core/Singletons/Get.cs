using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Get : MonoBehaviour
{
    private static Get _singleton;

    private readonly Dictionary<Type, ISingletonInstance> _singletons = new Dictionary<Type, ISingletonInstance>();

    public static bool ShuttingDown = false;

    void Awake()
    {
        ShuttingDown = false;
        if (_singleton == null)
        {
            _singleton = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _singletons.Clear();

        foreach (Transform child in transform)
        {
            var singletonInstances = child.GetComponentsInChildren<ISingletonInstance>();
            if(singletonInstances.Length == 0)
            {
                Debug.LogError($"You forgt to inherit ISingletonInstance on {child.name}. Either add it or move this object out from under Get");
                continue;
            }

            var obj = child.GetComponentsInChildren<ISingletonInstance>().First();
            _singletons.Add(obj.GetType(), obj);
        }
    }

    public static T Instance<T>() where T : MonoBehaviour, ISingletonInstance
    {
        return _singleton?._Instance<T>();
    }

    private T _Instance<T>() where T : MonoBehaviour, ISingletonInstance
    {
        if (_singletons.TryGetValue(typeof(T), out var component))
            return component as T;

        return null;
    }

    //[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    //static void Init()
    //{
    //    foreach( var singleton in Singleton._singletons)
    //    {
    //        singleton.Value.Start();
    //    }
    //}

    public static void Set(Type type, ISingletonInstance monoBehaviour)
    {
        _singleton._singletons[type] = monoBehaviour;
    }

    private void OnDestroy()
    {
        ShuttingDown = true;
        _singleton = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        _singleton = null;
    }

    public void OnApplicationQuit()
    {
        ShuttingDown = true;
    }
}

public interface ISingletonInstance
{
}