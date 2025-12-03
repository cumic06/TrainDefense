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
            var db = GetDB();
            if (db == null || db.TriChoiceDB == null)
            {
                Debug.LogError("DB or TriChoiceDB not found");
                return new List<IChoiceOption>();
            }

            List<IChoiceOption> result = new();

            if (TrainManager.Instance.IsMaxTrainCountReached())
            {

            }
            else
            {

            }

            return result;
        }

        private DB GetDB()
        {
            return DataBaseManager.Instance?.GetDB();
        }
    }
}