# PROJECT PLAN: Stride3D Survival Game Engine v1.0.0

**PLAN ID:** PLAN-STRIDE-SURV-001
**Date:** 2025-05-23T19:54:00Z
**Version:** 1.0.0

## 1. PROJECT OVERVIEW

### 1.1. Objective
To develop a robust and feature-rich survival game engine using Stride3D (version 4.2.0.2381), providing a foundational platform for creating immersive survival game experiences. The engine will support both single-player and multiplayer modes, with a focus on modularity, performance, and ease of customization.

### 1.2. Requirements

#### 1.2.1. Core Engine Features:
*   **World Building:** Procedural or manual world generation, dynamic weather, day/night cycles.
*   **Player Mechanics:** Health, hunger, thirst, stamina, inventory, crafting, building.
*   **Combat System:** Melee and ranged combat, AI for creatures and NPCs.
*   **Networking:** Support for multiplayer (PvP/PvE).
*   **Persistence:** Saving and loading game state.

#### 1.2.2. Specific Gameplay Features:
*   **Separate PvP/PvE Modes:** Distinct rule sets and server configurations for Player vs. Player and Player vs. Environment gameplay.
*   **Tribe Log System:** A logging system similar to Ark: Survival Evolved, detailing significant tribe-related events (e.g., player joins/leaves, structures destroyed, creatures tamed/killed by tribe members).
*   **Security Camera System:** Placeable in-game cameras that players can use to monitor remote locations.
*   **Screaming NPC:** An NPC character that can detect other NPCs or players within a certain range and emit a vocal "scream" or alert, notifying nearby friendly entities.
*   **Souls-like Melee Combat (FPS/TPS):**
    *   **Stamina-based actions:** Attacks, dodges, blocks consume stamina.
    *   **Lock-on targeting:** Ability to focus on a single enemy.
    *   **Hitbox-based combat:** Precise collision detection for attacks.
    *   **Parry/riposte system:** Defensive maneuvers that can open enemies to counter-attacks.
    *   **Variety of weapon types:** Each with unique move sets and attack animations.
    *   **FPS/TPS views:** Player choice of perspective.
*   **Animation Merging (Mixamo) & Ragdoll Physics:**
    *   Integration of Mixamo animations for player and NPC characters.
    *   Advanced animation merging techniques to blend multiple animations smoothly (e.g., running while aiming).
    *   Physics-based ragdoll effects for characters upon death or significant impact.
    *   Area-based damage system linked to ragdoll physics (e.g., headshots, limb damage affecting animations and character behavior).
    *   Reference Three.js example for ragdoll setup: Implement ragdoll physics similar to the provided three.js example, focusing on realistic joint constraints and impact responses.
*   **[REQ-XXX] Dual Melee System:** Support for standard melee (e.g., Ark-like) and a toggleable "Souls-like" precision melee mode. {PRIORITY:HIGH}

### 1.3. Constraints
*   **Engine:** Stride3D version 4.2.0.2381. Project files must be updated if starting from an older template.
*   **Timeline:** Phased development, aiming for iterative releases.
*   **Team Size:** Primarily individual development with potential for collaboration.
*   **Budget:** Open-source / personal project constraints.

## 2. ARCHITECTURE

### 2.1. Diagram Description
*(A conceptual diagram would be inserted here in a full document. For this text-based plan, it's described.)*

The architecture will be modular, based on Stride's Entity-Component-System (ECS) model.
*   **Core Engine Layer:** Low-level systems (Rendering, Physics, Audio, Input, Networking).
*   **Game Systems Layer:** Manages game logic (World Management, Player State, AI Director, Crafting, Building, Combat). These systems will interact with entities and their components.
*   **Entity & Components Layer:** Player characters, NPCs, creatures, items, world objects, each with specific components defining their behavior and data.
*   **Game API/Modding Layer:** Interfaces for extending or modifying game functionality (optional, future goal).
*   **Presentation Layer:** UI, HUD, visual effects, soundscapes.

### 2.2. Rationale
*   **Modularity (ECS):** Stride's ECS is ideal for managing complex game objects and behaviors, allowing for easy addition, removal, or modification of features.
*   **Scalability:** Clear separation of concerns allows different systems to be developed and optimized independently.
*   **Performance:** Stride's design is performance-oriented. The architecture will leverage this by keeping game logic efficient.

## 3. COMPONENT BREAKDOWN

### 3.1. Hierarchy
*   **Game (Root Scene)**
    *   **GlobalSystems** (Scripts for managing overall game state, time, weather, AI director)
    *   **World** (Terrain, foliage, dynamic objects, environment probes)
    *   **Player Entities**
        *   CharacterControllerComponent
        *   StatsComponent (Health, Hunger, Thirst, Stamina)
        *   InventoryComponent
        *   CraftingComponent
        *   CombatComponent (handles attacks, damage, abilities)
            *   Note: Melee weapon functionality will need to adapt to both standard and Souls-like combat modes.
        *   AnimationComponent (linked to Animation Merging system)
        *   NetworkSyncComponent
        *   InputComponent
            *   Note: Input handling will need to support a toggle for switching between standard melee and Souls-like melee modes.
    *   **NPC/Creature Entities**
        *   AIControllerComponent (Pathfinding, Behavior Trees)
        *   StatsComponent
        *   CombatComponent
        *   AnimationComponent (linked to Animation Merging system)
        *   RagdollComponent
        *   NetworkSyncComponent
        *   ScreamerBehaviorComponent (for the specific NPC type)
    *   **Item Entities** (Weapons, tools, resources, consumables)
        *   ItemComponent (Data: type, stats, stackability)
        *   PickupComponent
        *   RenderComponent
    *   **Structure Entities** (Foundations, walls, crafting stations, security cameras)
        *   StructureComponent (Data: health, owner, type)
        *   BuildableComponent
        *   InteractableComponent (e.g., for camera view, door open/close)
        *   NetworkSyncComponent
    *   **Specialized Systems Entities**
        *   TribeLogSystem (Script managing tribe event data and display)
        *   SecurityCameraSystem (Script managing camera feeds and player interaction)

### 3.2. Component Specifications (Examples)

*   **StatsComponent:**
    *   `MaxHealth: float`
    *   `CurrentHealth: float`
    *   `Hunger: float`
    *   `MaxHunger: float`
    *   `Thirst: float`
    *   `MaxThirst: float`
    *   `Stamina: float`
    *   `MaxStamina: float`
    *   `OnHealthChanged: event`
    *   `OnStaminaChanged: event`

*   **CombatComponent (Souls-like focus):**
    *   `CurrentWeapon: ItemEntity`
    *   `LockOnTarget: Entity`
    *   `Attack(WeaponAttackType): bool` (Returns true if attack initiated)
    *   `Dodge(Vector3 direction): bool`
    *   `Block(): bool`
    *   `Parry(): bool` (Timed block for riposte opportunity)
    *   `TakeDamage(float amount, DamageType type, Entity source, Bone hitBone)`
    *   `OnDamageTaken: event`
    *   `OnTargetLocked: event`

*   **ScreamerBehaviorComponent:**
    *   `DetectionRadius: float`
    *   `DetectionAngle: float` (Field of view)
    *   `TargetTypes: List<EntityType>` (e.g., Player, HostileNPC)
    *   `ScreamSound: SoundEffect`
    *   `AlertCooldown: float`
    *   `CheckForTargets(): void` (Called periodically)
    *   `Scream(): void` (Plays sound, triggers alert to nearby friendlies)

*   **RagdollComponent:**
    *   `IsActive: bool`
    *   `RootBone: Bone`
    *   `LimbHitboxes: Dictionary<Bone, HitboxShape>`
    *   `ActivateRagdoll(Vector3 impulse)`
    *   `DeactivateRagdoll()`
    *   `ApplyForceToLimb(Bone limb, Vector3 force, ForceMode mode)`

## 4. IMPLEMENTATION STRATEGY

### 4.1. Phases
1.  **Phase 1: Core Engine Setup & Basic Player**
    *   Project creation, version control.
    *   Basic player entity, movement, camera (FPS/TPS).
    *   Stride scene setup, basic lighting.
2.  **Phase 2: Core Survival Mechanics**
    *   Health, hunger, thirst, stamina.
    *   Basic inventory and item pickup.
    *   Day/night cycle.
    *   **Sound Note:** Weapon and tool implementation in this phase must consider a detailed list of sound event categories: Equip, Unequip, Idle handling, Attack, Impact (varied by surface), Miss, Durability break, Reload, Ammo insert/remove, and Special actions.
3.  **Phase 3: Combat System - Melee Focus**
    *   Souls-like combat mechanics (stamina, lock-on, basic attacks, dodge).
    *   Hitbox system and damage application.
    *   Basic enemy AI (placeholder).
4.  **Phase 4: Animation & Physics**
    *   Character animation integration (Mixamo).
    *   Animation merging system development.
    *   Ragdoll physics implementation and area-based damage.
5.  **Phase 5: Advanced Gameplay Features**
    *   Crafting and Building systems.
    *   Security Camera system.
    *   Screaming NPC implementation.
    *   Tribe Log system.
6.  **Phase 6: Networking & Multiplayer**
    *   Basic multiplayer synchronization (player movement, actions).
    *   PvP/PvE mode distinction.
    *   Networked interactions for core features.
7.  **Phase 7: World & Content**
    *   Basic procedural world generation or manual level design tools.
    *   More diverse NPCs and creatures.
    *   Sound design and VFX polish.
    *   **Sound Note:** Sound system expansion in this phase should revisit and enhance initial weapon/tool sounds (Equip, Unequip, Idle, Attack, Impact, Miss, Durability break, Reload, Ammo actions, Special) and add environmental and character sounds.
8.  **Phase 8: Testing, Optimization & Refinement**
    *   Comprehensive testing.
    *   Performance profiling and optimization.
    *   Bug fixing and polish.

### 4.2. Tasks (with example prompts for an AI assistant)

*   **TASK-001-A: Implement FPS/TPS camera and movement.**
    *   **Prompt/Description:** "Create a new Stride 4.2.0.2381 project. Set up a basic scene with a ground plane. Adapt existing template scripts: `PlayerCamera.cs` for switchable FPS/TPS views (with camera collision handling), `PlayerInput.cs` for input event management, and `PlayerController.cs` (utilizing `CharacterComponent`) for player movement (WASD, jump).
    *   **Souls-like Melee Mechanics Integration:** Investigate Stride3D's capabilities for core Souls-like features: target lock-on, target switching (e.g., mouse wheel or dedicated keys), and dynamic camera adjustments for optimal combat visibility. Explore implementing distinct melee attack states (e.g., light, heavy, special) and basic combo sequences. Add new input events to `PlayerInput.cs` for lock-on, dodge, and different attack types. Consider how player model orientation and movement should adapt when locked onto a target (e.g., strafing, maintaining focus).
    *   **Animation System with Mixamo Blending:** Investigate Stride's animation system for advanced blending techniques, particularly upper/lower body animation separation and merging (inspired by the user's three.js ragdoll/animation example, focusing on animation aspects here). Use a placeholder character model initially and integrate a selection of Mixamo animations (e.g., idle, walk, run, basic attacks, dodge). This includes the sub-task of pre-processing or designing a workflow to generate separate upper-body (e.g., aiming, attacking) and lower-body (e.g., walking, running, strafing) animation clips from full-body Mixamo animations. Implement initial logic in an animation controller script to play and blend these clips based on player state (e.g., lower body plays walk/run, upper body plays idle or aiming). Note that physics-based ragdoll effects for realistic damage feedback and death animations are a related but distinct future task (`TASK-002-B`).
    *   **Input Remapping Clarification:** The initial setup will utilize the template's approach for input handling (e.g., hardcoded key lists or simple mapping in `PlayerInput.cs`). Full UI-driven input remapping is a more extensive feature planned for `TASK-004-C`.
    *   **PvP/PvE Design Consideration:** Develop the player controller with an awareness of future PvP and PvE mode distinctions. Consider how systems like targeting, damage application, or ability usage might need to differ or be configurable based on the game mode.
    *   **NPC Alert System Note:** Player actions (e.g., making noise, being detected) will eventually need to interface with an NPC alert system. This is for future integration and not part of this immediate task, but the player controller should be extensible enough to support such interactions.
    *   **Dual Melee Mode Toggle:** The player controller must support a toggleable melee mode system (standard vs. Souls-like). Input for this toggle should be considered.
    *   **Souls-like Melee Mechanics Investigation Findings & Design Outline:**
        *   **I. Lock-On System:**
            *   **Target Acquisition:**
                *   Proposal: Use `Simulation.ShapeSweep()` with a `SphereColliderShape` from the player/camera.
                *   Target Identification: Define a specific `CollisionFilterGroup` (e.g., "TargetableEnemy") and a `TargetableComponent.cs` script on enemy entities. This component will provide a lock-on point (e.g., a `Transform` or `Vector3` offset).
            *   **Target Selection:** If multiple valid targets are found by the sweep, select the one closest to the player's forward vector or camera's center screen. Store the current target (e.g., `Entity targetEntity` in `PlayerCombatController`).
            *   **Camera Control (Locked-On):**
                *   In `PlayerCamera.cs`, when a target is locked, the camera should smoothly orient to keep both the player and the target's lock-on point in view, typically trying to frame them based on combat needs. Player input (right stick/mouse) allows for minor adjustments or orbiting around the target.
            *   **Player Movement (Locked-On):**
                *   In `PlayerController.cs` (or a new `PlayerCombatController.cs`), when locked-on:
                    *   Forward/backward input (W/S or left stick Y-axis) moves the player towards or away from the target.
                    *   Horizontal input (A/D or left stick X-axis) makes the player strafe/orbit around the target while maintaining orientation towards it.
                    *   Player model should always face the target.
            *   **Target Switching:** Implement input (e.g., right stick flick, dedicated buttons like Q/E or L1/R1) to cycle through other valid targets within the acquisition range.
            *   **Exiting Lock-On:** Lock-on state is exited if:
                *   Player presses the lock-on toggle input again.
                *   Target is defeated (e.g., health <= 0).
                *   Target moves out of a maximum lock-on range or breaks line of sight for a certain duration.
        *   **II. Animation System for Melee (using `AnimationComponent`):**
            *   **Upper/Lower Body Blending:**
                *   The `AnimationComponent` allows playing multiple animations. Manage `PlayingAnimation` instances directly.
                *   Lower body animations (e.g., Idle, Walk, Run, Strafe_L/R, Dodge_Fwd/Bwd/L/R) should be played on a lower set of bones. Use `AnimationBlendOperation.LinearBlend` for smooth transitions between movement states.
                *   Upper body animations (e.g., Idle_Upper, Attack_Light, Attack_Heavy, Block_Idle, HitReaction_Upper) should be played on an upper set of bones. Use `AnimationBlendOperation.Additive` with a weight of 1.0 for these, or `LinearBlend` if they are full-body but masked.
                *   This requires Mixamo (or other) animations to be either pre-separated into upper/lower body clips or for the animation system to support bone masking during playback. Stride's `AnimationComponent` might require manual setup of which bones are affected by which `PlayingAnimation` if direct masking isn't available per-track in the same layer.
            *   **Playing Attacks/Actions:** Use `AnimationComponent.Play()` for distinct actions like attacks or dodges. These might temporarily override either the upper body or full body. `AnimationComponent.Crossfade()` can be used for smoother transitions if animations are designed for it.
            *   **Hit Reactions:** Play specific hit reaction animations upon taking damage. These would typically affect the upper body or full body and interrupt other actions.
            *   **`IBlendTreeBuilder`:** Stride provides this interface. For advanced blending (e.g., multi-directional movement, speed-based animation changes within a single state), a custom blend tree might be necessary. However, initial upper/lower body separation and action overrides can likely be managed with careful `Play()` and `Crossfade()` calls on the `AnimationComponent` without a full custom `IBlendTreeBuilder`.
        *   **III. Core Combat Scripting:**
            *   **Player States:** Introduce a player state machine (e.g., an `enum PlayerState` managed in `PlayerController.cs` or a dedicated `PlayerCombatController.cs`). States could include: `Idle`, `Moving`, `LockedOnIdle`, `LockedOnMoving`, `Attacking`, `Dodging`, `Blocking`, `HitStun`, `Dead`. Player input and animation playback will be handled based on the current state.
            *   **Stamina Management:** Create a `StaminaComponent.cs` (or integrate into an existing `PlayerStats.cs`) to manage stamina. Actions like attacking, dodging, blocking, and potentially running will consume stamina. Stamina should regenerate over time when not performing stamina-consuming actions. Running out of stamina could prevent actions or induce a fatigue state.
            *   **Input Expansion (PlayerInput.cs):**
                *   Add new `EventKey`s for combat actions: `LockOnToggleEventKey`, `DodgeEventKey`, `LightAttackEventKey`, `HeavyAttackEventKey`, `BlockEventKey`, `ParryEventKey` (future).
                *   Map these to keyboard/mouse and gamepad inputs (e.g., Middle Mouse/R3 for Lock-On, Space/B for Dodge, LMB/RB for Light Attack, Shift+LMB/RT for Heavy Attack, RMB/LB for Block).
        *   **IV. Future Considerations (Briefly Mention):**
            *   **Blocking/Parrying:** Implement mechanics for active blocking (reduce/negate damage, consume stamina) and parrying (timed block for a counter-attack opportunity).
            *   **Attack Combos:** Allow sequencing of light and heavy attacks into predefined combo chains.
            *   **Invincibility Frames (I-frames):** Grant temporary invulnerability during parts of the dodge animation.
            *   **Animation-Driven Movement (Root Motion):** Investigate Stride's support for root motion. If robust, it could simplify attack/dodge movement by driving character displacement from animation data rather than purely script-based movement. This requires animations to be authored with root motion.
*   **TASK-001-B: Integrate modular inventory and hotbar.**
    *   **Prompt/Description:** "Develop a foundational inventory system.
    *   **Leverage Existing Template UI:** Investigate adapting UI scripts like `InventoryPanelScript.cs`, `ItemSlotScript.cs`, and associated `.sdslui` assets from Stride templates to create the basic visual structure for the inventory panel and a player hotbar.
    *   **Modular Item Data Structure:** Design and implement a flexible data structure for items. This structure should be capable of representing various item types: basic resources (wood, stone), tools (axe, pickaxe), weapons (sword, bow), consumables (food, potions), and deployable items (e.g., the future 'Security Camera'). Key item properties might include ID, name, description, stackability, type, weight, and specific data based on type (e.g., damage for weapons, healing amount for consumables).
    *   **Hotbar Functionality:** Implement a hotbar UI that allows quick access to a limited number of items, primarily weapons, tools, and potentially consumables. This will involve linking inventory slots to hotbar slots and handling input for selecting/using items from the hotbar. Consider adapting or creating an equipment manager (potentially inspired by `PlayerEquipment.cs` from templates) to handle equipping/unequipping items reflected in the hotbar.
    *   **Tribe Log Interaction Note:** Certain inventory events, such_as dropping high-value items, crafting rare gear, or using specific quest-related items, may eventually need to be recorded by the Tribe Log system. This is for future integration.
    *   **Security Camera System Note:** The inventory system must be designed to handle special items like 'Security Camera' deployables or tools related to viewing camera feeds. This is for future integration.
*   **[TASK-002-A] Implement Basic Melee Weapons and Tools (Pick, Hatchet):** (Prompt for this task would detail initial weapon/tool setup, linking to `PlayerEquipment`, basic attack animations, and resource gathering interaction. Placeholder for now.)
*   **[TASK-002-B] Add ranged, explosive, and special weapons {ESTIMATE:24h}**
    *   **Prompt:**
        > Extend the weapon system to include bows (multiple arrow types), firearms, explosives, and special weapons (plasma, railgun). Each should have distinct sound effects and visual feedback.
*   **Task Example (Phase 3):** "Implement a lock-on targeting system for the CombatComponent. When a button is pressed, the player should target the nearest enemy within a specified range and cone of view. The camera should smoothly adjust to keep both player and target in frame."
*   **Task Example (Phase 4):** "Develop a script to manage ragdoll physics for character entities. On death, the character's kinematic animation should be replaced by a physics-driven ragdoll. Implement a basic area-based damage system where hits to specific ragdoll bones (e.g., head) apply damage multipliers."
*   **Task Example (Phase 5):** "Create the Screaming NPC. It needs a detection component (cone of vision, range) to spot players or hostile NPCs. Upon detection, it should play a 'scream' sound effect and trigger an event that other nearby friendly NPCs can subscribe to."
*   **[TASK-006-A] Add boats, spyglass, binoculars, scouting drone {ESTIMATE:24h}**
    *   **Prompt:**
        > Implement controllable boats, handheld spyglass, binoculars, and a deployable scouting drone with remote camera and minimap integration.

### 4.3. Implementation Order Diagram Description
*(A conceptual diagram would be inserted here. For text, it's described.)*

The implementation order will generally follow the phases. Core systems are built first, followed by gameplay mechanics layered on top. Networking will be integrated iteratively with features once they are stable in single-player. Animation and physics enhancements will be developed in parallel once the basic character controller is ready.

`[Core Setup] -> [Survival Mechanics] -> [Combat Basics] -> [Animation/Physics] -> [Advanced Gameplay] -> [Networking] -> [World/Content] -> [Testing/Polish]`

Dependencies:
*   Combat depends on Player Mechanics.
*   Advanced Gameplay Features depend on Combat and Player Mechanics.
*   Networking depends on most other systems being functional locally.
*   Ragdoll/Animation depends on basic character setup.

## 5. TESTING STRATEGY

### 5.1. Approach
*   **Unit Testing:** For isolated components and systems (e.g., StatsComponent calculations, Inventory logic).
*   **Integration Testing:** Testing interactions between systems (e.g., CombatComponent affecting StatsComponent, Crafting using InventoryComponent).
*   **Gameplay Testing:** Manual testing by playing the game to assess usability, fun factor, and identify bugs. Focus on specific feature sets during their development phase.
*   **Stress Testing:** (Later phases) Pushing limits for performance, networking, and AI.

### 5.2. Coverage
*   Aim for high unit test coverage for critical logic (combat calculations, persistence, core player stats).
*   Integration tests for all major feature interactions.
*   Gameplay testing scenarios covering all implemented features and player actions.

### 5.3. Automation
*   Explore Stride's testing capabilities or external .NET testing frameworks for unit tests.
*   Consider simple in-game debug tools or command cheats to facilitate specific scenario testing during development (e.g., spawn items, set player stats).

---
## 6. APPENDICES

### A. Weapon Stats Table
| Weapon               | Damage | Fire Rate | Range | Special         |
|----------------------|--------|-----------|-------|-----------------|
| Pick                 | 15     | 0.5/s     | Melee | Mining bonus    |
| Hatchet              | 20     | 0.6/s     | Melee | Wood bonus      |
| Mining Drill         | 10     | 1.5/s     | Melee | Area mining     |
| Bow                  | 40     | 1/s       | 50m   | Arrow drop      |
| Pistol               | 30     | 2/s       | 70m   | Moderate recoil |
| Shotgun              | 10x10  | 1/s       | 20m   | Spread          |
| Assault Rifle        | 25     | 10/s      | 100m  | Auto/Burst      |
| Sniper Rifle         | 100    | 0.5/s     | 300m  | Scope           |
| Grenade              | 150    | N/A       | 20m   | Thrown, AoE     |
| Rocket Launcher      | 250    | 0.5/s     | 60m   | Splash damage   |
| Plasma Rifle         | 35     | 8/s       | 80m   | Energy damage   |
| Railgun              | 200    | 0.2/s     | 500m  | Penetration     |

---

### B. Structure Material Table
| Material     | HP    | Weakness           | Strength                |
|--------------|-------|--------------------|-------------------------|
| Thatch       | 200   | Fire, rain         | Cheap, fast to build    |
| Wood         | 1000  | Fire, explosives   | Moderate cost/time      |
| Stone        | 5000  | Explosives, siege  | High melee resist       |
| Metal        | 10000 | Explosives         | High overall resist     |
| Reinforced   | 20000 | Heavy explosives   | Highest resist          |
| Glass        | 600   | Melee, explosives  | Greenhouse effect       |

---

### C. Defensive Structure Table
| Defense Type       | Damage/Effect    | Power Use | Special                      |
|--------------------|------------------|-----------|------------------------------|
| Spike Wall         | 40/sec contact   | 0         | Bleed effect                 |
| Auto Turret        | 30/shot          | 10/min    | Needs ammo, targets hostiles |
| Laser Turret       | 50/shot          | 20/min    | High accuracy, energy        |
| Electric Fence     | Stun/10dmg       | 5/min     | Stuns targets                |
| Force Field        | Blocks all but C4| 50/min    | Only explosive/plasma damage |

---

### D. Power & Irrigation Table
| Device             | Power Use | Range/Capacity | Notes                       |
|--------------------|-----------|----------------|-----------------------------|
| Gas Generator      | 20/min    | 50m cable      | Needs fuel                  |
| Wind Turbine       | 0-15/min  | 30m cable      | Wind dependent              |
| Solar Panel        | 0-10/min  | 30m cable      | Sun dependent               |
| Battery            | Input/Output | 1000 capacity | Stores power                |
| Water Reservoir    | 0         | 500 units      | Collects rain               |
| Irrigation Pipe    | 0         | Connects       | Transports water            |
| Sprinkler          | 2/min     | 8m radius      | Irrigates crops             |

---

## 7. ADDITIONAL USER REQUIREMENTS (Incorporated into relevant task prompts)

The following additional features and considerations were requested by the user and have been integrated into the detailed prompts of the relevant tasks above, or will be considered during design and implementation:

*   **Separate PvP/PvE Modes:** Multiplayer will eventually need distinct rule sets and potentially server settings for Player vs. Player and Player vs. Environment modes. This will be primarily addressed in PHASE-005 (Multiplayer & Networking) and influence menu design in PHASE-004.
*   **Tribe Log System (Ark-like):**
    *   Log significant team/player events: member deaths, destruction of hostile structures, kills of hostile players/NPCs.
    *   NPC/Team member death notifications.
    *   Tek Sensor-like logging capabilities for specific in-game items/structures.
    *   This will be a new system, likely with UI elements (PHASE-004) and backend logic integrated with various game events.
*   **Security Camera:** An item that can be placed and accessed remotely. This will involve inventory integration (TASK-001-B, TASK-004-A), placement mechanics (PHASE-003), and a UI for viewing.
*   **NPC Alert System ("Screamer" NPC):** An NPC type that detects and alerts to nearby hostile players or other NPCs. This involves AI behavior (new task, likely post-PHASE-001) and potentially integrates with the notification/log system.
*   **Souls-like Melee Combat Features (FPS/TPS):**
    *   More intuitive close-quarters combat with features like lock-on, responsive attacks/dodges, and potentially parrying.
    *   Investigation and initial animation setup are part of TASK-001-A. Full weapon integration is in PHASE-002.
*   **Animation Merging (Mixamo & Physics-based Ragdoll):**
    *   Utilize Mixamo animations.
    *   Implement animation blending for upper/lower body parts (as per user's three.js example).
    *   Physics-based ragdoll effects for realistic area-specific damage reactions.
    *   Initial setup and investigation are part of TASK-001-A.

---

## 8. GENERAL TECHNICAL NOTES & WORKFLOW

*   **Stride Version:** All development must be compatible with Stride 4.2.0.2381.
*   **Project Files:** The solution (`.sln`) and project (`.csproj`, `.sdpkg`) files must be kept up-to-date as new files and dependencies are added.
*   **Source Control Workflow ("Auto Publish"):** The AI (Jules) will create commits and branches for features/fixes. The user will be responsible for reviewing and pushing these to the remote GitHub repository.
*   **Task Batching ("8 tasks"):** This is a general guideline for the AI to structure its work into manageable chunks. The AI will aim to complete roughly 8 plan steps or a similar amount of work before requiring a major check-in or new planning phase, user feedback permitting.
*   **Commercialization:** The project is intended for commercial release (e.g., on Steam). All code and asset usage must comply with relevant licenses (e.g., Stride's MIT license, asset store licenses).

---
**End of Plan**
---
