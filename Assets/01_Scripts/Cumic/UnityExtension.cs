using System;
using System.Diagnostics;
using UnityEngine;

namespace Cumic
{
public static class UnityExtension
{
    public static void Tester(Action action)
    {
        if (action == null)
        {
            UnityEngine.Debug.Log("Action is null!");
            return;
        }

        Stopwatch stopwatch = new();

        UnityEngine.Debug.Log("Starting test...");
        stopwatch.Start();
        action.Invoke();
        stopwatch.Stop();

        UnityEngine.Debug.Log($"Action executed! Time: {stopwatch.ElapsedMilliseconds}ms ({stopwatch.ElapsedTicks} ticks)");
    }

    public static void Tester(Action action, int iterations = 1)
    {
        if (action == null)
        {
            UnityEngine.Debug.Log("Action is null!");
            return;
        }

        Stopwatch stopwatch = new();

        UnityEngine.Debug.Log($"Starting test with {iterations} iterations...");
        stopwatch.Start();

        for (int i = 0; i < iterations; i++)
        {
            action.Invoke();
        }

        stopwatch.Stop();

        UnityEngine.Debug.Log($"Action {action.Method.Name} executed {iterations} times! Total: {stopwatch.ElapsedMilliseconds}ms");
    }
}
}