using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Cumic;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game
{
    /// <summary>
    /// 삼중택일 선택지 관리 매니저
    /// </summary>
    public class TriChoiceManager : Singleton<TriChoiceManager>
    {
        /// <summary>
        /// 삼중택일 선택지를 반환합니다.
        /// </summary>
        /// <param name="count">요청하는 선택지 개수</param>
        /// <returns>선택된 선택지 목록</returns>
        public List<IChoiceOption> GetChoices(int count)
        {
            List<IChoiceOption> result = new();

            if (TrainManager.Instance == null)
            {
                Debug.LogError("TrainManager is null");
                return result;
            }

            for (int i = 0; i < count; i++)
            {
                if (TrainManager.Instance.IsMaxTrainCountReached())//무조건 UpgradeTrainChoice를 반환한다.
                {
                    result = GetUpgradeTrainChoices();
                }
                else //AddTrainChoice와 UpgradeTrainChoice 중 랜덤으로 반환한다.
                {
                    var addChoices = GetAddTrainChoices();
                    var upgradeChoices = GetUpgradeTrainChoices();
                    var maxUpgradeTrains = TrainManager.Instance.GetMaxUpgradeTrains();
                    var trains = TrainManager.Instance.GetTrains();

                    if (maxUpgradeTrains.Length > 0 && trains.Length < maxUpgradeTrains.Length)
                    {
                        Debug.Log("UpgradeTrain");
                        result.Add(GetRandomChoices(upgradeChoices));
                    }

                    if (trains.Length > 0 && trains.Length < maxUpgradeTrains.Length)
                    {
                        Debug.Log("AddTrain");
                        IChoiceOption randomAddChoice = GetRandomChoices(addChoices);
                        if (result.Contains(randomAddChoice))
                        {
                            continue;
                        }
                        
                        result.Add(randomAddChoice);
                    }
                    else if (trains.Length <= 0)
                    {
                        Debug.Log("AddFirstTrain");
                        IChoiceOption randomAddChoice = GetRandomChoices(addChoices);
                        if (result.Contains(randomAddChoice))
                        {
                            continue;
                        }

                        result.Add(randomAddChoice);
                    }
                }
            }

            Debug.Log($"ResultCount: {result.Count}");

            return result;
        }

        private IChoiceOption GetRandomChoices(List<IChoiceOption> choices)//랜덤으로 선택지를 반환한다.
        {
            //중복은 제외하고 다시 랜덤으로 선택지를 반환한다.
            return choices[Random.Range(0, choices.Count)];
        }

        private List<IChoiceOption> GetAddTrainChoices()//AddTrainChoice 목록을 반환한다.
        {
            var addDatas = DatabaseManager.Instance.GetAddTrainChoices();
            return addDatas.ToList();
        }

        private List<IChoiceOption> GetUpgradeTrainChoices()//UpgradeTrainChoice 목록을 반환한다.
        {
            var upgradeDatas = DatabaseManager.Instance.GetUpgradeTrainChoices();
            return upgradeDatas.ToList();
        }
    }
}