using Cumic.Events;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Cumic
{
    public class SequenceInvokor : MonoBehaviour
    {
        [Button("Engage Ready")]
        private void EngageReady()
        {
            GameEventSystem.Publish(new EngageReadyEvent());
        }

        [Button("Engage Start")]
        private void EngageStart()
        {
            GameEventSystem.Publish(new EngageStartEvent());
        }

        [Button("Stage End")]
        private void StageEnd()
        {
            GameEventSystem.Publish(new StageEndEvent(true));
        }

        [Button("Game End")]
        private void GameEnd()
        {
            GameEventSystem.Publish(new GameEndEvent());
        }
    }
}