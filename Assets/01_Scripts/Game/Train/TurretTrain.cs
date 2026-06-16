using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// (전환 과도기) 포탑 직렬화 설정 보유 마커. 모든 로직은 Train + TurretAttackModule이 담당한다.
    /// 마이그레이션(Add AttackModules)이 이 config를 TurretAttackModule로 복사한다.
    /// 복사·검증 완료 후 이 클래스(컴포넌트)는 Train으로 교체해 제거 예정(Step 3b 후속).
    /// </summary>
    public class TurretTrain : Train
    {
        // 마이그레이션이 SerializedObject로 읽어 TurretAttackModule로 복사하는 소스 필드.
        [SerializeField] private Transform[] turretProjectileSpawnPoints;
        [SerializeField] private bool useParticleProjectile;
        [SerializeField] private bool isTargeting = false;
        [SerializeField] private GameObject turret;
        [SerializeField] private GameObject turretModel;
    }
}
