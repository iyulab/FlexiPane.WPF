# FlexiPane Screen Splitting Mechanism Design

## Overview

This document describes the tree structure-based design for FlexiPane.WPF's dynamic screen splitting system. The system uses FlexiPanel as the top-level container and dynamically splits and merges UI areas at runtime through a hierarchical structure between FlexiPaneContainer and FlexiPaneItem.

## Control Hierarchy

### Basic Structure
```
FlexiPanel (Top-level container)
└── RootContent: UIElement
    ├── FlexiPaneItem (Single panel)
    └── FlexiPaneContainer (Split container)
        ├── FirstChild: UIElement
        └── SecondChild: UIElement
```

### Role Definitions
- **FlexiPanel**: Global state management and Attached Property provider
- **FlexiPaneContainer**: Container managing split areas (Grid-based)
- **FlexiPaneItem**: Individual panel containing actual content

## Tree Structure Transformation Patterns

### Scenario 1: Basic Split (1 area → 2 areas)

#### Initial State
```
FlexiPanel
└── RootContent: FlexiPaneItem(A)
    └── Content: UserContentA
```

#### After Split
```
FlexiPanel
└── RootContent: FlexiPaneContainer(Root)
    ├── FirstChild: FlexiPaneItem(A)
    │   └── Content: UserContentA (existing)
    └── SecondChild: FlexiPaneItem(B)
        └── Content: UserContentB (newly created)
```

### Scenario 2: Nested Split (2 areas → 3 areas)

#### Before Split (2 areas)
```
FlexiPanel
└── RootContent: FlexiPaneContainer(Root)
    ├── FirstChild: FlexiPaneItem(A)
    │   └── Content: UserContentA
    └── SecondChild: FlexiPaneItem(B)
        └── Content: UserContentB
```

#### After Splitting Area B (3 areas)
```
FlexiPanel
└── RootContent: FlexiPaneContainer(Root)
    ├── FirstChild: FlexiPaneItem(A)
    │   └── Content: UserContentA
    └── SecondChild: FlexiPaneContainer(Nested)
        ├── FirstChild: FlexiPaneItem(B)
        │   └── Content: UserContentB (existing)
        └── SecondChild: FlexiPaneItem(C)
            └── Content: UserContentC (newly created)
```

### Scenario 3: Complex Nesting (3 areas → 4 areas)

#### After Splitting Area A (4 areas)
```
FlexiPanel
└── RootContent: FlexiPaneContainer(Root)
    ├── FirstChild: FlexiPaneContainer(NestedLeft)
    │   ├── FirstChild: FlexiPaneItem(A1)
    │   │   └── Content: UserContentA1 (split from A)
    │   └── SecondChild: FlexiPaneItem(A2)
    │       └── Content: UserContentA2 (newly created)
    └── SecondChild: FlexiPaneContainer(NestedRight)
        ├── FirstChild: FlexiPaneItem(B)
        │   └── Content: UserContentB
        └── SecondChild: FlexiPaneItem(C)
            └── Content: UserContentC
```

## Area Removal Patterns

### Scenario 4: Leaf Panel Removal (3 areas → 2 areas)

#### Before Removal (3 areas)
```
FlexiPanel
└── RootContent: FlexiPaneContainer(Root)
    ├── FirstChild: FlexiPaneItem(A)
    │   └── Content: UserContentA
    └── SecondChild: FlexiPaneContainer(Nested)
        ├── FirstChild: FlexiPaneItem(B)
        │   └── Content: UserContentB
        └── SecondChild: FlexiPaneItem(C) ← To be removed
            └── Content: UserContentC
```

#### After Removing Panel C (2 areas) - Correct Behavior
```
FlexiPanel
└── RootContent: FlexiPaneContainer(Root)
    ├── FirstChild: FlexiPaneItem(A)
    │   └── Content: UserContentA
    └── SecondChild: FlexiPaneItem(B) ← Nested container removed, B promoted
        └── Content: UserContentB
```

**Core Rule**: When a container has only one child remaining, promote that child to the parent container

### Scenario 5: Intermediate Container Cleanup (4 areas → 3 areas)

#### Before Removal (4 areas)
```
FlexiPanel
└── RootContent: FlexiPaneContainer(Root)
    ├── FirstChild: FlexiPaneContainer(NestedLeft)
    │   ├── FirstChild: FlexiPaneItem(A1)
    │   └── SecondChild: FlexiPaneItem(A2) ← To be removed
    └── SecondChild: FlexiPaneContainer(NestedRight)
        ├── FirstChild: FlexiPaneItem(B)
        └── SecondChild: FlexiPaneItem(C)
```

#### After Removing Panel A2 (3 areas)
```
FlexiPanel
└── RootContent: FlexiPaneContainer(Root)
    ├── FirstChild: FlexiPaneItem(A1) ← NestedLeft removed, A1 promoted
    │   └── Content: UserContentA1
    └── SecondChild: FlexiPaneContainer(NestedRight)
        ├── FirstChild: FlexiPaneItem(B)
        │   └── Content: UserContentB
        └── SecondChild: FlexiPaneItem(C)
            └── Content: UserContentC
```

### Scenario 6: Final Simplification (2 areas → 1 area)

#### Before Removal (2 areas)
```
FlexiPanel
└── RootContent: FlexiPaneContainer(Root)
    ├── FirstChild: FlexiPaneItem(A)
    │   └── Content: UserContentA
    └── SecondChild: FlexiPaneItem(B) ← To be removed
        └── Content: UserContentB
```

#### After Removing Panel B (1 area)
```
FlexiPanel
└── RootContent: FlexiPaneItem(A) ← Root container removed, A directly promoted
    └── Content: UserContentA
```

## Area Removal Algorithm Design

### Step 1: Target Identification
```
Find FlexiPaneItem to remove → Find direct parent FlexiPaneContainer
```

### Step 2: Determine Sibling Element
```
FlexiPaneContainer
├── FirstChild: FlexiPaneItem(target) → sibling is SecondChild
└── SecondChild: FlexiPaneItem(sibling)

or

FlexiPaneContainer
├── FirstChild: FlexiPaneItem(sibling)
└── SecondChild: FlexiPaneItem(target) → sibling is FirstChild
```

### Step 3: Check Parent's Parent (GrandParent)
```
GrandParent (FlexiPaneContainer or FlexiPanel)
└── Child: FlexiPaneContainer(parent)
    ├── FirstChild: FlexiPaneItem(target)
    └── SecondChild: FlexiPaneItem(sibling) → Promote this sibling to GrandParent
```

### Step 4: Execute Structure Change
1. **Detach sibling element from parent container**
2. **Replace parent container with sibling element in GrandParent**
3. **Clean up parent container and removal target**

### Step 5: Empty Container Cleanup
- Remove empty FlexiPaneContainer
- Clean up unnecessary intermediate layers
- Simplify structure of containers with single child

## Special Case Handling

### RootContent Handling
```
When FlexiPanel.RootContent is FlexiPaneContainer:
- If only one child remains, set that child directly as RootContent
- If FlexiPaneItem, restore to single panel state
- If FlexiPaneContainer, maintain hierarchical structure
```

### Empty Container Identification
```
FlexiPaneContainer is empty when:
- FirstChild == null && SecondChild == null

Handling:
- Remove from parent
- Clean up resources
```

### Single Child Container Promotion
```
When FlexiPaneContainer has only one child:
- (FirstChild != null && SecondChild == null) or
- (FirstChild == null && SecondChild != null)

Handling:
- Promote the only child to parent level
- Remove current container
```

## Split Direction and Properties

### Split Direction
- **IsVerticalSplit = true**: Vertical split (left-right)
- **IsVerticalSplit = false**: Horizontal split (top-bottom)

### Split Ratio
- **SplitRatio**: First child size ratio in range 0.1 ~ 0.9

### Hierarchical Split Information
```
FlexiPaneContainer(Root, Vertical, 0.6)
├── FirstChild: FlexiPaneItem(A) [60% width]
└── SecondChild: FlexiPaneContainer(Nested, Horizontal, 0.4)
    ├── FirstChild: FlexiPaneItem(B) [40% height]
    └── SecondChild: FlexiPaneItem(C) [60% height]
```

## Implementation Considerations

### Event Propagation
- FlexiPaneItem → FlexiPanel Routed Event bubbling
- State inheritance through Attached Properties

### Resource Management
- Disconnect event handlers of removed FlexiPaneItem
- Memory cleanup of empty FlexiPaneContainer
- Visual Tree and Logical Tree synchronization

### Validation Logic
- Maintain minimum 1 panel (prevent last panel removal)
- Parent-child relationship integrity validation
- Circular reference prevention

Based on this design, structural changes should be predictable and consistent.