# ⚔️ KALKI: NEON DHARMA — Player Movement & Combat System (Person 2)

This module provides the complete core player gameplay and combat mechanics for **KALKI: NEON DHARMA** in Unity (C#).

---

## 📁 Included Scripts & Architecture

| Script | Purpose & Key Features |
| :--- | :--- |
| **`PlayerMovement.cs`** | Restricts movement to the 2.5D fighting plane (X/Y axis, locked Z). Supports left/right movement via **A/D** or arrow keys, snappy jumping with **Space**, ground detection, coyote time, jump buffering, and smooth turning. |
| **`PlayerCombat.cs`** | Basic Attack (**J** / **Left Click**) and special Power Strike (**K**). Anti-spam attack cooldowns, `Physics.OverlapSphere` hit detection against MAYA, damage & knockback calculation, audio/VFX hooks, and scene gizmos. |
| **`Health.cs`** | Universal health component for Kalki & MAYA. Handles `TakeDamage()`, invulnerability frames (i-frames) to prevent repeated damage from one swing, visual flash, knockback resistance, and death notification. |
| **`IDamageable.cs`** | Unified interface for any entity capable of taking damage in the game. |
| **`HitFeedback.cs`** | Flashes character materials neon-red/white upon receiving damage and plays hit sound effects. |
| **`CameraShake.cs`** | Impactful screen shake triggered during combat and Power Strikes for a juicy fighting game feel. |
| **`GameManager.cs`** | Singleton handling round states (*Playing*, *GameOver*, *Victory*), listens for character deaths, and supports pressing **R** to restart. |
| **`HealthBarUI.cs`** | Smoothly interpolating UI component for Health Sliders and neon fill bars. |

---

## 🎮 Controls Reference

| Action | Primary Input | Alternative Input | Details |
| :--- | :--- | :--- | :--- |
| **Move Left / Right** | `A` / `D` | `Left` / `Right` Arrow | Responsive acceleration along 2.5D X-axis |
| **Jump** | `Space` | Joystick Button 0 | Ground-checked, coyote time & jump buffer |
| **Basic Attack** | `J` | `Left Mouse Click` | Fast sword slash, 15 damage, cooldown |
| **Power Strike** | `K` | — | Heavy strike, 40 damage, screen shake & knockback |
| **Restart Match** | `R` | — | Instant scene reload via `GameManager` |

---

## 🛠️ Step-by-Step Unity Setup Guide

### 1. Setting up Kalki (The Player)
1. In your Unity Scene, select your Kalki character GameObject (or import model).
2. Set its **Tag** to `Player`.
3. Add the following components in the Inspector:
   - **`Rigidbody`**:
     - Mass: `1`
     - Interpolate: `Interpolate`
     - Collision Detection: `Continuous Dynamic`
     - Constraints: Freeze Position **Z**, Freeze Rotation **X**, **Y**, **Z** *(PlayerMovement automatically configures this as well)*.
   - **`CapsuleCollider`**: Adjust height and center to enclose Kalki's body.
   - **`PlayerMovement`**:
     - *Move Speed*: `8`
     - *Jump Force*: `12`
     - *Ground Check Point*: Drag an empty child object positioned at Kalki's feet (or leave null for automatic capsule detection).
     - *Ground Layer*: Select your environment/floor layer.
     - *Visual Model*: Drag the visual/mesh child object that should rotate when facing left/right.
   - **`PlayerCombat`**:
     - *Basic Attack Damage*: `15`
     - *Basic Attack Cooldown*: `0.35`
     - *Power Strike Damage*: `40`
     - *Power Strike Cooldown*: `3.0`
     - *Target Tags*: Set size `2` (`MAYA`, `Enemy`).
     - *Attack Point*: (Optional) Create an empty child object in front of Kalki for the slash center.
   - **`Health`**:
     - *Max Health*: `100`
     - *Invulnerability Duration*: `0.5`
   - **`HitFeedback`**: (Optional) For neon damage flashes.

---

### 2. Setting up MAYA (The Boss / Enemy)
1. Select MAYA's GameObject in the Hierarchy.
2. Set its **Tag** to `MAYA` (or `Enemy`).
3. Add:
   - **`CapsuleCollider`** (Solid collider).
   - **`Rigidbody`** (Kinematic or non-kinematic with Freeze Rotation).
   - **`Health`**:
     - *Max Health*: `150` (or your desired boss HP).
     - *Invulnerability Duration*: `0.4` (Prevents multi-hit exploit from a single sword swing).
   - **`HitFeedback`**: Flashes red when Kalki strikes her.

---

### 3. Setting up the Camera & GameManager
1. Select your **Main Camera**:
   - Add the **`CameraShake`** script.
2. Create an empty GameObject named **`GameManager`**:
   - Add the **`GameManager`** script.
   - (Optional) Assign Game Over and Victory UI panels.

---

## 🎭 Animator Controller Setup

In your Animator Controller for Kalki (and MAYA), configure the following parameters:

| Parameter Name | Type | Used By | Description |
| :--- | :--- | :--- | :--- |
| `Speed` | **Float** | `PlayerMovement` | `0.0` for Idle, `> 0.1` for Run transition. |
| `IsGrounded` | **Bool** | `PlayerMovement` | `true` when grounded, `false` while airborne. |
| `VerticalVelocity` | **Float** | `PlayerMovement` | `> 0` ascending, `< 0` falling. |
| `Jump` | **Trigger** | `PlayerMovement` | Fired once when takeoff impulse occurs. |
| `Attack` | **Trigger** | `PlayerCombat` | Fired when J or LMB basic attack is pressed. |
| `PowerStrike` | **Trigger** | `PlayerCombat` | Fired when K heavy strike is pressed. |
| `Hit` | **Trigger** | `Health` | Fired whenever receiving damage. |
| `Die` | **Trigger** | `Health` | Fired when health drops to 0. |

---

## 🛡️ Anti-Spam & Damage Verification Features
1. **Attack Cooldown Timer**: `PlayerCombat` enforces `nextBasicAttackTime` and `nextPowerStrikeTime` preventing button mashing.
2. **Per-Swing Hash Filtering**: `HashSet<IDamageable>` ensures a single sword swing collider only hits each enemy once.
3. **Invulnerability Frames (i-Frames)**: `Health` provides `isInvulnerable` and a configurable flicker coroutine so enemies cannot be hit multiple times during the same frame sequence.
4. **Plane Locking**: `FixedUpdate` constantly locks `Z` position and velocity, ensuring Kalki stays strictly on the 2.5D combat axis.
