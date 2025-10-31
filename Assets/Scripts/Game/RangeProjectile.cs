using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 범위 공격 총알 (장판 같은 느낌)
    /// Stay하는 동안 지속적으로 데미지를 입힙니다.
    /// </summary>
    public class RangeProjectile : Projectile
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsMonster(other, out Monster monster))
            {
                // 처음 들어올 때는 즉시 데미지 적용 (Tick 데미지이므로 Dictionary에 추가)
                if (!_monsterDamageTimers.ContainsKey(monster))
                {
                    _monsterDamageTimers[monster] = Time.time;
                    monster.TakeDamage(_damage);
                }

                if (isShoveProjectile)
                {
                    if (monster == null) return;
                    monster.Shove(shovePower, shoveDuration);
                }
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (IsMonster(other, out Monster monster))
            {
                // Monster가 Dictionary에 있는지 확인하고, tickDamageInterval 시간이 지났으면 데미지 적용
                if (_monsterDamageTimers.ContainsKey(monster))
                {
                    float lastDamageTime = _monsterDamageTimers[monster];
                    if (Time.time - lastDamageTime >= tickDamageInterval)
                    {
                        if (monster == null) return;
                        monster.TakeDamage(_damage);
                        _monsterDamageTimers[monster] = Time.time;
                    }
                }

                if (isSlowProjectile)
                {
                    if (monster == null) return;
                    monster.Slow(slowValue);
                }

                if (isShoveProjectile)
                {
                    if (monster == null) return;
                    monster.Shove(shovePower, shoveDuration);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsMonster(other, out Monster monster))
            {
                // Dictionary에서 제거
                _monsterDamageTimers.Remove(monster);

                if (isSlowProjectile)
                {
                    if (monster == null || !monster.gameObject.activeInHierarchy) return;
                    monster.ResetMoveSpeed();
                }
            }
        }
    }
}

