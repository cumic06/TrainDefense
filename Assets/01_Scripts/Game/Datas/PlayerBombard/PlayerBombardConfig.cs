using UnityEngine;
using Sirenix.OdinInspector;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 플레이어 개입 포격(화면 탭 지점 포격) 설정.
    /// 편성 포탑 데이터(50xxx)와 분리해 따로 둔다. 여기 값은 기본값이고, 레벨업 강화 배율은 PlayerBombardManager가 발사 시점에 곱한다.
    /// Resources/Data/PlayerBombardConfig 에셋이 있으면 그것을, 없으면 아래 기본값을 쓴다.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerBombardConfig", menuName = "Data/PlayerBombard/PlayerBombardConfig")]
    public class PlayerBombardConfig : ScriptableObject
    {
        [BoxGroup("Damage")]
        [SerializeField]
        [LabelText("발당 데미지")]
        private float damage = 50f;

        [BoxGroup("Damage")]
        [SerializeField]
        [LabelText("폭발 반경 (AttackArea)")]
        private float attackArea = 3f;

        [BoxGroup("Cooldown")]
        [SerializeField]
        [LabelText("발사 쿨다운(초)")]
        private float cooldown = 1.5f;

        [BoxGroup("Knockback")]
        [SerializeField]
        [LabelText("넉백 세기")]
        private float knockbackPower = 3f;

        [BoxGroup("Knockback")]
        [SerializeField]
        [LabelText("넉백 지속(초)")]
        private float knockbackDuration = 0.5f;

        [BoxGroup("Projectile")]
        [SerializeField]
        [LabelText("포탄 프리팹 id (Resources/Prefabs/Projectiles/TrainProjectile/)")]
        // 개입 포격은 지정한 지점에 조준선이 뜨고 터지는 형태. 박격포 포탑은 자기 자리에서 포탄을 쏘아 보내는
        // CannonShellProjectile을 쓰므로 둘은 움직임부터 다르다.
        private string projectilePrefabId = "CannonProjectile";

        [BoxGroup("Projectile")]
        [SerializeField]
        [LabelText("발사음")]
        private SoundType attackSoundType = SoundType.CannonTurretTrainAttack;

        #region Properties

        public float Damage => damage;
        public float AttackArea => attackArea;
        public float Cooldown => cooldown;
        public float KnockbackPower => knockbackPower;
        public float KnockbackDuration => knockbackDuration;
        public string ProjectilePrefabId => projectilePrefabId;
        public SoundType AttackSoundType => attackSoundType;

        #endregion
    }
}
