using UnityEngine;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using Cumic.Events;
using Cumic.Sequence;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.Tutorial;

namespace TrainDefense.Game
{
   // MainTrain 주무기(선택 포탑) 장착 + 화면 터치 조준 연속 발사 + 조준 마커.
   // 본체(MainTrain.cs)는 편성 관리·이동을 담당하고, 무기 발사 책임은 이 partial로 분리한다.
   public partial class MainTrain
   {
      #region Field
      private const string AimMarkerPrefabPath = "Prefabs/AimMarker";
      private const float DefaultFireCooldown = 0.2f;

      [SerializeField]
      [BoxGroup("Weapon")]
      [Tooltip("선택 포탑(주무기)을 올릴 마운트. MainTrain prefab의 'Turret' 자식 Transform을 연결. 마운트의 위치/회전/스케일이 곧 포탑 배치가 된다. 비우면 MainTrain 루트에 올린다.")]
      private Transform turretMount;

      // 장착된 주무기. 선택 포탑의 비주얼(모델·스폰포인트)만 입힌 경량 Turret으로, 터치 입력으로만 발사한다.
      private Turret _turret;
      private GameObject _aimMarker;
      private float _fireCooldown;
      #endregion

      // 무기 입력 갱신은 본체(MainTrain.cs)의 Update()에서 호출한다.
      // partial 클래스는 Update()를 중복 정의할 수 없으므로, 발사 로직은 _UpdateWeaponInput()으로 분리만 한다.

      /// <summary>
      /// 선택한 포탑을 MainTrain 주무기로 장착한다. (TrainManager가 게임 시작 시 호출)
      /// </summary>
      public void EquipWeapon(TurretTrainData turretData)
      {
         if (turretData == null || turretData.Prefab == null)
            return;

         if (_turret != null)
         {
            Destroy(_turret.gameObject);
            _turret = null;
         }

         // 'Turret' 마운트 아래에 경량 Turret을 만들고 선택 포탑의 비주얼(모델·스폰포인트)만 입힌다.
         // 마운트의 위치/회전/스케일(인스펙터 조정)이 곧 포탑 배치가 된다.
         Transform mount = turretMount != null ? turretMount : transform;
         var turretObject = new GameObject("Turret_Weapon");
         turretObject.transform.SetParent(mount, false);

         _turret = turretObject.AddComponent<Turret>();
         _turret.Setup(turretData, this);
      }

      private void _UpdateWeaponInput()
      {
         if (_turret == null || _isDead)
         {
            _HideAimMarker();

            return;
         }

         // 전투 진행(Engage) 중에만 발사. 상점·일시정지·삼중택일 중에는 조준/발사를 막는다.
         // 단, 메인 트레인 공격 튜토리얼 중에는 게임이 멈춰 있어도 발사를 허용한다(발사로 튜토리얼 완료 → 게임 재개).
         bool tutorialFiringAllowed = TutorialManager.Instance != null
            && TutorialManager.Instance.CurrentSequenceId == "mainTrainAttackTutorial";

         if (InGameSequence.Instance != null && !InGameSequence.Instance.IsRunning && !tutorialFiringAllowed)
         {
            _HideAimMarker();

            return;
         }

         // 누르지 않아도 공속 쿨다운은 계속 진행(0에서 멈춰 대기). 발사만 누를 때 한다.
         if (_fireCooldown > 0f)
            _fireCooldown -= Time.deltaTime;

         if (!_TryGetAimPosition(out Vector2 aimPosition))
         {
            _HideAimMarker();

            return;
         }

         _ShowAimMarker(aimPosition);

         if (_fireCooldown <= 0f)
         {
            _turret.Fire(aimPosition);
            GameEventSystem.Publish(new MainTrainFiredEvent());
            float interval = _turret.AttackInterval;
            _fireCooldown = interval > 0f ? interval : DefaultFireCooldown;
         }
      }

      /// <summary>
      /// 화면을 누르고 있고 그 지점이 UI 위가 아니면 true와 월드 좌표를 반환한다.
      /// </summary>
      private bool _TryGetAimPosition(out Vector2 worldPosition)
      {
         worldPosition = default;

         Camera cam = Camera.main;
         if (cam == null)
            return false;

         Vector3 screenPosition;
         if (Input.touchCount > 0)
         {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
               return false;

            if (_IsPointerOverUI(touch.fingerId))
               return false;

            screenPosition = touch.position;
         }
         else if (Input.GetMouseButton(0))
         {
            if (_IsPointerOverUI(-1))
               return false;

            screenPosition = Input.mousePosition;
         }
         else
         {
            return false;
         }

         Vector3 world = cam.ScreenToWorldPoint(screenPosition);
         worldPosition = new Vector2(world.x, world.y);

         return true;
      }

      private bool _IsPointerOverUI(int pointerId)
      {
         if (EventSystem.current == null)
            return false;

         return pointerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(pointerId)
            : EventSystem.current.IsPointerOverGameObject();
      }

      private void _ShowAimMarker(Vector2 worldPosition)
      {
         if (_aimMarker == null)
         {
            GameObject prefab = Resources.Load<GameObject>(AimMarkerPrefabPath);
            if (prefab == null)
               return;

            _aimMarker = Instantiate(prefab, transform);
         }

         _aimMarker.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);

         if (!_aimMarker.activeSelf)
            _aimMarker.SetActive(true);
      }

      private void _HideAimMarker()
      {
         if (_aimMarker != null && _aimMarker.activeSelf)
            _aimMarker.SetActive(false);
      }
   }
}
