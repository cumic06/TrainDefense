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
        var choiceOption = triChoiceSelectEvent.ChoiceOption;

        if (choiceOption == null) return;

        if (_triChoiceData.ContainsKey(choiceOption.Id))
        {
            _triChoiceData[choiceOption.Id]++;
            Debug.Log($"{choiceOption.Id} : {_triChoiceData[choiceOption.Id]}");
        }
        else
        {
            _triChoiceData.Add(choiceOption.Id, 1);
            Debug.Log($"{choiceOption.Id} : {_triChoiceData[choiceOption.Id]}");
        }
    }
}