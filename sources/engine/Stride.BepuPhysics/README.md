# Bepu Physics V2 Integration with Stride 3D - Work in Progress

Integrating [Bepu Physics v2](https://github.com/bepu/bepuphysics2) into [Stride](https://github.com/stride3d/stride).

## Usage

Clone this repository to your local machine and utilize it either as a library within your Stride project or as an independent Stride project.

1. **Add Bepu Settings** :

![Bepu Settings](https://github.com/Nicogo1705/BepuPhysicIntegrationTest/assets/20603105/3b41193f-cfd2-4c47-b615-76580b4c42d6)

2. **Add Containers & Colliders** :

![Containers & Colliders](https://github.com/Nicogo1705/BepuPhysicIntegrationTest/assets/20603105/9e0492b4-6801-4de8-8582-f02acc3cccfc)

3. **Add constraints** :

![Constraints](https://github.com/Nicogo1705/BepuPhysicIntegrationTest/assets/20603105/d36f7f30-128c-4166-a657-8f6bdad36ec8)

4. **Use/Write Utility Scripts** :

![Utility Scripts](https://github.com/Nicogo1705/BepuPhysicIntegrationTest/assets/20603105/d36f7f30-128c-4166-a657-8f6bdad36ec8)

## Settings

### Description
Settings represent an instance of Bepu physics, defining various configurations for a simulation. You can create multiple settings to set up different simulations within your Stride project. These settings allow editing simulation global parameters, enabling customization and fine-tuning of the physics environment to suit specific requirements.

### Properties
### Properties

1. TimeWrap
   - **Type:** float
   - **Description:** Allows you to choose the speed of the simulation.

2. Pose gravity
   - **Type:** Vector3
   - **Description:** Represents general gravity. Note: Will change in the future.
   
3. Linear damping
   - **Type:** float
   - **Description:** Controls linear damping. *(Refer to [Bepu Docs](https://github.com/bepu/bepuphysics2) for more details.)*
   
4. Angular damping
   - **Type:** float
   - **Description:** Controls angular damping. *(Refer to [Bepu Docs](https://github.com/bepu/bepuphysics2) for more details.)*
   
5. Solve iteration
   - **Type:** int
   - **Description:** Controls the number of iterations for the solver. *(Refer to [Bepu Docs](https://github.com/bepu/bepuphysics2) for more details.)*
   
6. Solve sub step
   - **Type:** int
   - **Description:** Specifies the number of sub-steps for solving. *(Refer to [Bepu Docs](https://github.com/bepu/bepuphysics2) for more details.)*
   
7. Parallel update
   - **Type:** bool
   - **Description:** Allows updating Stride's entities' transform in parallel.
   
8. Simulation Fixed step
   - **Type:** float
   - **Description:** Specifies the number of milliseconds per step to simulate.
   
9. Max steps/frame
   - **Type:** int
   - **Description:** Represents the maximum number of steps per frame to avoid a death loop. *(Refer to [Bepu Docs](https://github.com/bepu/bepuphysics2) for more details. Warning: You may lose real-time physics.)*


## Containers

### Description

A containers is your entry in the physics world, it represent one object.
There is 4 kind of containers : Static container, Body container & these both previous variant of the MeshContainer

### Properties

1. SimulationIndex
Allow you to choose in wich simulation the object is.
- **Type:** Integer


2. Spring frequency
TODO

3. Spring gamping ratio
TODO

4. Friction coefficient
TODO

5. Maxixmum recovery velocity
TODO

6. Collider group mask
The collision mask system allows precise control over collision interactions between different groups of objects within the simulation.

- **Type:** Byte
- **Algorithm:**
  - `bool CollisionOccur => (com == a.colliderGroupMask || com == b.colliderGroupMask) && com != 0;`
  - `com` is the comparison value obtained from the bitwise comparison of collision masks of two colliding objects.
  - Collision occurs if the `com` value matches either collider's group mask and is not equal to zero.
- **Truth Table:**
  
| Collision Group Masks | 255 | 1 | 3 | 5 | 0 |
|-----------------------|-----|---|---|---|---|
| 255                   | 255 | 1 | 3 | 5 | 0!|
| 1                     | 1   | 1 | 1 | 1 | 0!|
| 3                     | 3   | 1 | 3 | 1!| 0!|
| 5                     | 5   | 1 | 1!| 5 | 0!|
| 0                     | 0   | 0 | 0 | 0 | 0 |

## Colliders

### Description
TODO
### Properties
TODO


## Constraints

### Description
TODO
### Properties
TODO

## Use/build Utility scripts

These scripts allow runtime modification of the simulation using keyboard inputs and serve as a good starting point to understand Bepu integration in Stride.

## Issues & To-Do

Check out the [TODO.txt](https://github.com/Nicogo1705/BepuPhysicIntegrationTest/blob/master/BepuPhysicIntegrationTest/Todo.txt) for pending tasks and issues.

## Videos

- [Demo](https://www.youtube.com/watch?v=EfCq23aUThM)
- [Stress Test](https://www.youtube.com/watch?v=-3EgJr2k4uE)
- [The Mixer](https://www.youtube.com/watch?v=dMS5TSkN6q0)

*These videos were recorded in 1080p using OBS, demonstrating the integration in Debug mode on hardware featuring an i7 6700k & GTX 970.*
