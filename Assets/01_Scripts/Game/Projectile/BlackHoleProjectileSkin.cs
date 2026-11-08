using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 블랙홀 포탑 총알의 그림을 고른다. 소유 포탑에 화이트홀 개조(BlackHoleModifier.IsRepulse)가 있으면 화이트홀 그림,
    /// 아니면 기본 블랙홀 그림. 풀 재사용 때마다 Projectile.Init이 Apply를 불러 매 발사 다시 고른다.
    /// </summary>
    public class BlackHoleProjectileSkin : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        [Tooltip("그림을 바꿀 총알 모델의 SpriteRenderer.")]
        private SpriteRenderer targetRenderer;
        [SerializeField]
        [Tooltip("개조 없는 기본 블랙홀 총알 그림.")]
        private Sprite blackHoleSprite;
        [SerializeField]
        [Tooltip("화이트홀 개조 시 총알 그림.")]
        private Sprite whiteHoleSprite;

        private bool _isHoldingSound;
        #endregion

        #region LifeCycle
        // 착탄·사거리 소멸로 풀에 돌아갈 때 반복음 몫을 내려놓는다. 착탄 장판이 그 전에 이어받아 소리가 끊기지 않는다.
        private void OnDisable()
        {
            if (!_isHoldingSound)
                return;

            _isHoldingSound = false;
            BlackHoleSoundLoop.Release();
        }
        #endregion

        public void Apply(IProjectileTarget owner)
        {
            // 발사(Init)마다 불린다. 날아가는 동안 블랙홀 반복음을 켠다.
            if (!_isHoldingSound)
            {
                _isHoldingSound = true;
                BlackHoleSoundLoop.Acquire();
            }

            if (targetRenderer == null)
                return;

            bool isWhiteHole = owner is TurretTrain turret && turret.BlackHole.IsRepulse;
            Sprite sprite = isWhiteHole ? whiteHoleSprite : blackHoleSprite;

            if (sprite != null)
                targetRenderer.sprite = sprite;
        }
    }
}
