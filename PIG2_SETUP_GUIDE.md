# Pig 2 Setup Guide

## Overview
Pig 2 uses a different mini-game system with progressive height mechanics and flexible cycles.

---

## Step 1: Script Setup

### 1.1 Pig 1 (Keep Existing)
- **Keep using `PlayerInteractionController`** on Pig 1 GameObject (or rename to `Pig1InteractionController` if you prefer)
- No changes needed - it already works!

### 1.2 Pig 2 (New Setup)
1. **Select Pig 2 GameObject** in the hierarchy
2. **Add Components:**
   - `PlayerController` (same as Pig 1 - handles movement, animation, SFX)
   - `Pig2InteractionController` (NEW - handles Pig 2's specific material logic)

---

## Step 2: Configure Pig2InteractionController

### 2.1 Basic Setup
1. **Action Button**: Drag your UI Button here (same button as Pig 1)
2. **Pig Animator**: Drag the Animator component (same as Pig 1)
3. **Audio Source**: Can share with PlayerController or use separate
4. **SFX AudioClips**: Assign all 5 SFX (same as Pig 1)

### 2.2 Material Assignment - AUTO-ASSIGN METHOD (EASIEST!)

**Option A: Auto-Assign from Parent GameObjects (Recommended)**

1. **Organize Materials in Hierarchy:**
   - Create parent GameObjects (or use existing ones):
     - `RawMaterials_Pig2` (contains all 6 raw materials)
     - `ProcessedMaterials_Pig2` (contains all 36 processed materials)
     - `BuildMaterials_Pig2` (contains all 27 build materials)

2. **In Pig2InteractionController Inspector:**
   - **Raw Materials Parent**: Drag the parent GameObject containing raw materials
   - **Processed Materials Parent**: Drag the parent GameObject containing processed materials
   - **Build Materials Parent**: Drag the parent GameObject containing build materials
   - **Auto Assign Materials**: ✓ Check this checkbox
   - Materials will auto-populate! (Checkbox will uncheck automatically)

3. **Verify:**
   - Check that arrays are filled correctly
   - Materials should be sorted by name automatically

**Option B: Manual Assignment (If auto-assign doesn't work)**
- Expand each array in Inspector
- Drag materials manually (6 raw, 36 processed, 27 build)

### 2.3 Height Settings
- **Height One Third**: 0.02 (default)
- **Height Two Thirds**: 0.04 (default)
- **Height Full**: 0.06 (default)
- **Use Scale For Height**: false (uses Y position - recommended)
- **Original Raw Material Scale Y**: 1.0 (only used if useScaleForHeight = true)

### 2.4 Final Objects
- **House**: Drag house GameObject
- **Base Object**: Drag base GameObject

---

## Step 3: Configure MultiImageTargetManager

### 3.1 Add Manager Script
1. Create an empty GameObject in scene (name it "PigManager" or "ImageTargetManager")
2. Add `MultiImageTargetManager` component

### 3.2 Assign References
1. **Image Target References:**
   - **Image Target Pig 1**: Drag the ImageTarget GameObject for pig1build
   - **Image Target Pig 2**: Drag the ImageTarget GameObject for pig2build
   - **Image Target Pig 3**: Drag the ImageTarget GameObject for pig3build (if exists)

2. **Pig GameObjects:**
   - **Pig 1**: Drag Pig 1 GameObject
   - **Pig 2**: Drag Pig 2 GameObject
   - **Pig 3**: Drag Pig 3 GameObject (if exists)

3. **UI References:**
   - **Joystick**: Drag your joystick UI element
   - **Action Button**: Drag your action button UI element

### 3.3 Setup Image Target Tracking (Choose One Method)

**Method 1: UnityEvents (Recommended - Most Reliable)**

1. For each ImageTarget GameObject (pig1build, pig2build, pig3build):
   - Find the `DefaultObserverEventHandler` component
   - In Inspector, expand **OnTargetFound** event
   - Click **+** to add listener
   - Drag the **MultiImageTargetManager** GameObject to the object field
   - Select method:
     - For pig1build → `MultiImageTargetManager.OnPig1TargetFound()`
     - For pig2build → `MultiImageTargetManager.OnPig2TargetFound()`
     - For pig3build → `MultiImageTargetManager.OnPig3TargetFound()`

2. Repeat for **OnTargetLost** event:
   - For pig1build → `MultiImageTargetManager.OnPig1TargetLost()`
   - For pig2build → `MultiImageTargetManager.OnPig2TargetLost()`
   - For pig3build → `MultiImageTargetManager.OnPig3TargetLost()`

**Method 2: Automatic Fallback (Works but less reliable)**
- The script will automatically detect active image targets in Update()
- No manual setup needed, but UnityEvents are more reliable

---

## Step 4: Material Organization Tips

### For Auto-Assignment to Work:
- **Naming Convention**: Materials should have names containing:
  - `Raw_Material` (for raw materials)
  - `Processed_Material` (for processed materials)
  - `Build_Material` (for build materials)

- **Examples of Good Names:**
  - `Raw_Material (1)`, `Raw_Material (2)`, etc.
  - `Processed_Material (1)`, `Processed_Material (2)`, etc.
  - `Build_Material (1)`, `Build_Material (2)`, etc.

- **Parent Structure:**
  ```
  Processing_Zone
    └── RawMaterials_Pig2 (or any parent name)
        ├── Raw_Material (1)
        ├── Raw_Material (2)
        └── ...
    └── ProcessedMaterials_Pig2 (or any parent name)
        ├── Processed_Material (1)
        ├── Processed_Material (2)
        └── ...
  Building_Zone
    └── BuildMaterials_Pig2 (or any parent name)
        ├── Build_Material (1)
        ├── Build_Material (2)
        └── ...
  ```

---

## Step 5: Verify Setup

### Checklist:
- [ ] Pig 1 has `PlayerInteractionController` (or `Pig1InteractionController`)
- [ ] Pig 2 has `PlayerController` + `Pig2InteractionController`
- [ ] All materials auto-assigned (or manually assigned)
- [ ] MultiImageTargetManager configured
- [ ] Image target UnityEvents connected (or using fallback)
- [ ] Joystick and Action Button assigned in Manager
- [ ] Height settings configured (defaults should work)
- [ ] House and Base Object assigned

---

## Pig 2 Game Flow Summary:

### Collecting Zone:
- Press action button → Raw material grows: 1/3 → 2/3 → full (3 presses per raw)
- Total: 6 raw materials × 3 presses = 18 presses to complete all raw materials

### Processing Zone:
- Press action button → Raw material shrinks: full → 2/3 → 1/3 → hidden (3 presses)
- Each press creates 1 processed material (shown from array index 0 onwards)
- Total: 1 raw → 3 processed materials

### Building Zone:
- Press action button → Consumes 4 processed materials (from END of array: 32-35, then 28-31, etc.)
- Creates 1 build material (shown sequentially: Build 1, Build 2... Build 27)
- Total: 4 processed → 1 build material

### Flexible Cycles:
- Can collect/process multiple times before building
- Max 36 processed materials visible at once
- Goal: Get all 27 build materials visible

---

## Troubleshooting:

### Auto-Assignment Not Working:
- Check material names contain "Raw_Material", "Processed_Material", or "Build_Material"
- Ensure parent GameObjects are assigned
- Check Console for debug messages
- Manually assign if needed

### Image Target Switching Not Working:
- Check UnityEvents are connected in Inspector
- Verify MultiImageTargetManager has all references assigned
- Check Console for debug messages
- Try the fallback method (automatic detection)

### Height Not Changing:
- Check `useScaleForHeight` setting (should be false for position-based)
- Verify height values (0.02, 0.04, 0.06)
- Check if materials have correct Transform components

---

**Good luck with Pig 2 setup!** 🐷🏗️
