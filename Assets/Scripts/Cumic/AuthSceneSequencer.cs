using UnityEngine;

public class AuthSceneSequencer : ISceneSequencer
{
    private IAuthable _authChecker;
    private IVersionable _versionChecker;

    private bool _isVersionMatched = false;

    private GameSceneSequence _gameSceneSequence;

    public AuthSceneSequencer(GameSceneSequence gameSceneSequence)
    {
        _gameSceneSequence = gameSceneSequence;
        
        if (_versionChecker == null)
        {
            _versionChecker = new VersionChecker();
            VersionCheck();
        }

        if (!_isVersionMatched) return;

        if (_authChecker == null)
        {
            _authChecker = new AuthChecker();
            AuthCheck();
        }
    }

    private void VersionCheck()
    {
        if (!_versionChecker.CheckVersion())
        {
            _isVersionMatched = false;

            _gameSceneSequence.PopupUI("Popup_VersionCheckFailed");

        }
        else
        {
            Debug.Log("Version Matched");
            _isVersionMatched = true;
        }
    }

    private void AuthCheck()
    {
        bool isLogined = _authChecker.TryLogin();
        //로그인 팝업UI 표시

        if (isLogined)
        {
            Debug.Log("Login Success");
            SceneController.NextScene();
        }
        else
        {
            //로그인 실패 팝업UI 표시
            _gameSceneSequence.PopupUI("Popup_LoginFailed");
        }
    }
}