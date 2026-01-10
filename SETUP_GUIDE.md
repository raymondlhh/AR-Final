# Mini-Game Setup Guide - Step by Step

This guide will walk you through setting up the pig character mini-game with animations and SFX.

## Prerequisites
- Unity scene `MiniGameScene` with zones already created
- Player GameObject (pig character) with CharacterController
- All material GameObjects placed in the scene (initially hidden)

---

## Step 1: Add Scripts to Player GameObject

1. **Select your Player GameObject** (the pig character) in the hierarchy
2. **Add Components:**
   - `PlayerController` (Assets/SC_Assets/SC_Scripts/PlayerController.cs)
   - `PlayerInteractionController` (Assets/SC_Assets/SC_Scripts/PlayerInteractionController.cs)

---

## Step 2: Configure PlayerController

### 2.1 Basic Setup
1. Select the Player GameObject
2. In the **PlayerController** component:
   - **Movement Joystick**: Drag your joystick UI element here
   - **Player Speed**: Adjust as needed (default: 2)
   - **Gravity**: -9.8 (standard)

### 2.2 Animation Setup
1. **Pig Animator**: 
   - If your pig has an Animator component, it will auto-detect
   - Otherwise, drag the Animator component here
   - Make sure your Animator Controller has **"Idle"** and **"Walk"** triggers

2. **Audio Source** (for walking SFX):
   - If not already present, an AudioSource will be auto-created
   - Or drag an existing AudioSource here

3. **Walking SFX**:
   - Drag your walking sound effect AudioClip here
   - This will loop automatically while the pig is moving

---

## Step 3: Configure PlayerInteractionController

### 3.1 Zone Detection Setup

**Option A: Using Rigidbody (Recommended)**
1. Select Player GameObject
2. Add **Rigidbody** component
3. Check **Is Kinematic** = true
4. Leave **Use Gravity** = false (CharacterController handles gravity)

**Option B: Using Child Trigger Collider (Alternative)**
1. Create an empty child GameObject under the Player
2. Name it "PlayerDetectionZone"
3. Add **BoxCollider** component
4. Set **Is Trigger** = true
5. Adjust size to match player bounds
6. Add **ZoneDetectionHelper** script to this child GameObject

### 3.2 Action Button Setup
1. In **PlayerInteractionController**:
   - **Action Button**: Drag your UI Button here
   - The button's OnClick will automatically connect

### 3.3 Material Assignment
**Important**: All materials must be assigned in the Inspector arrays in the correct order!

**Paired Processing System:**
- Raw materials are processed in pairs: Raw 0&1 → Processed 0, Raw 2&3 → Processed 1, etc.
- Processed materials are built in pairs: Processed 0&1 → Build 0, Processed 2&3 → Build 1, etc.
- **The order matters!** Assign materials in order (0, 1, 2, 3...) so pairs work correctly.

1. **Raw Materials** (16 total):
   - Expand the **Raw Materials** array
   - Set Size to 16
   - Drag each Raw_Material GameObject from the Processing Zone into slots 0-15 **in order**
   - Slot 0 = Raw Material 1, Slot 1 = Raw Material 2, etc.
   - Materials should be placed at the Processing Zone but initially hidden
   - **Pairs**: (0,1), (2,3), (4,5), (6,7), (8,9), (10,11), (12,13), (14,15)

2. **Processed Materials** (8 total):
   - Expand the **Processed Materials** array
   - Set Size to 8
   - Drag each Processed_Material GameObject from the Processing Zone into slots 0-7 **in order**
   - Slot 0 = Processed Material 1 (made from Raw 0&1), Slot 1 = Processed Material 2 (made from Raw 2&3), etc.
   - **Pairs**: (0,1), (2,3), (4,5), (6,7) → These will create Build materials

3. **Build Materials** (8 total):
   - Expand the **Build Materials** array
   - Set Size to 8
   - Drag each Build_Material GameObject from the Building Zone into slots 0-7 **in order**
   - Slot 0 = Build Material 1 (made from Processed 0&1), Slot 1 = Build Material 2 (made from Processed 2&3), etc.

4. **Final Objects**:
   - **House**: Drag the house GameObject here
   - **Base Object**: Drag the base GameObject that should be hidden when house appears

**Example Flow:**
- Collect: Raw 0, Raw 1, Raw 2, Raw 3... (any order)
- Process: Raw 0&1 → Processed 0, then Raw 2&3 → Processed 1, etc. (must be pairs)
- Build: Processed 0&1 → Build 0, then Processed 2&3 → Build 1, etc. (must be pairs)

### 3.4 Animation Setup
1. **Pig Animator**: 
   - Same as PlayerController (can share the same Animator)
   - Make sure your Animator Controller has:
     - **"Eat"** trigger (used for valid actions)
     - **"Damaged"** trigger (used for invalid actions)
   - *(Optional: You can later add custom "ActionValid" and "ActionInvalid" triggers)*

2. **Audio Source** (for action SFX):
   - Can share the same AudioSource as PlayerController (PlayOneShot won't interrupt looping audio)
   - Or use a separate AudioSource if preferred

### 3.5 SFX Assignment
Assign all 5 AudioClips:

1. **Collect Valid SFX**: Sound when successfully collecting raw material
2. **Process Valid SFX**: Sound when successfully processing (2 raw → 1 processed)
3. **Build Valid SFX**: Sound when successfully building (2 processed → 1 build)
4. **Action Invalid SFX**: Shared sound for ALL invalid actions (collect/process/build when action cannot be performed)
5. **House Complete SFX**: Sound when house is fully built

**Important**: All invalid actions (collect invalid, process invalid, build invalid) share the same `Action Invalid SFX`.

---

## Step 4: Verify Scene Setup

### 4.1 Check Zones
Ensure all three zones exist with trigger colliders:
- **Collecting_Zone** (parent) with child **PlayerDetectZone** (BoxCollider, Is Trigger = true)
- **Processing_Zone** (parent) with child **PlayerDetectZone** (BoxCollider, Is Trigger = true)
- **Building_Zone** (parent) with child **PlayerDetectZone** (BoxCollider, Is Trigger = true)

### 4.2 Check Materials Location
- **Raw Materials**: Should be placed at Processing Zone (but initially hidden)
- **Processed Materials**: Should be placed at Processing Zone (but initially hidden)
- **Build Materials**: Should be placed at Building Zone (but initially hidden)
- **House**: Should be placed at Building Zone (but initially hidden)
- **Base Object**: Should be visible at Building Zone

### 4.3 Initial State Check
- All materials should be **active** in the scene hierarchy
- The scripts will automatically hide them at Start()
- House should be **active** but will be hidden at Start()
- Base object should be **active** and visible

---

## Step 5: Test the System

1. **Play the scene**
2. **Test Movement:**
   - Use joystick to move → Pig should play Walk animation and Walking SFX
   - Stop moving → Pig should play Idle animation and Walking SFX stops

3. **Test Collecting Zone:**
   - Move pig to Collecting Zone
   - Press Action Button → Should see raw material 1 appear at Processing Zone
   - Pig should play "Eat" animation and Collect Valid SFX
   - Press again → Raw material 2 appears, then 3, 4, etc.
   - Repeat until 16 materials collected (materials appear in order 1-16)
   - Press Action Button after 16 → Should play "Damaged" animation and Action Invalid SFX

4. **Test Processing Zone (Paired Processing):**
   - Move pig to Processing Zone
   - Make sure you have at least 2 raw materials visible (raw materials 1 & 2)
   - Press Action Button → Should hide raw materials 1 & 2, show processed material 1
   - Pig should play "Eat" animation and Process Valid SFX
   - **Important**: Materials are processed in pairs: Raw 1&2 → Processed 1, Raw 3&4 → Processed 2, etc.
   - Press Action Button without a complete pair → Should play "Damaged" animation and Action Invalid SFX

5. **Test Building Zone (Paired Building):**
   - Move pig to Building Zone
   - Make sure you have at least 2 processed materials visible (processed materials 1 & 2)
   - Press Action Button → Should hide processed materials 1 & 2, show build material 1
   - Pig should play "Eat" animation and Build Valid SFX
   - **Important**: Materials are built in pairs: Processed 1&2 → Build 1, Processed 3&4 → Build 2, etc.
   - Continue until 8 build materials (4 presses total: pairs 1&2, 3&4, 5&6, 7&8)
   - After 8th build material → House should appear, build materials + base hidden
   - Should play House Complete SFX

---

## Troubleshooting

### Zone Detection Not Working
- **Solution 1**: Ensure Player has Rigidbody (IsKinematic = true)
- **Solution 2**: Use ZoneDetectionHelper on child GameObject with trigger collider
- **Check**: PlayerDetectZone colliders are set as triggers
- **Check**: Player GameObject has a collider (CharacterController or separate)

### Animations Not Playing
- **Check**: Animator Controller has "Idle", "Walk", "Eat", "Damaged" triggers
- **Check**: Pig Animator field is assigned in both scripts
- **Check**: Animator Controller is assigned to the Animator component

### SFX Not Playing
- **Check**: AudioSource is assigned
- **Check**: AudioClips are assigned in Inspector
- **Check**: AudioSource volume is not muted
- **Check**: AudioListener exists in scene (usually on Main Camera)

### Materials Not Showing/Hiding
- **Check**: All material GameObjects are assigned in the arrays
- **Check**: Materials are active in hierarchy (scripts will handle visibility)
- **Check**: Console for debug logs to see what's happening

### Action Button Not Working
- **Check**: Action Button is assigned in Inspector
- **Check**: Button has OnClick event (should be auto-connected)
- **Alternative**: Use keyboard Space or E key for testing

---

## Optional: Custom Animation Triggers

If you want to use custom animation triggers instead of "Eat" and "Damaged":

1. Open your Animator Controller
2. Add new Trigger parameters:
   - "ActionValid"
   - "ActionInvalid"
3. Create animation states or transitions for these triggers
4. In **PlayerInteractionController.cs**, change:
   - `s_Eat` to `Animator.StringToHash("ActionValid")`
   - `s_Damaged` to `Animator.StringToHash("ActionInvalid")`

---

## Summary Checklist

- [ ] PlayerController added to Player GameObject
- [ ] PlayerInteractionController added to Player GameObject
- [ ] Joystick assigned in PlayerController
- [ ] Animator assigned (or auto-detected)
- [ ] Walking SFX assigned
- [ ] Rigidbody added to Player (IsKinematic = true) OR ZoneDetectionHelper setup
- [ ] Action Button assigned
- [ ] All 16 Raw Materials assigned in array
- [ ] All 8 Processed Materials assigned in array
- [ ] All 8 Build Materials assigned in array
- [ ] House GameObject assigned
- [ ] Base Object assigned
- [ ] All 5 SFX AudioClips assigned (Collect Valid, Process Valid, Build Valid, Action Invalid, House Complete)
- [ ] Zones have PlayerDetectZone trigger colliders
- [ ] Tested movement (walk/idle animations and SFX)
- [ ] Tested all three zones and their interactions

---

**Good luck with your AR storybook project!** 🐷🏠

