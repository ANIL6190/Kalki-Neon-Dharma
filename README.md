# ⚔️ KALKI: NEON DHARMA — Master Gameplay & Adaptive Storytelling System

Welcome to the unified core codebase for **KALKI: NEON DHARMA** in Unity (C#).

This repository contains the complete architecture for:
1. **Playable Core**: Character movement, 3D third-person controls, camera collision avoidance, combat mechanics, and damage systems.
2. **Training & Mission Loop**: Target calibration trials, progress objectives, timer limits, and mission state management.
3. **Adaptive Storytelling 🧠**: Dynamic narration and dialogue variations reflecting player performance, skill, retries, and speed.
4. **Research & AI Integration**: Structured telemetry data containers, CSV/JSON logging, and AI multi-feature scoring evaluators.

---

## 🏛️ System Architecture & Communication Flow

```text
                             +------------------------+
                             |   GAME FLOW MANAGER    |
                             +-----------+------------+
                                         |
               +-------------------------+-------------------------+
               |                                                   |
               v                                                   v
    +----------------------+                            +----------------------+
    |  KALKI STORY MANAGER |                            |   TRAINING MISSION   |
    |  - Subtitles & Audio |                            |  - Objective Targets |
    |  - Cinematic Blends  |                            |  - Timers & Progress |
    +----------+-----------+                            +----------+-----------+
               |                                                   |
               +-------------------------+-------------------------+
                                         |
                                         v
                             +------------------------+
                             |  PERFORMANCE TRACKER   |
                             |  - Speed, Accuracy, HP |
                             |  - Attempts & Failures |
                             +-----------+------------+
                                         |
                                         v
                             +------------------------+
                             | ADAPTIVE STORY DIRECTOR|
                             |  - Rule / AI Selection |
                             +-----------+------------+
                                         |
                                         v
                             +------------------------+
                             |    GAME UI MANAGER     |
                             |  - Subtitles & Banners |
                             |  - Performance Summary |
                             +------------------------+
```

---

## 📁 Complete Script Manifest

### Phase 1 — Playable Systems & Calibration
| Script | Class Name | Responsibility | Validation in Unity |
| :--- | :--- | :--- | :--- |
| **`KalkiController.cs`** | `KalkiController` | 3D third-person movement, sprint, jumping, camera-relative facing, ground check, Animator sync. | Smooth WASD movement, sprint with Shift, space jump, locked during cutscenes. |
| **`KalkiCameraController.cs`** | `KalkiCameraController` | Third-person follow camera with obstacle & ground collision avoidance (`SphereCast`), mouse orbit, and cinematic shot blending. | Smooth orbit without geometry clipping; smooth blend to cinematic vantage points. |
| **`KalkiStoryManager.cs`** | `KalkiStoryManager` | Opening cinematic cutscene, subtitle queue, audio voiceover playback, and player control lockout. | Plays intro sequence on launch, updates subtitles, frees player on completion. |
| **`TrainingMissionManager.cs`** | `TrainingMissionManager` | Objective tracking (dummy elimination), countdown timer, mission success and failure management. | Tracks target kills; triggers victory banner and adaptive evaluation on completion. |
| **`TrainingTarget.cs`** | `TrainingTarget` | Combat dummy implementing `IDamageable`. Features neon hit flash, wobble punch scale, sound, and destruction VFX. | Struck by basic attack (J) or power strike (K); reports damage & destruction events. |
| **`GameUIManager.cs`** | `GameUIManager` | Central HUD manager for objective progress, live timer, subtitles, animated banners, and performance summary cards. | Subtitles display cleanly; objective counters update; results card displays after trials. |

### Phase 2 — Adaptive Storytelling 🧠
| Script | Class Name | Responsibility | Key Features |
| :--- | :--- | :--- | :--- |
| **`PlayerPerformanceTracker.cs`** | `PlayerPerformanceTracker` | Telemetry tracking: completion time, attempt count, hits landed vs. swings (accuracy), damage taken. | Computes player skill tier: **Mastery**, **Resilient**, or **Struggling**. |
| **`AdaptiveStoryDirector.cs`** | `AdaptiveStoryDirector` | Selects dynamic narration and mentor dialogue based on player performance metrics. | Connects to `DialogueData` and dispatches dialogue to `KalkiStoryManager` and `GameUIManager`. |
| **`DialogueData.cs`** | `DialogueData` | ScriptableObject defining dialogue lines, voice clips, speaker names, and performance condition branches. | Easily customizable directly in the Unity Inspector (`Create -> Kalki -> Dialogue Data`). |
| **`GameFlowManager.cs`** | `GameFlowManager` | High-level state machine: Cinematic Intro -> Training Mission -> Story Evaluation -> Next Phase / Retry. | Prevents duplicate cutscene triggers; supports [Enter] to continue or [R] to retry. |

### Phase 3 — AI & Research Features
| Script | Class Name | Responsibility | Key Features |
| :--- | :--- | :--- | :--- |
| **`TrainingMetrics.cs`** | `TrainingMetrics` | Structured telemetry data container. | Supports `ToJson()`, CSV row export, and auto-logging to `Application.persistentDataPath`. |
| **`AIStoryEvaluator.cs`** | `AIStoryEvaluator` | Weighted multi-feature composite scoring model (speed, accuracy, health, attempts). | Recommends narrative outcome; supports research comparisons (Fixed Baseline vs. Adaptive). |

### Core Combat & 2.5D Modules (Pre-Existing & Integrated)
| Script | Class Name | Purpose |
| :--- | :--- | :--- |
| **`PlayerCombat.cs`** | `PlayerCombat` | Basic Attack (**J** / LMB) and Power Strike (**K**). Telemetry hooks automatically notify `PlayerPerformanceTracker`. |
| **`PlayerMovement.cs`** | `PlayerMovement` | 2.5D locked plane combat movement (X/Y axis, locked Z). |
| **`Health.cs`** | `Health` | Health management, i-frames, death triggers. Reports player damage to `PlayerPerformanceTracker`. |
| **`IDamageable.cs`** | `IDamageable` | Universal damage interface implemented by `Health` and `TrainingTarget`. |
| **`CameraShake.cs`** | `CameraShake` | Combat impact screen shake. |
| **`HitFeedback.cs`** | `HitFeedback` | Material flash and hit audio feedback. |
| **`GameManager.cs`** | `GameManager` | Global round manager and scene reloader. |
| **`HealthBarUI.cs`** | `HealthBarUI` | Neon UI health bar slider. |

---

## 🎭 Dynamic Story Response Matrix

| Player Performance | Assigned Tier | Narrative Mentor Response |
| :--- | :--- | :--- |
| **Fast Completion (<25s), High Accuracy, <=2 Attempts** | `Mastery` | *"Your strength awakens. Destiny calls."* |
| **Moderate Speed, Steady Strikes, Multiple Attempts** | `Resilient` | *"A true warrior learns from every failure."* |
| **Slow Completion, High Damage Taken, Repeated Retries** | `Struggling` | *"Rise again, Kalki. Your journey has only begun."* |

---

## 🎮 Controls Reference

| Action | Primary Key | Alternate Input | Purpose |
| :--- | :--- | :--- | :--- |
| **Move (3D / 2.5D)** | `W / A / S / D` | Arrow Keys | Third-person or 2.5D movement |
| **Sprint** | `Left Shift` | `Right Shift` | Faster movement speed in 3D exploration |
| **Jump** | `Space` | Joystick Button 0 | Ground-checked jump with coyote time |
| **Orbit Camera** | `Mouse Movement` | — | Smooth camera orbit around Kalki |
| **Toggle Cursor Lock** | `Escape` | — | Unlock mouse for UI interaction |
| **Basic Attack** | `J` | `Left Mouse Click` | Fast sword strike (15 DMG) |
| **Power Strike** | `K` | — | Heavy cybernetic strike (40 DMG) |
| **Skip Cinematic** | `Enter` / `Return` | `Escape` | Skip active cutscene sequence |
| **Advance After Mission**| `Enter` / `Return` | `Space` | Confirm results and advance to next phase |
| **Restart / Retry Mission**| `R` | — | Instant retry of calibration trial |

---

## 🛠️ Step-by-Step Unity Hierarchy Setup Guide

### 1. The Player (`Kalki`)
1. Create or select your Kalki character GameObject in the scene.
2. Tag: `Player`.
3. Add **`CharacterController`** (Height: 2.0, Radius: 0.4, Center: 0, 1.0, 0).
4. Add **`KalkiController`**:
   - *Camera Transform*: Drag Main Camera.
   - *Visual Root*: Drag the child mesh/visual GameObject.
   - *Animator*: Assign player Animator Controller.
5. Add **`PlayerCombat`**:
   - *Target Tags*: Ensure `MAYA`, `Enemy`, and `TrainingTarget` are present.
6. Add **`Health`**:
   - *Max Health*: 100.

### 2. The Camera (`Main Camera`)
1. Select **`Main Camera`**.
2. Add **`KalkiCameraController`**:
   - *Target*: Drag Kalki (or an empty child at head height).
   - *Collision Layers*: Select `Default`, `Ground`, `Environment` (exclude `Player` layer!).
3. Add **`CameraShake`**.

### 3. The Mission Targets (`Training Dummies`)
1. Create a 3D model (e.g. cylinder or combat dummy model).
2. Tag: `Enemy`.
3. Add a **`Collider`** (Capsule or Box).
4. Add **`TrainingTarget`**:
   - *Max Health*: 50.
   - *Target Renderer*: Drag target mesh renderer.
   - *Flash Color*: Neon Cyan (`#00FFFF`).
   - *Hit VFX & Destroy VFX*: Assign optional particle prefabs.
5. Duplicate 3 to 5 targets across the training arena.

### 4. Game Managers & Flow
Create an empty GameObject named **`=== MANAGERS ===`** with the following children or components:
1. **`GameFlowManager`**:
   - Coordinates scene flow from intro cutscene to mission completion.
2. **`KalkiStoryManager`**:
   - Manages opening cinematic and subtitle queues.
   - Assign an `AudioSource` for voiceover narration.
3. **`TrainingMissionManager`**:
   - Objective title: "CALIBRATION TRIAL: TARGET ELIMINATION".
   - *Time Limit Seconds*: 60.
4. **`PlayerPerformanceTracker`**:
   - Records telemetry (time, accuracy, attempts, damage).
5. **`AdaptiveStoryDirector`**:
   - Assign `DialogueData` ScriptableObject asset.
   - Check *Log Telemetry To Disk* for CSV research dataset output.
6. **`AIStoryEvaluator`**:
   - Multi-feature weighted scoring model for adaptive refinement.

### 5. UI Canvas (`HUD_Canvas`)
1. Create a Screen-Space Overlay Canvas named `HUD_Canvas`.
2. Add **`GameUIManager`**:
   - Link Subtitle Panel, Speaker Text, Subtitle Text.
   - Link Objective Text and Timer Text.
   - Link Banner Panel (Title, Subtitle, Background).
   - Link Summary Panel (Title, Story Text, Stats Text, Prompt Text).

---

## 🧪 Unity Validation Checklist

- [x] **Compilation**: All 12 scripts compile cleanly with zero errors or warnings.
- [x] **Runtime Initialization**: `GameFlowManager` initiates the opening cinematic; player movement is locked during the dialogue.
- [x] **Movement & Camera**: Player moves smoothly relative to camera orientation; camera stops before clipping walls or ground.
- [x] **Combat & Hits**: Pressing `J` or `K` strikes targets; targets flash neon and take damage; hit accuracy is logged.
- [x] **Objective Completion**: Destroying all targets ends the mission; victory banner displays.
- [x] **Adaptive Story Reaction**: Mentor voice and subtitle reaction adapts according to whether player was fast (<25s, Mastery) or slower (Resilient/Struggling).
- [x] **Retry Loop**: Pressing `R` instantly resets the targets, resets timer, increments attempt counter, and resumes trial without scene reloads.
