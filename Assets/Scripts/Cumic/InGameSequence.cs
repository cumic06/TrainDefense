using UnityEngine;

public class InGameSequence : MonoBehaviour
{
    #region Fields
    [SerializeField]
    private GameObject _engageReadyUI;

    [SerializeField]
    private GameObject _engageStartUI;

    [SerializeField]
    private GameObject _stageEndUI;

    [SerializeField]
    private GameObject _gameEndUI;
    #endregion

    private void Start()
    {
        GameEventSystem.Subscribe<EngageReadyEvent>(EngageReady);
        GameEventSystem.Subscribe<EngageStartEvent>(EngageStart);
        GameEventSystem.Subscribe<StageEndEvent>(StageEnd);
        GameEventSystem.Subscribe<GameEndEvent>(GameEnd);

        string userNameEvent = GameEventSystem.Query<GetUserNameEvent, string>(new GetUserNameEvent());
        Debug.Log($"userNameEvent: {userNameEvent}");
    }

    private void OnDestroy()
    {
        GameEventSystem.Unsubscribe<EngageReadyEvent>(EngageReady);
        GameEventSystem.Unsubscribe<EngageStartEvent>(EngageStart);
        GameEventSystem.Unsubscribe<StageEndEvent>(StageEnd);
        GameEventSystem.Unsubscribe<GameEndEvent>(GameEnd);
    }

    private void EngageReady(EngageReadyEvent engageReadyEvent)
    {
        Debug.Log("Engage Ready");
        _engageReadyUI.SetActive(true);
        _engageStartUI.SetActive(false);
        _stageEndUI.SetActive(false);
        _gameEndUI.SetActive(false);
    }

    private void EngageStart(EngageStartEvent engageStartEvent)
    {
        Debug.Log("Engage Start");
        _engageStartUI.SetActive(true);
        _engageReadyUI.SetActive(false);
        _stageEndUI.SetActive(false);
        _gameEndUI.SetActive(false);
    }

    private void StageEnd(StageEndEvent stageEndEvent)
    {
        Debug.Log("Stage End");
        _stageEndUI.SetActive(true);
        _engageReadyUI.SetActive(false);
        _engageStartUI.SetActive(false);
        _gameEndUI.SetActive(false);
    }

    private void GameEnd(GameEndEvent gameEndEvent)
    {
        Debug.Log("Game End");
        _gameEndUI.SetActive(true);
        _engageReadyUI.SetActive(false);
        _engageStartUI.SetActive(false);
        _stageEndUI.SetActive(false);
    }
}