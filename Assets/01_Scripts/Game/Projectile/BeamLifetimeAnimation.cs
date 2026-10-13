using System.Collections;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 빔 그림(Laser 5장)을 정해진 시간에 맞춰 재생한다. 또렷한 앞 장들을 되풀이해 빔이 살아 있는 듯 깜빡이다가,
    /// 끝나기 직전 흩어지는 장면을 그 시간 안에 끝나도록 빨리 재생한다. 흩어짐이 곧 소멸 연출이라 투사체는 페이드하지 않는다. (섬멸 회전 빔)
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class BeamLifetimeAnimation : MonoBehaviour
    {
        [Tooltip("흩어지는 첫 장이 시작되는 지점(정규화 시간). 그 앞 장들이 또렷한 빔.")]
        [SerializeField, Range(0f, 1f)] private float scatterNormalizedTime = 0.4f;
        [Tooltip("빔이 꺼지기 이만큼 전에 흩어지기 시작해 꺼지는 순간 끝난다(초). 원래 길이보다 짧으면 그만큼 빨리 재생.")]
        [SerializeField] private float scatterSeconds = 0.1f;

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        // 풀에 돌아갈 때 빨리 재생하던 속도가 남지 않게 원복한다.
        private void OnDisable()
        {
            _animator.speed = 1f;
        }

        public void Play(float seconds)
        {
            StopAllCoroutines();
            StartCoroutine(_PlayCoroutine(seconds));
        }

        private IEnumerator _PlayCoroutine(float seconds)
        {
            float clipSeconds = _animator.runtimeAnimatorController.animationClips[0].length;
            float scatterDuration = Mathf.Min(scatterSeconds, seconds);
            for (float elapsed = 0f; elapsed < seconds - scatterDuration; elapsed += Time.deltaTime)
            {
                AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
                // 애니메이터는 이 코루틴 뒤에 진행되므로, 이번 프레임에 흩어지는 장으로 넘어갈 참이면 미리 되감아 그 장이 한 프레임도 안 보이게 한다.
                if (state.normalizedTime + Time.deltaTime / clipSeconds >= scatterNormalizedTime)
                    _animator.Play(state.fullPathHash, 0, 0f);
                yield return null;
            }

            _animator.speed = clipSeconds * (1f - scatterNormalizedTime) / scatterDuration;
            _animator.Play(_animator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, scatterNormalizedTime);
        }
    }
}
