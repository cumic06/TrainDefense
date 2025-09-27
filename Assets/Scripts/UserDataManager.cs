using System.Collections.Generic;
using TrainDefense.Game.Events;
using UnityEngine;
using Cumic.Events;

public class UserDataManager : MonoBehaviour
{
    private Dictionary<string, int> _triChoiceData = new();

    private void Start()
    {
        DontDestroyOnLoad(gameObject);

        GameEventSystem.Subscribe<TriChoiceSelectEvent>(AddTriChoiceData);
    }

    public void AddTriChoiceData(TriChoiceSelectEvent triChoiceSelectEvent)
    {
        var triChoiceData = triChoiceSelectEvent.Data;

        if (triChoiceData == null) return;

        if (_triChoiceData.ContainsKey(triChoiceData.Id))
        {
            _triChoiceData[triChoiceData.Id]++;
            Debug.Log($"{triChoiceData.Id} : {_triChoiceData[triChoiceData.Id]}");
        }
        else
        {
            _triChoiceData.Add(triChoiceData.Id, 1);
            Debug.Log($"{triChoiceData.Id} : {_triChoiceData[triChoiceData.Id]}");
        }
    }
}