# FlexiPane.WPF API 설계

## API 개요

FlexiPane.WPF는 직관적이고 사용하기 쉬운 API를 제공하여 개발자들이 빠르게 화면분할 기능을 구현할 수 있도록 설계되었습니다.

## 네임스페이스 구조

**FlexiPane.WPF**
- **Controls/** - 주요 컨트롤들
  - FlexiPaneContainer
  - FlexiPaneItem
  - FlexiPaneHost
- **Managers/** - 관리 클래스들
  - FlexiPaneManager
  - LayoutManager
- **Models/** - 데이터 모델들
  - PaneLayout
  - SplitConfiguration
- **Events/** - 이벤트 관련
  - PaneSplitEventArgs
  - PaneClosingEventArgs
- **Themes/** - 테마 관련
  - FlexiPaneTheme
  - ThemeManager
- **Converters/** - 값 변환기들
  - SplitRatioConverter
- **Extensions/** - 확장 메서드들
  - PaneExtensions

## 핵심 API

### FlexiPaneHost (최상위 컨테이너)

FlexiPane 시스템의 최상위 호스트 컨트롤입니다.

**주요 속성:**
- **RootPane** - 루트 패널 참조
- **IsSplitModeEnabled** - 분할 모드 활성화 여부
- **Theme** - 현재 적용된 테마
- **Constraints** - 분할 제약 조건

**주요 메서드:**
- **ToggleSplitMode()** - 분할 모드 토글
- **SerializeLayout()** - 레이아웃을 JSON으로 직렬화
- **RestoreLayout(string)** - 레이아웃 복원
- **ClearAllPanes()** - 모든 패널 제거 (루트만 남김)

**이벤트:**
- **PaneSplit** - 패널이 분할될 때 발생
- **PaneClosed** - 패널이 제거될 때 발생
- **LayoutChanged** - 레이아웃이 변경될 때 발생

### FlexiPaneItem (패널 아이템)

분할 가능한 개별 패널을 나타냅니다.

**주요 속성:**
- **PaneId** - 패널 고유 식별자
- **Title** - 패널 제목
- **Icon** - 패널 아이콘
- **CanClose** - 닫기 버튼 표시 여부
- **CanSplit** - 분할 가능 여부
- **MinimumSize/MaximumSize** - 최소/최대 크기
- **State** - 패널 상태
- **Tag** - 사용자 정의 데이터

**Command 속성:**
- **CloseCommand** - 패널 닫기 명령
- **SplitVerticalCommand** - 수직 분할 명령
- **SplitHorizontalCommand** - 수평 분할 명령
- **ToggleSplitModeCommand** - 분할 모드 토글 명령

**주요 메서드:**
- **Split(Orientation, double, object)** - 패널 분할
- **Close()** - 패널 제거
- **Activate()** - 패널 활성화
- **Focus()** - 포커스 설정

**이벤트:**
- **SplitRequested** - 분할 요청 시 발생
- **Closing** - 닫기 요청 시 발생 (취소 가능)
- **Closed** - 닫힘 완료 시 발생
- **Activated** - 활성화 시 발생
- **SizeChanged** - 크기 변경 시 발생

### FlexiPaneManager (정적 관리자)

FlexiPane 시스템의 전역 관리자 클래스입니다.

**전역 속성:**
- **DefaultTheme** - 전역 기본 테마
- **GlobalConstraints** - 전역 분할 제약
- **IsDebugMode** - 디버그 모드 활성화 여부

**패널 관리 메서드:**
- **SplitPane(FlexiPaneItem, Orientation, double, object)** - 패널 분할
- **RemovePane(FlexiPaneItem)** - 패널 제거
- **MovePane(FlexiPaneItem, FlexiPaneItem, DropPosition)** - 패널 이동
- **SwapPane(FlexiPaneItem, FlexiPaneItem)** - 패널 교체

**레이아웃 관리 메서드:**
- **SerializeLayout(FlexiPaneHost)** - 레이아웃 직렬화
- **RestoreLayout(FlexiPaneHost, PaneLayout)** - 레이아웃 복원
- **ValidateLayout(PaneLayout)** - 레이아웃 검증

**유틸리티 메서드:**
- **FindPane(FlexiPaneHost, string)** - 패널 찾기
- **GetAllPanes(FlexiPaneHost)** - 모든 패널 가져오기
- **GetActivePane(FlexiPaneHost)** - 활성 패널 가져오기
- **GetLayoutTree(FlexiPaneHost)** - 레이아웃 트리 구조 가져오기

**전역 이벤트:**
- **GlobalPaneChanged** - 전역 패널 변경 이벤트
- **GlobalLayoutChanged** - 전역 레이아웃 변경 이벤트

## 이벤트 시스템

### 주요 이벤트 아규먼트 클래스

**PaneSplitRequestedEventArgs**
- 패널 분할 요청 이벤트 데이터
- 소스 패널, 분할 방향, 비율 정보 포함
- Cancel 속성으로 취소 가능

**PaneClosingEventArgs**
- 패널 닫기 요청 이벤트 데이터 (취소 가능)
- 대상 패널과 닫기 사유 정보 포함

**LayoutChangedEventArgs**
- 레이아웃 변경 이벤트 데이터
- 변경 타입, 영향받은 패널, 이전/현재 레이아웃 정보

## 데이터 모델

### 분할 설정

**SplitConstraints**
- **MaxDepth** - 최대 중첩 깊이
- **MinimumPaneSize** - 최소 패널 크기 (픽셀)
- **AllowNestedSplit** - 중첩 분할 허용 여부
- **AllowAutoResize** - 자동 분할 비율 조정 허용 여부
- **UseAnimation** - 분할 시 애니메이션 사용 여부
- **AnimationDuration** - 애니메이션 지속 시간

**PaneLayout**
- **Version** - 레이아웃 버전
- **CreatedAt** - 생성 시간
- **Root** - 루트 노드
- **Metadata** - 메타데이터

**PaneLayoutNode**
- **Id** - 노드 ID
- **NodeType** - 노드 타입 (Container/Item)
- **SplitOrientation** - 분할 방향 (Container 노드인 경우)
- **SplitRatio** - 분할 비율 (Container 노드인 경우)
- **ContentInfo** - 콘텐츠 정보 (Item 노드인 경우)
- **Children** - 자식 노드들

## 확장 메서드

**PaneExtensions 클래스**
- **GetRootHost(FlexiPaneItem)** - 패널의 루트 호스트 찾기
- **GetParentContainer(FlexiPaneItem)** - 패널의 부모 컨테이너 찾기
- **GetSiblings(FlexiPaneItem)** - 패널의 형제 패널들 가져오기
- **GetDepth(FlexiPaneItem)** - 패널의 중첩 깊이 가져오기
- **CanSplitSafely(FlexiPaneItem, SplitConstraints)** - 패널이 분할 가능한지 확인
- **ToJson(PaneLayout)** - 패널을 JSON으로 직렬화
- **FromJson(string)** - JSON에서 패널 레이아웃 복원

## 사용 패턴

### 기본 사용 패턴

**XAML 선언적 사용:**
FlexiPaneHost 내부에 FlexiPaneItem을 배치하여 기본적인 분할 가능한 영역 생성

**프로그래매틱 분할:**
코드에서 Split 메서드를 호출하여 동적 분할 수행

**이벤트 기반 처리:**
분할 및 제거 이벤트를 구독하여 커스텀 로직 구현

### 고급 사용 패턴

**커스텀 제약 조건:**
SplitConstraints를 통해 분할 규칙 정의

**레이아웃 영속성:**
SerializeLayout/RestoreLayout을 통한 레이아웃 저장/복원

**테마 커스터마이징:**
FlexiPaneTheme를 통한 시각적 스타일 변경

## API 설계 원칙

### 1. 직관성
- WPF 개발자들에게 익숙한 패턴 사용
- 명확하고 일관된 네이밍
- 최소한의 학습 곡선

### 2. 확장성
- 이벤트 기반 확장 포인트 제공
- 테마 시스템을 통한 시각적 커스터마이징
- 플러그인 아키텍처 지원

### 3. 성능
- 지연 로딩과 효율적인 리소스 관리
- 최적화된 레이아웃 알고리즘
- 메모리 누수 방지

### 4. 안정성
- 예외 안전한 API 설계
- 검증된 입력 파라미터 처리
- 명확한 에러 메시지

이 API 설계는 직관적이고 확장 가능하며, WPF의 기존 패턴을 따라 개발자들이 쉽게 학습하고 사용할 수 있도록 구성되었습니다.