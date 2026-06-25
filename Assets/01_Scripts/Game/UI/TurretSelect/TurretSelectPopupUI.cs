using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cumic;
using TrainDefense.Game.Datas;
using TrainDefense.Localize;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 게임 시작 시 가장 먼저(기차 등장 연출 전) 뜨는 포탑 선택창. 하단 슬롯에서 주무기를 고르고 "출발"로 게임을 시작한다.
    /// 노출 항목은 Resources/Data/TurretSelectConfig(있으면)를 우선 사용하고, 없으면 DB의 기본 터렛 목록을 사용한다.
    /// 고른 포탑은 UserDataManager.SelectedTurretId에 저장되어 MainTrain 주무기로 장착된다.
    /// </summary>
    public class TurretSelectPopupUI : MonoBehaviour
    {
        private const string ResourceName = "Popup_TurretSelect";
        private const string ConfigResourcePath = "Data/TurretSelectConfig";

        // 로비 씬 빌드 인덱스 (ChangeSceneButton.LobbySceneIndex와 동일)
        private const int LobbySceneIndex = 1;

        // 선택창 한 항목의 정규화 표현(별도 config든 기본 터렛이든 동일하게 다룬다).
        private class SelectEntry
        {
            public string TurretDataId;
            public Sprite Icon;
            public string Name;
            public TurretTrainData TurretData;
        }

        #region Fields
        [Header("선택 포탑 정보")]
        [SerializeField]
        [Tooltip("스탯 카드 헤더 TMP. 선택 시 포탑 이름으로 바뀐다.")]
        private TextMeshProUGUI cardHeaderText;
        [SerializeField]
        private Image selectedIconImage;
        [SerializeField]
        private TextMeshProUGUI attackText;
        [SerializeField]
        private TextMeshProUGUI attackSpeedText;
        [SerializeField]
        private TextMeshProUGUI rangeText;
        [SerializeField]
        private TextMeshProUGUI bestSurvivalText;

        [Header("정적 라벨 (로컬라이즈)")]
        [SerializeField]
        private TextMeshProUGUI attackLabel;
        [SerializeField]
        private TextMeshProUGUI attackSpeedLabel;
        [SerializeField]
        private TextMeshProUGUI rangeLabel;
        [SerializeField]
        private TextMeshProUGUI bestSurvivalTitle;
        [SerializeField]
        private TextMeshProUGUI bestSurvivalHint;
        [SerializeField]
        private TextMeshProUGUI departText;

        [Header("슬롯")]
        [SerializeField]
        private Transform slotContainer;
        [SerializeField]
        private TurretSelectSlotUI slotPrefab;

        [Header("출발/뒤로")]
        [SerializeField]
        private Button departButton;
        [SerializeField]
        [Tooltip("포탑 선택을 취소하고 로비 씬으로 돌아가는 버튼.")]
        private Button backButton;
        #endregion

        private readonly List<TurretSelectSlotUI> _slots = new();
        private readonly List<SelectEntry> _entries = new();
        private SelectEntry _selected;
        private Action _onDepart;

        /// <summary>
        /// 포탑 선택창을 띄운다. "출발"을 누르면 onDepart가 호출된다.
        /// 프리팹을 찾지 못하면 onDepart를 즉시 호출해 게임이 막히지 않게 한다.
        /// </summary>
        public static TurretSelectPopupUI Show(Action onDepart)
        {
            GameObject prefab = Resources.Load<GameObject>(ResourceName);
            if (prefab == null)
            {
                Debug.LogError($"TurretSelectPopupUI: '{ResourceName}' 프리팹을 Resources에서 찾지 못했습니다.");
                onDepart?.Invoke();

                return null;
            }

            Canvas canvas = FindObjectOfType<Canvas>();
            GameObject instance = Instantiate(prefab, canvas != null ? canvas.transform : null);
            instance.transform.SetAsLastSibling();

            TurretSelectPopupUI popup = instance.GetComponent<TurretSelectPopupUI>();
            popup._onDepart = onDepart;

            return popup;
        }

        private void Start()
        {
            _ApplyStaticTexts();
            _BuildEntries();
            _BuildSlots();

            if (departButton != null)
                departButton.onClick.AddListener(_OnDepart);

            if (backButton != null)
                backButton.onClick.AddListener(_OnBack);

            if (_entries.Count > 0)
                _Select(_entries[0]);
        }

        // config(별도 데이터)가 있으면 그걸, 없으면 DB의 기본 터렛 목록을 항목으로 만든다.
        private void _BuildEntries()
        {
            _entries.Clear();

            if (DatabaseManager.Instance == null)
                return;

            TurretSelectConfig config = Resources.Load<TurretSelectConfig>(ConfigResourcePath);
            if (config != null && config.Options != null && config.Options.Count > 0)
            {
                foreach (var option in config.Options)
                {
                    if (option == null || string.IsNullOrEmpty(option.TurretDataId))
                        continue;

                    var turretData = DatabaseManager.Instance.GetTrainData(option.TurretDataId) as TurretTrainData;
                    if (turretData == null)
                    {
                        Debug.LogWarning($"TurretSelectPopupUI: '{option.TurretDataId}'에 해당하는 TurretTrainData를 찾지 못했습니다.");
                        continue;
                    }

                    _entries.Add(new SelectEntry
                    {
                        TurretDataId = option.TurretDataId,
                        Icon = option.Icon != null ? option.Icon : turretData.Icon,
                        Name = !string.IsNullOrEmpty(option.DisplayName) ? option.DisplayName : turretData.Name,
                        TurretData = turretData
                    });
                }

                return;
            }

            // fallback: 별도 config가 없으면 DB 기본 터렛을 노출하되, 엘리트(31xxx 등 base+1000 변형)는 제외한다.
            // 엘리트는 게임 중 진화 보상이고 아이콘도 일반과 동일 guid라, 시작 선택창에 중복으로 뜨면 혼란스럽다.
            foreach (var turretData in DatabaseManager.Instance.GetTurretTrainDatas())
            {
                if (turretData == null || string.IsNullOrEmpty(turretData.Id))
                    continue;

                if (_IsEliteTurretId(turretData.Id))
                    continue;

                // MainTrain 주무기는 직선 비행 총알(Linear) 포탑만 사용한다(기관총·미사일·저격 등).
                // 빔(화염·레이저=NonMovement)·소환(전기·포격=TargetPos)형은 경량 Turret 발사 모델과 맞지 않아 시작 선택창에서 제외.
                if (!_IsLinearProjectileTurret(turretData))
                    continue;

                _entries.Add(new SelectEntry
                {
                    TurretDataId = turretData.Id,
                    Icon = turretData.Icon,
                    Name = turretData.Name,
                    TurretData = turretData
                });
            }
        }

        // 투사체가 Linear(직선 비행 총알)인 포탑만 MainTrain 주무기 후보로 허용한다.
        private static bool _IsLinearProjectileTurret(TurretTrainData turretData)
        {
            GameObject prefab = turretData.TurretProjectilePrefab;
            if (prefab == null || !prefab.TryGetComponent<Projectile>(out var projectile))
                return false;

            ProjectileData data = projectile.GetData();

            return data != null && data.MovementType == MovementType.Linear;
        }

        // 엘리트 터렛 판정: id의 천의 자리가 1인 변형(일반 30xxx → 엘리트 31xxx, 40xxx → 41xxx)을 시작 선택창에서 제외한다.
        private static bool _IsEliteTurretId(string id)
        {
            if (!int.TryParse(id, out int numericId))
                return false;

            return (numericId / 1000) % 10 == 1;
        }

        private void _BuildSlots()
        {
            if (slotPrefab == null || slotContainer == null)
                return;

            foreach (var entry in _entries)
            {
                TurretSelectSlotUI slot = Instantiate(slotPrefab, slotContainer);
                slot.Setup(entry.TurretDataId, entry.Icon, _OnSlotClicked);
                _slots.Add(slot);
            }
        }

        private void _OnSlotClicked(TurretSelectSlotUI slot)
        {
            SelectEntry entry = _entries.Find(e => e.TurretDataId == slot.TurretId);
            if (entry != null)
                _Select(entry);
        }

        private void _Select(SelectEntry entry)
        {
            _selected = entry;

            foreach (var slot in _slots)
                slot.SetSelected(slot.TurretId == entry.TurretDataId);

            // 스탯 카드 헤더를 선택 포탑 이름으로 바꾼다.
            if (cardHeaderText != null)
                cardHeaderText.text = entry.Name;

            if (selectedIconImage != null)
                selectedIconImage.sprite = entry.Icon;

            var status = entry.TurretData.TurretTrainStatus;

            if (attackText != null)
                attackText.text = Mathf.RoundToInt(status.AttackDamage).ToString();

            if (attackSpeedText != null)
                attackSpeedText.text = status.AttackInterval.ToString("0.0");

            if (rangeText != null)
                rangeText.text = Mathf.RoundToInt(status.AttackRange).ToString();

            if (bestSurvivalText != null)
            {
                float best = UserDataManager.Instance != null ? UserDataManager.Instance.GetBestSurvivalTime(entry.TurretDataId) : 0f;
                bestSurvivalText.text = _FormatTime(best);
            }
        }

        // 프리팹에 한국어로 박혀 있는 정적 라벨(스탯 이름·생존 시간·버튼)을 현재 언어로 갱신한다.
        private void _ApplyStaticTexts()
        {
            if (attackLabel != null)
                attackLabel.text = LocalizeHelper.GetByKey("Detail_Damage", "공격력");

            if (attackSpeedLabel != null)
                attackSpeedLabel.text = LocalizeHelper.GetByKey("Detail_Speed", "공속");

            if (rangeLabel != null)
                rangeLabel.text = LocalizeHelper.GetByKey("Detail_Range", "사거리");

            if (bestSurvivalTitle != null)
                bestSurvivalTitle.text = LocalizeHelper.GetByKey("UI_TurretSelect_BestSurvival", "최장 생존 시간");

            if (bestSurvivalHint != null)
                bestSurvivalHint.text = LocalizeHelper.GetByKey("UI_TurretSelect_BestSurvivalHint", "해당 포탑으로 기록한\n최고 생존 시간입니다.").Replace("\\n", "\n");

            if (departText != null)
                departText.text = LocalizeHelper.GetByKey("UI_TurretSelect_Depart", "출발 ≫");
        }

        private string _FormatTime(float seconds)
        {
            if (seconds <= 0f)
                return "--:--";

            int total = Mathf.FloorToInt(seconds);

            return $"{total / 60:00}:{total % 60:00}";
        }

        private void _OnDepart()
        {
            // 선택된 포탑이 없으면(목록이 비면) null로 저장되어 무기 없이 시작한다.
            if (UserDataManager.Instance != null)
                UserDataManager.Instance.SelectedTurretId = _selected != null ? _selected.TurretDataId : null;

            _onDepart?.Invoke();
            Destroy(gameObject);
        }

        // 뒤로가기: 포탑 선택을 취소하고 로비 씬으로 돌아간다.
        // SceneController.LoadScene이 내부(PrepareForSceneChange)에서 TimeManager.Resume·StopAllSFX를
        // 처리하므로, TimeManager.Pause로 멈춰 있던 게임 상태도 함께 정리된다.
        private void _OnBack()
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayBGM(SoundType.BGM_Lobby);

            SceneController.LoadScene(LobbySceneIndex, false);
        }
    }
}
