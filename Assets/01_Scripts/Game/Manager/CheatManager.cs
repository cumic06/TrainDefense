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

            // 다음 레벨까지 남은 경험치를 넣어 한 번 레벨업시킨다(카드는 역 도착 때 뜬다)
            if (Input.GetKeyDown(KeyCode.F) && UserDataManager.Instance != null)
                GameEventSystem.Publish(new AddExpEvent(Mathf.CeilToInt(UserDataManager.Instance.GetNextLevelUpExp() - UserDataManager.Instance.CurrentExp)));

            if (Input.GetKeyDown(KeyCode.M))
                StageManager.Instance?.ForceMapSelection();
        }

        private void _AddMaxCoin()
        {
            int current = UserDataManager.Instance.Coin;
            GameEventSystem.Publish(new ChangeCoinUIEvent(current, 999999999));
        }
    }
#endif
}
