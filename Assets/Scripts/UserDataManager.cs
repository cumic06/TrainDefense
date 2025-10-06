using System.Collections.Generic;
using TrainDefense.Game.Events;
using UnityEngine;
using Cumic.Events;
using Cumic;

public class UserDataManager : Singleton<UserDataManager>
{
    private Dictionary<string, int> _triChoiceData = new();
    private int _money;

    public int Money => _money;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);

        GameEventSystem.Subscribe<TriChoiceSelectEvent>(AddTriChoiceData);
        GameEventSystem.Subscribe<MonsterDeadEvent>(AddMoney);
        _money = 0;
    }

    private void AddMoney(MonsterDeadEvent monsterDeadEvent)
    {
        _money += monsterDeadEvent.Coin;
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