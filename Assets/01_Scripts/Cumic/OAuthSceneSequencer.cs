using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Cumic.Checker;

namespace Cumic.Sequence
{
    public class OAuthSceneSequencer : ISceneSequencer
    {
        private readonly IAuthable _authChecker;
        private readonly IVersionable _versionChecker;
        private readonly IAgreeable _agreementChecker;

        private bool _isVersionMatched = false;
        private bool _isLoggedIn = false;

        private readonly GameSceneSequence _gameSceneSequence;

        public event Action<bool> OnCompleted;

        public OAuthSceneSequencer(GameSceneSequence gameSceneSequence, IVersionable versionChecker, IAgreeable agreementChecker, IAuthable authChecker)
        {
            _gameSceneSequence = gameSceneSequence;
            _versionChecker = versionChecker;
            _agreementChecker = agreementChecker;
            _authChecker = authChecker;
        }

        public async UniTask Run()
        {
            await TrainDefense.Localize.Localization.InitializeAsync();
            _isVersionMatched = await VersionCheck();

            if (!_isVersionMatched) return;

            // 개인정보처리방침 필수 동의 전에는 다음 단계(로그인 → 로비)로 넘어가지 않는다.
            await _agreementChecker.WaitForAgreement();

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