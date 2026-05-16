using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Cumic.Checker;

namespace Cumic.Sequence
{
    public class AuthSceneSequencer : ISceneSequencer
    {
        private readonly IAuthable _authChecker;
        private readonly IVersionable _versionChecker;

        private bool _isVersionMatched = false;
        private bool _isLoggedIn = false;

        private readonly GameSceneSequence _gameSceneSequence;

        public event Action<bool> OnCompleted;

        public AuthSceneSequencer(GameSceneSequence gameSceneSequence, IVersionable versionChecker, IAuthable authChecker)
        {
            _gameSceneSequence = gameSceneSequence;
            _versionChecker = versionChecker;
            _authChecker = authChecker;
        }

        public async UniTask Run()
        {
            await TrainDefense.Localize.Localization.InitializeAsync();
            _isVersionMatched = await VersionCheck();

            if (!_isVersionMatched) return;

            _isLoggedIn = await AuthCheck();

            OnCompleted?.Invoke(_isLoggedIn);
        }

        private async UniTask<bool> VersionCheck()
        {
            bool isChecked = await _versionChecker.CheckVersion();
            if (!isChecked)
            {
                _gameSceneSequence.PopupUI("Popup_VersionCheckFailed");
                return false;
            }
            else
            {
                Debug.Log("Version Matched");
                return true;
            }
        }

        private async UniTask<bool> AuthCheck()
        {
            bool isLogined = await _authChecker.TryLogin();
            //로그인 팝업UI 표시

            if (isLogined)
            {
                Debug.Log("Login Success");
                return true;
            }
            else
            {
                //로그인 실패 팝업UI 표시
                _gameSceneSequence.PopupUI("Popup_LoginFailed");
                return false;
            }
        }

        public void Dispose()
        {

        }
    }
}