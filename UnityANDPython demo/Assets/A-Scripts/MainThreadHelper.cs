using UnityEngine;
using System.Collections;
using System.Collections.Concurrent;
using System;

public class MainThreadHelper : MonoBehaviour
{
    private static MainThreadHelper _instance;
    private readonly ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();

    public static MainThreadHelper Instance()
    {
        if (_instance == null)
        {
            _instance = FindObjectOfType<MainThreadHelper>();
            if (_instance == null)
            {
                GameObject go = new GameObject("MainThreadDispatcher");
                _instance = go.AddComponent<MainThreadHelper>();
                DontDestroyOnLoad(go);
            }
        }
        return _instance;
    }

    void Update()
    {
        while (_actions.TryDequeue(out var action))
        {
            action?.Invoke();
        }
    }

    public void Enqueue(Action action)
    {
        _actions.Enqueue(action);
    }
}