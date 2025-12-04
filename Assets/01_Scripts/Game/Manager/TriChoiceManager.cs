using System.Collections.Generic;
using UnityEngine;
using Cumic;
using TrainDefense.Game.Datas;
using System.Linq;

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

            if (TrainManager.Instance.IsMaxTrainCountReached())//무조건 UpgradeTrainChoice를 반환한다.
            {
                result = GetUpgradeTrainChoices();
            }
            else //AddTrainChoice와 UpgradeTrainChoice 중 랜덤으로 반환한다.
            {
                var addChoices = GetAddTrainChoices();
                var upgradeChoices = GetUpgradeTrainChoices();
                var randomChoices = new List<IChoiceOption>();
                randomChoices.AddRange(addChoices);
                randomChoices.AddRange(upgradeChoices);

                if (addChoices.Count > 0 && upgradeChoices.Count > 0)
                {
                    result = GetRandomChoices(randomChoices, count);
                }
                else if (addChoices.Count > 0)
                {
                    result = addChoices;
                }
            }
            return result;
        }

        private List<IChoiceOption> GetRandomChoices(List<IChoiceOption> choices, int count)//랜덤으로 선택지를 반환한다.
        {
            var result = new List<IChoiceOption>();

            for (int i = 0; i < count; i++)
            {
                result.Add(choices[Random.Range(0, choices.Count)]);
            }
            return result;
        }

        private List<IChoiceOption> GetUpgradeTrainChoices()//UpgradeTrainChoice 목록을 반환한다.
        {
            var upgradeDatas = DatabaseManager.Instance.GetUpgradeTrainChoices();
            return upgradeDatas.ToList();
        }

        private List<IChoiceOption> GetAddTrainChoices()//AddTrainChoice 목록을 반환한다.
        {
            var addDatas = DatabaseManager.Instance.GetAddTrainChoices();
            return addDatas.ToList();
        }
    }
}