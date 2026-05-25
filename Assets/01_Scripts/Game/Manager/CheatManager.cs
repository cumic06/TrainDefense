using Cumic.Events;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense.Game.Manager
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public class CheatManager : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
                _AddMaxCoin();

            if (Input.GetKeyDown(KeyCode.Space))
                TimeManager.Instance?.SetFastForward(true);
            else if (Input.GetKeyUp(KeyCode.Space))
                TimeManager.Instance?.SetFastForward(false);

            if (Input.GetKeyDown(KeyCode.F))
                GameEventSystem.Publish(new LevelUpEvent(1));
        }

        private void _AddMaxCoin()
        {
            int current = UserDataManager.Instance.Coin;
            GameEventSystem.Publish(new ChangeCoinUIEvent(current, 999999999));
        }
    }
#endif
}
