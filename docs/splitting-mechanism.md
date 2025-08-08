# FlexiPane 화면분할 메커니즘 설계

## 개요

FlexiPane.WPF의 동적 화면분할 시스템에 대한 트리 구조 기반 설계 문서입니다. 이 시스템은 FlexiPanel을 최상위 컨테이너로 하여 FlexiPaneContainer와 FlexiPaneItem 간의 계층적 구조를 통해 런타임에 UI 영역을 동적으로 분할하고 병합합니다.

## 컨트롤 계층 구조

### 기본 구조
```
FlexiPanel (최상위 컨테이너)
└── RootContent: UIElement
    ├── FlexiPaneItem (단일 패널)
    └── FlexiPaneContainer (분할 컨테이너)
        ├── FirstChild: UIElement
        └── SecondChild: UIElement
```

### 역할 정의
- **FlexiPanel**: 전역 상태 관리 및 Attached Property 제공
- **FlexiPaneContainer**: 분할된 영역을 관리하는 컨테이너 (Grid 기반)
- **FlexiPaneItem**: 실제 콘텐츠를 담는 개별 패널

## 트리 구조 변화 패턴

### 시나리오 1: 기본 분할 (1영역 → 2영역)

#### 초기 상태
```
FlexiPanel
└── RootContent: FlexiPaneItem(A)
    └── Content: UserContentA
```

#### 분할 후 구조
```
FlexiPanel
└── RootContent: FlexiPaneContainer(Root)
    ├── FirstChild: FlexiPaneItem(A)
    │   └── Content: UserContentA (기존)
    └── SecondChild: FlexiPaneItem(B)
        └── Content: UserContentB (새로 생성)
```

### 시나리오 2: 중첩 분할 (2영역 → 3영역)

#### 분할 전 (2영역)
```
FlexiPanel
└── RootContent: FlexiPaneContainer(Root)
    ├── FirstChild: FlexiPaneItem(A)
    │   └── Content: UserContentA
    └── SecondChild: FlexiPaneItem(B)
        └── Content: UserContentB
```

#### B 영역 분할 후 (3영역)
```
FlexiPanel
└── RootContent: FlexiPaneContainer(Root)
    ├── FirstChild: FlexiPaneItem(A)
    │   └── Content: UserContentA
    └── SecondChild: FlexiPaneContainer(Nested)
        ├── FirstChild: FlexiPaneItem(B)
        │   └── Content: UserContentB (기존)
        └── SecondChild: FlexiPaneItem(C)
            └── Content: UserContentC (새로 생성)
```

### 시나리오 3: 복잡한 중첩 (3영역 → 4영역)

#### A 영역 분할 후 (4영역)
```
FlexiPanel
└── RootContent: FlexiPaneContainer(Root)
    ├── FirstChild: FlexiPaneContainer(NestedLeft)
    │   ├── FirstChild: FlexiPaneItem(A1)
    │   │   └── Content: UserContentA1 (기존 A에서 분할)
    │   └── SecondChild: FlexiPaneItem(A2)
    │       └── Content: UserContentA2 (새로 생성)
    └── SecondChild: FlexiPaneContainer(NestedRight)
        ├── FirstChild: FlexiPaneItem(B)
        │   └── Content: UserContentB
        └── SecondChild: FlexiPaneItem(C)
            └── Content: UserContentC
```

## 영역 제거 패턴

### 시나리오 4: 리프 패널 제거 (3영역 → 2영역)

#### 제거 전 (3영역)
```
FlexiPanel
└── RootContent: FlexiPaneContainer(Root)
    ├── FirstChild: FlexiPaneItem(A)
    │   └── Content: UserContentA
    └── SecondChild: FlexiPaneContainer(Nested)
        ├── FirstChild: FlexiPaneItem(B)
        │   └── Content: UserContentB
        └── SecondChild: FlexiPaneItem(C) ← 제거 대상
            └── Content: UserContentC
```

#### C 패널 제거 후 (2영역) - 올바른 동작
```
FlexiPanel
└── RootContent: FlexiPaneContainer(Root)
    ├── FirstChild: FlexiPaneItem(A)
    │   └── Content: UserContentA
    └── SecondChild: FlexiPaneItem(B) ← Nested 컨테이너가 제거되고 B가 승격
        └── Content: UserContentB
```

**핵심 규칙**: 컨테이너의 자식이 하나만 남으면 해당 자식을 부모 컨테이너로 승격

### 시나리오 5: 중간 컨테이너 정리 (4영역 → 3영역)

#### 제거 전 (4영역)
```
FlexiPanel
└── RootContent: FlexiPaneContainer(Root)
    ├── FirstChild: FlexiPaneContainer(NestedLeft)
    │   ├── FirstChild: FlexiPaneItem(A1)
    │   └── SecondChild: FlexiPaneItem(A2) ← 제거 대상
    └── SecondChild: FlexiPaneContainer(NestedRight)
        ├── FirstChild: FlexiPaneItem(B)
        └── SecondChild: FlexiPaneItem(C)
```

#### A2 패널 제거 후 (3영역)
```
FlexiPanel
└── RootContent: FlexiPaneContainer(Root)
    ├── FirstChild: FlexiPaneItem(A1) ← NestedLeft 제거, A1 승격
    │   └── Content: UserContentA1
    └── SecondChild: FlexiPaneContainer(NestedRight)
        ├── FirstChild: FlexiPaneItem(B)
        │   └── Content: UserContentB
        └── SecondChild: FlexiPaneItem(C)
            └── Content: UserContentC
```

### 시나리오 6: 최종 단순화 (2영역 → 1영역)

#### 제거 전 (2영역)
```
FlexiPanel
└── RootContent: FlexiPaneContainer(Root)
    ├── FirstChild: FlexiPaneItem(A)
    │   └── Content: UserContentA
    └── SecondChild: FlexiPaneItem(B) ← 제거 대상
        └── Content: UserContentB
```

#### B 패널 제거 후 (1영역)
```
FlexiPanel
└── RootContent: FlexiPaneItem(A) ← Root 컨테이너 제거, A 직접 승격
    └── Content: UserContentA
```

## 영역 제거 알고리즘 설계

### 1단계: 대상 확인
```
제거할 FlexiPaneItem → 직접 부모 FlexiPaneContainer 찾기
```

### 2단계: 형제 요소 결정
```
FlexiPaneContainer
├── FirstChild: FlexiPaneItem(제거대상) → 형제는 SecondChild
└── SecondChild: FlexiPaneItem(형제)

또는

FlexiPaneContainer
├── FirstChild: FlexiPaneItem(형제)
└── SecondChild: FlexiPaneItem(제거대상) → 형제는 FirstChild
```

### 3단계: 부모의 부모(GrandParent) 확인
```
GrandParent (FlexiPaneContainer 또는 FlexiPanel)
└── Child: FlexiPaneContainer(부모)
    ├── FirstChild: FlexiPaneItem(제거대상)
    └── SecondChild: FlexiPaneItem(형제) → 이 형제를 GrandParent로 승격
```

### 4단계: 구조 변경 실행
1. **형제 요소를 부모 컨테이너에서 분리**
2. **GrandParent에서 부모 컨테이너를 형제 요소로 교체**
3. **부모 컨테이너와 제거 대상 정리**

### 5단계: 빈 컨테이너 정리
- 빈 FlexiPaneContainer 제거
- 불필요한 중간 계층 정리
- 단일 자식만 있는 컨테이너의 구조 단순화

## 특수 케이스 처리

### RootContent 처리
```
FlexiPanel.RootContent가 FlexiPaneContainer인 경우:
- 자식이 하나만 남으면 해당 자식을 RootContent로 직접 설정
- FlexiPaneItem이면 단일 패널 상태로 복원
- FlexiPaneContainer이면 계층 구조 유지
```

### 빈 컨테이너 식별
```
FlexiPaneContainer가 빈 상태:
- FirstChild == null && SecondChild == null

처리 방법:
- 부모에서 제거
- 리소스 정리
```

### 단일 자식 컨테이너 승격
```
FlexiPaneContainer에 자식이 하나만 있는 경우:
- (FirstChild != null && SecondChild == null) 또는
- (FirstChild == null && SecondChild != null)

처리 방법:
- 유일한 자식을 부모 레벨로 승격
- 현재 컨테이너 제거
```

## 분할 방향 및 속성

### 분할 방향
- **IsVerticalSplit = true**: 세로 분할 (좌우)
- **IsVerticalSplit = false**: 가로 분할 (상하)

### 분할 비율
- **SplitRatio**: 0.1 ~ 0.9 범위의 첫 번째 자식 크기 비율

### 계층별 분할 정보
```
FlexiPaneContainer(Root, Vertical, 0.6)
├── FirstChild: FlexiPaneItem(A) [60% 너비]
└── SecondChild: FlexiPaneContainer(Nested, Horizontal, 0.4)
    ├── FirstChild: FlexiPaneItem(B) [40% 높이]
    └── SecondChild: FlexiPaneItem(C) [60% 높이]
```

## 구현 시 고려사항

### 이벤트 전파
- FlexiPaneItem → FlexiPanel 로 Routed Event 버블링
- Attached Property를 통한 상태 상속

### 리소스 관리
- 제거된 FlexiPaneItem의 이벤트 핸들러 해제
- 빈 FlexiPaneContainer의 메모리 정리
- Visual Tree와 Logical Tree 동기화

### 검증 로직
- 최소 1개 패널 유지 (마지막 패널 제거 방지)
- 부모-자식 관계 무결성 검증
- 순환 참조 방지

이 설계를 기반으로 구조 변화가 예측 가능하고 일관성 있게 이루어져야 합니다.