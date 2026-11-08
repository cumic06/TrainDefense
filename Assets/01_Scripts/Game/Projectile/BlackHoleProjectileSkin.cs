using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 블랙홀 포탑 총알의 그림을 고른다. 소유 포탑에 화이트홀 개조(BlackHoleModifier.IsRepulse)가 있으면 화이트홀 그림,
    /// 아니면 기본 블랙홀 그림. 풀 재사용 때마다 Projectile.Init이 Apply를 불러 매 발사 다시 고른다.
    /// 반복음은 날아가는 동안에는 내지 않고, 착탄해 장판(BlackHoleField)이 생성된 뒤부터 장판이 낸다.
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
        [SerializeField]
        [Tooltip("날아가는 동안의 총알 그림 크기(원래 크기 대비). 작은 점으로 날아가 착탄 지점에서 장판이 커지며 소환된다.")]
        private float flightScale = 0.35f;

        private Vector3 _rendererBaseScale = Vector3.one;
        #endregion

        #region LifeCycle
        private void Awake()
        {
            if (targetRenderer != null)
                _rendererBaseScale = targetRenderer.transform.localScale;
        }
        #endregion

        public void Apply(IProjectileTarget owner)
        {
            if (targetRenderer == null)
                return;

            targetRenderer.transform.localScale = _rendererBaseScale * flightScale;

            bool isWhiteHole = owner is TurretTrain turret && turret.BlackHole.IsRepulse;
            Sprite sprite = isWhiteHole ? whiteHoleSprite : blackHoleSprite;

            if (sprite != null)
                targetRenderer.sprite = sprite;
        }
    }
}
