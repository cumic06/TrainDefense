---
# ============================================================
# DESIGN.md — game-ui-hig 생성 산출물
# 대상: TrainDefense 로비 스킬 트리 화면
# 작성: 2026-07-15
# 수치 옆: [강제]/[권장]/[통념]/[v1]=이번에 정한 값
# ⚠️ 이 스킬의 기본 프로파일은 PC/콘솔이다. TrainDefense는 Android 단독이라
#    세이프에어리어·텍스트최소·입력 레지스터를 모바일로 재프로파일했다(아래 platform-profiles).
# ============================================================
name: "TrainDefense — 스킬 트리 (로비)"
genre: "tower-defense / roguelite-progression"
target_platforms: ["android"]
reference_resolution: "1920x1080"   # 프로젝트 CanvasScaler 컨벤션

# === 정체성 결정 (생성기의 핵심 출력) ===
identity:
  preset: "stylized (규율 있는 대담함으로 조정)"
  expression-signature: "점등되는 선로 — 습득한 경로가 오렌지로 불이 들어오는 살아있는 철로. 트리의 연결선이 장식이 아니라 게임 세계의 실제 선로다."
  trends-on:
    - "T7 타이포-as-그래픽 (오버사이즈 비트맵 헤더 · 계열명을 그래픽으로)"
    - "T6 모션/juice (습득 순간의 선로 점등 스윕 + 노드 펀치)"
    - "T5 디제틱 — 부분형만 (선로 커넥터. 화면 전체를 디제틱화하지 않음)"
    - "T2 접근성 이중부호화 (위생)"
  trends-off:
    - "T8 글래스모피즘 — 모바일 실시간 블러 성능 비용 + 30~50노드 위 반투명은 대비 붕괴"
    - "T9 에디토리얼 비대칭 — 고정보 화면에 혼돈형 레이아웃은 과업 성공률 8~10%로 붕괴. 프레임에만 허용, 노드 배치엔 금지"
    - "T10 레이디얼 — 30~50노드에 방사형은 탐색 속도 자살"
    - "T1 게임패드 포커스 — 터치 전용이라 해당 없음 (press 상태로 대체)"
    - "T3 HDR UI 밝기 — 모바일이라 해당 없음"

# --- 색: 시맨틱 역할 ---
# 출처: 팝업 컨벤션(Popup_PermanentUpgrade/Collection 실측) + 게임 아트 팔레트(turret-sprite-ai-prompt.md)
colors:
  dim:              "#000000"   # @60% — 전체화면 딤. PopupTween 주석에 명시된 고정 컨벤션 [기존]
  surface:          "#2C2E3A"   # @98% — 팝업 메인 패널 [v1 #212330 → v3 2026-08-16: 채도 14%·명도 +4% ("너무 파랑"→"칙칙" 피드백 절충)]
  surface-sunken:   "#1B1E24"   # 트리 캔버스 배경 — 패널보다 깊게 파서 선로가 떠 보이게 [v1 #0f131c → v3]
  surface-raised:   "#353B46"   # 노드 배경 [v1 #262f40 → v3]
  surface-line:     "#424B58"   # 헤어라인/구분선·버튼·슬롯 카드 [v1 #343f52 → v3]
  accent:           "#e47a3c"   # 습득/활성/점등된 선로 전용. 아트 팔레트 확정 포인트 오렌지. 장식 금지 [v1]
  mastered:         "#FFDE33"   # 만렙 노드 전용 골드 [기존 LuckyUI]
  danger-fill:      "#B34040"   # 위험/비용부족 — 면(fill)에만 [기존 컨벤션]
  danger-text:      "#E06666"   # 위험 텍스트. ⚠️ #B34040은 surface 위 2.78:1로 대비 미달 → 텍스트는 반드시 이 값 [v1]
  on-surface:       "#DDDDE2"   # 본문 [기존 #D9D9E6 → v2 탈채도] — surface 위 ≈11:1
  on-surface-muted: "#999AA6"   # 잠김/비활성 [기존 컨벤션, 채도 7%라 v2 탈채도 대상 아님] — surface 위 5.7:1
  on-accent:        "#f8e0bd"   # accent 면 위 텍스트 (아트 팔레트 라이트) [v1]
  # focus-ring: 없음 — 터치 전용이라 게임패드 포커스 링 미해당

# --- 타이포: 역할 기반 스케일 (px@1080p) ---
# ⚠️ 템플릿 기본(body 16)은 데스크톱 값이라 못 쓴다. 모바일 1920×1080 랜드스케이프에서
#    1dp ≈ 2.6px → Android 최소 12sp ≈ 31px. 아래는 sp 환산 후 4pt 그리드에 스냅한 값.
typography:
  fontFamily-display: "DNFBitBitv2 SDF"   # Assets/02_Resources/Font/ — 게임 표준. 한글 O. 비트맵 성격이 곧 정체성
  fontFamily-base:    "DNFBitBitv2 SDF"
  fontFamily-fallback: "Noto Sans SDF 계열"  # 로컬라이즈 시스템이 CJK/Arabic/Thai 자동 전환
  scale: { caption: 28, small: 32, body: 36, strong: 40, title-s: 48, title-l: 64, display: 96 }
  min-functional-px: 32   # ≈12sp
  hard-floor-px: 28       # ≈11sp
  # ⚠️ display(96)는 로컬라이즈 대상이면 폰트 폴백 시 비트맵 성격이 무너진다(Noto는 비트맵 아님).
  #    → 헤더 문구는 짧게 유지 + autoSizing 필수. 상세는 §3.

# --- 스페이싱: 8pt 그리드 (모바일 스케일) ---
spacing:
  grid: { s1: 8, s2: 16, s3: 24, s4: 32, s6: 48, s8: 64, s12: 96 }
  roles: { inline-gap: 16, intra-group: 16, node-gap: 48, card-pad: 32, block-gap: 32, section-gap: 48, panel-margin: 24 }

# --- 라운드 ---
rounded: { sm: 8, lg: 20 }   # TurretSelectSlotBg 9-slice border 20 에 맞춤

# --- 머티리얼·모션 성격 ---
material: "solid + 헤어라인 (블러 없음). 노드=TurretSelectSlotBg 9-slice 틴트, 연결선=Rail.png 타일링"
motion:
  character: "tactile"   # 절제하지 않되 과하지 않게. 습득 순간에 예산을 몰아준다
  transitions: { press-ms: 100, acquire-ms: 240, rail-sweep-ms: 320, panel-ms: 280 }
  reduced-motion: "지원 — 점등 스윕→즉시 색전환, 펀치 제거. 기존 옵션 '모션 감소' 항목과 연동"
  helper: "PopupTween.PlayShow/PlayHide 재사용 (OutBack + 페이드, SetUpdate(true))"

# --- 입력 레지스터 ---
input-registers:
  active: "touch"
  min-target-px: 128    # ≈49dp ≥ Android 48dp [강제급/Material]
  # hover 없음 → hover-only 정보 자동 금지. 포커스 링 대신 press 상태.

# --- 세이프 에어리어 (모바일 재프로파일) ---
safe-area:
  title-safe: "panel-margin 24px + 노치/펀치홀 회피"
  foldable: "⚠️ 필수 — 부모 폭 초과 시 localScale 균등 축소. TrainInfoUI/ShopUI에서 2회 물린 패턴"
  user-adjustable: false

# --- 플랫폼 프로파일 ---
platform-profiles:
  mobile-landscape: { base-resolution: "1920x1080", input: "touch", viewing-distance: "~30cm" }
---

## 1. 비주얼 테마 & 분위기

기차로 몰려오는 적을 막는 게임의 판타지는 **"내 열차 편성을 키운다"**다. 스킬 트리는 그 성장을 **노선 확장**으로 번역한다 — 출발역에서 시작해 선로를 깔며 종착역까지 뻗어나가는 화면.

톤은 스타일라이즈드지만 **"규율 있는 대담함"**으로 조정했다. 프리셋 원본(Persona 5식)은 혼돈형 에디토리얼까지 허용하지만, 이 화면은 30~50개 노드를 정확히 읽고 비교해야 하는 **고정보 화면**이다. 대담함은 **타이포·선로·습득 모션**에 싣고, **노드 격자 자체는 규율 있게** 간다.

## 2. 컬러 팔레트 & 역할

기존 팝업 컨벤션(남색/차콜)과 게임 아트 팔레트(`turret-sprite-ai-prompt.md`)가 톤이 일치해서 **양쪽을 합쳐 쓴다** — 새 색을 발명하지 않는다.

- **`accent` #e47a3c(오렌지)는 "점등된 선로와 습득한 노드"에만.** 이 화면에서 오렌지가 보이면 곧 "내가 가진 것"이다. 장식·강조·버튼에 흘리면 시그니처가 죽는다.
- **`mastered` #FFDE33(골드)는 만렙 노드에만.** 오렌지 위계의 최상단.
- **`surface-sunken` #0f131c**로 트리 캔버스를 패널(#212330)보다 파서, 선로가 홈에 놓인 것처럼 보이게 한다. 깊이는 블러가 아니라 명도로 만든다.
- **⚠️ `danger-fill` #B34040은 텍스트 금지.** surface 위 대비 2.78:1로 WCAG 3:1도 미달이다(가드레일 `contrast-below-wcag`). 기존 팝업이 이 색을 텍스트에 쓰고 있다면 그건 기존 버그지 계승 대상이 아니다. 텍스트는 `danger-text` #E06666(4.69:1).
- 최악 배경 대응: 트리 캔버스가 단색(#0f131c)이라 HUD처럼 배경이 요동치지 않는다. 별도 외곽선 불필요.

## 3. 타이포그래피

**DNFBitBitv2 SDF 단일 패밀리.** 20개 이상 프리팹이 이미 쓰는 사실상 표준이고, 비트맵 성격 자체가 캐릭터라서 별도 디스플레이 폰트를 들일 이유가 없다. 위계는 폰트 교체가 아니라 **크기와 색**으로 만든다.

T7(타이포-as-그래픽)은 **헤더와 계열명**에서 실행한다 — `display 96`으로 "스킬 트리", 각 계열 레인 상단에 `title-l 64`로 화력/방어/유틸을 세로 레일 옆에 크게 박아 그래픽 요소로 쓴다.

**⚠️ 다국어 함정 (이 프로젝트 고유):** 31개 언어를 지원하고 CJK/Arabic/Thai는 Noto Sans로 폴백된다. **폴백되는 순간 비트맵 성격이 사라진다** — 아랍어 헤더는 완전히 다른 폰트로 보인다. 그래서:
- 오버사이즈 텍스트는 **짧은 단어만** (긴 문장에 96px 금지)
- 모든 라벨에 **autoSizing 필수** (색약 라벨이 버튼 밖으로 넘친 전례 있음)
- 헤더가 폴백돼도 레이아웃이 안 깨지는지 아랍어/태국어로 확인

`min-functional-px: 32`(≈12sp)는 Android 최소선이다. 템플릿 기본값 16px을 그대로 쓰면 `text-below-min` 위반이다.

## 4. 컴포넌트 & 레이아웃

**전체 골격 (1920×1080 랜드스케이프):**

```
┌────────────────────────────────────────────────────────┐  딤 #000 @60%
│ ┌────────────────────────────────────────────────────┐ │
│ │  스킬 트리                        ▣ 320    [X]     │ │  헤더 112px
│ ├──────────────────────────────────┬─────────────────┤ │
│ │                                  │                 │ │
│ │   화력        방어        유틸    │   화력 증강      │ │
│ │    ┃           ┃          ┃      │   Lv 2 / 5      │ │
│ │    ◆═══╗       ◆          ◇      │  ─────────────  │ │
│ │    ┃    ║      ┃          ┃      │   공격력 +12%   │ │  상세 패널
│ │    ◆    ◇      ◇          ◇      │   → +18%        │ │  520px
│ │    ┃           ┃                 │                 │ │
│ │    ●(출발역)   ●          ●      │  [ 획득  200 ▣ ]│ │
│ │                                  │                 │ │
│ │      ← 세로 스크롤 (아래→위) →    │                 │ │
│ └──────────────────────────────────┴─────────────────┘ │
└────────────────────────────────────────────────────────┘
   트리 스크롤 1160px          gap 32        상세 520px
```

- **그리드:** 6컬럼 × N행. 노드 144px + `node-gap` 48 = 192px 피치. 트리 폭 1160px에 정확히 6컬럼.
- **흐름은 아래→위.** 출발역이 하단, 종착역이 상단. 열차가 전진하는 방향과 일치시켜 은유를 유지한다. 세로 스크롤 하나뿐이라 30~50노드가 들어간다(가로 스크롤 금지 — 탐색 비용).
- **레인 = 계열.** 화력/방어/유틸이 각각 세로 선로 레인. 레인 간 환승(교차 조건)은 `StageSelectRailUI.png`(Y분기)로 표현.
- **머티리얼:** 노드 배경 = `TurretSelectSlotBg.png`(9-slice border 20) 틴트. 선택 링 = `TurretSelectHighlight.png`. 연결선 = `Rail.png` 타일링, 분기 = `StageSelectRailUI.png`. **블러 없음.**

**5상태 (터치 — hover/focus 없음):**

| 상태 | 노드 | 선로 | 이중부호화 |
|---|---|---|---|
| 잠김 | `SpriteGrayscale` 머티리얼 + `on-surface-muted` | 미점등 (#343f52 침목만) | 그레이 + 선로 꺼짐 + "Lv 0/5" |
| 획득 가능 | `surface-raised` + accent 테두리 | 미점등, 진입 선로만 accent 맥동 | 색 + 테두리 + 비용 표시 |
| 습득 | accent 틴트 | **점등** (accent) | 색 + 선로 켜짐 + "Lv 2/5" |
| 만렙 | `mastered` 골드 + 골드 아웃라인 셰이더 | 점등 (골드) | 색 + 아웃라인 + "MAX" |
| Press | scale 0.95, 100ms | — | — |

**⚠️ `color-only-signal` 방어:** 잠김/습득을 색으로만 구분하지 않는다. **선로 점등 여부(형태) + 레벨 텍스트**가 항상 병행한다 — 그레이스케일로 출력해도 읽힌다.

**폴더블:** 트리 스크롤 영역과 상세 패널 모두 `_FitToAvailableWidth` 패턴 적용. 부모 폭 초과 시 `localScale` 균등 축소. 이 프로젝트에서 이미 두 번 물린 곳이다.

## 5. 모션 성격

**촉각적(tactile).** 모션 예산을 **습득 순간 하나에 몰아준다** — 이 화면의 존재 이유가 그 순간이기 때문이다.

- **습득 시퀀스 (총 ~560ms):** 노드 펀치 scale 1.0→1.25→1.0 (OutBack, 240ms) → 직후 이전 노드에서 새 노드로 **선로가 오렌지로 흐르며 점등**(스윕, 320ms). 선로가 "이어졌다"는 것이 보상 피드백이다.
- **탭 press:** scale 0.95, 100ms — `feedback-over-100ms` 통과.
- **팝업 등장/퇴장:** `PopupTween.PlayShow/PlayHide` 그대로 재사용. 이미 `SetUpdate(true)`로 timeScale=0을 회피하고, 딤은 페이드만 하고 패널만 스케일하는 문제도 해결돼 있다.
- **모션감소:** 점등 스윕 → 즉시 색 전환, 펀치 제거. 기존 접근성 탭 옵션과 연동.
- 나머지는 조용하다. 스크롤·선택은 무모션에 가깝게 — juice를 고르게 뿌리면 습득 순간이 묻힌다.

## 6. 디제틱 동기

**부분 디제틱(세그먼트형).** 화면 전체가 열차 콘솔인 척하지 않는다 — 완전 디제틱은 반복 비용이 크고(Helldivers 2 개발사도 공개 인정), 이 트리는 밸런싱하며 자주 고칠 화면이다.

디제틱은 **연결선 하나에만** 적용한다: 스킬 노드를 잇는 선은 추상적인 선이 아니라 **게임 맵에 실제로 깔린 그 선로**(`Rail.png`)다. 프레임·패널·타이포는 논다이제틱으로 두고 명료성을 우선한다.

## 7. 결정 근거 (4단계)

- **정체성:** stylized → **"규율 있는 대담함"**으로 조정. 대담함은 타이포·선로·습득 모션에, 노드 격자는 규율 있게. 프리셋 원본의 에디토리얼 비대칭은 고정보 화면이라 의도적으로 버렸다.
- **켠 트렌드:** T7 타이포-as-그래픽(프리셋 핵심), T6 모션/juice(습득 순간에 집중), T5 디제틱-부분형(선로 커넥터만) + T2 이중부호화(위생).
- **끈 것과 이유 — 이게 정체성을 설명한다:**
  - **글래스모피즘(T8)** — 모바일 블러 비용 + 30~50노드 위 반투명은 대비 붕괴. 깊이는 블러가 아니라 **명도**로 만든다(#0f131c vs #212330).
  - **에디토리얼 비대칭(T9)** — 혼돈형 안티디자인은 정보 과밀 화면에서 과업 성공률 8~10%로 붕괴. 프레임엔 허용, 노드 배치엔 금지.
  - **레이디얼(T10)** — 30~50노드에 방사형은 속도 자살. Marvel Rivals가 방사형을 그리드로 되돌린 사례와 같은 판단.
  - **게임패드 포커스(T1)·HDR(T3)** — 터치 전용 Android라 해당 없음.
- **표현 시그니처:** **점등되는 선로.** 습득 경로가 오렌지로 불이 들어오는 살아있는 철로. 이 결정 하나가 (a) 기차 게임 정체성 (b) 기성 에셋 재활용(`Rail.png` + `StageSelectRailUI.png` Y분기) (c) 습득 순간의 juice (d) 색 없이도 읽히는 이중부호화를 **동시에** 해결한다.
- **왜 노선도 디제틱이 아니라 스타일라이즈드인가:** 사용자 선택. 다만 선로 은유는 스타일라이즈드 안에서 **커넥터 재료로만** 흡수했다 — 화면 전체를 노선도로 만들지 않아 반복 비용을 피했다.

## 8. 자가 검증 결과 (5단계)

| 룰 | 결과 |
|---|---|
| `safe-area-violation` | ✅ panel-margin 24px + 노치 회피. ⚠️ **폴더블 축소 미구현 시 재발** — 구현 시 필수 |
| `text-below-min` | ✅ hard-floor 28px(≈11sp), 본문 36px(≈14sp). 템플릿 기본 16px은 데스크톱 값이라 폐기하고 재프로파일 |
| `contrast-below-wcag` | ⚠️→✅ **수정 발생.** `#B34040`(기존 팝업 컨벤션)이 surface 위 2.78:1로 UI 3:1도 미달 → 텍스트용 `#E06666`(4.69:1) 분리. 나머지: on-surface 11.3:1 / muted 5.7:1 / accent on sunken 6.4:1 / mastered 14.2:1 전부 통과 |
| `hover-only-dependency` | ✅ 터치 전용이라 구조적으로 불가 |
| `controller-glyph-mismatch` | N/A (터치) |
| `missing-focus-state` | N/A (터치) — press 상태로 대체 |
| `color-only-signal` | ✅ 선로 점등(형태) + 레벨 텍스트가 색과 항상 병행. 그레이스케일 출력에서도 판독 |
| `hdr-ui-too-bright` | N/A (모바일) |
| `hud-over-budget` | N/A (메뉴 화면) |
| `off-grid-spacing` | ✅ 8pt 그리드(8/16/24/32/48/64/96). 타입도 4pt 스냅 |
| `type-scale-sprawl` | ✅ 7단계 역할 스케일에서만 선택 |
| `feedback-over-100ms` | ✅ press 100ms |
| **`generic-identity-less`** | ✅ 시그니처 = "점등되는 선로". 비어있지 않음 |
| `all-trends-on` | ✅ 미감 트렌드 3개(T7/T6/T5부분)로 절제. 4개를 명시적으로 끔 |
| `flat-hierarchy` | ✅ display 96 ↔ body 36 (2.7배), sunken/surface/raised 3단 명도, accent 단일 용도 |
| `static-no-microinteraction` | ✅ press·습득·점등 정의 |
| `convention-broken-in-hot-path` | ✅ 비대칭·키네틱을 노드 격자에서 배제 |

**미해결 — 구현 전 필요:**
- 🔴 **스킬 아이콘 신규 제작.** 프로젝트에 스킬/능력 전용 아이콘이 없다. Tiny Swords(Icon_01~12)는 중세 판타지라 톤 불일치 → placeholder로만. `turret-sprite-ai-prompt.md`의 32×32 픽셀아트 프롬프트(남색+오렌지)를 재사용해 계열별로 생성하고, `Resources.Load<Sprite>("Sprite/"+iconId)` 컨벤션에 태울 것.
- 🟡 **자물쇠 아이콘 없음.** 프로젝트 컨벤션은 `Color.black` 틴트(도감) 또는 `SpriteGrayscale` 머티리얼. 이중부호화가 선로 점등+텍스트로 이미 확보돼 있어 자물쇠는 선택.
