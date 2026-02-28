using System;
using UnityEngine;

namespace TrainDefense.Game
{
    public class MonsterAnimator : MonoBehaviour
    {
        private Animator _animator;
        public event Action OnAttackHit;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public void Attack()
        {
            _animator.CrossFade("Attack", 0f);
        }

        // Animation Event에서 호출될 메서드
        public void OnAttackEvent()
        {
            OnAttackHit?.Invoke();
        }
    }
}
