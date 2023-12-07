# Bepu Physics V2 Integration with Stride 3D - Work in Progress

Integrating [Bepu Physics v2](https://github.com/bepu/bepuphysics2) into [Stride](https://github.com/stride3d/stride).

## Features

1. Simulations: Highly configurable simulation & multi-simulation
2. Bodies: Static, dynamic, and kinematic
3. Colliders: MeshCollider, ConvexHullCollider, Box, Sphere, Cylinder, Capsule, Triangle (Note: MeshCollider is categorized as a "body"; further details in the documentation)
4. Collision Handler System by body & RayCast system
5. CharacterController (Work in Progress)
6. CarController
7. Numerous utility scripts & scenes to aid understanding and usage of this Bepu implementation in Stride.

## Usage

Clone this repository to your local machine and utilize it either as a library within your Stride project or as an independent Stride project.

1. **Add Bepu Settings** :

![Bepu Settings](https://github.com/Nicogo1705/BepuPhysicIntegrationTest/assets/20603105/5f0ee87e-b850-4871-ace0-b5eafc131eca)

2. **Add Containers & Colliders** :

![Static Containers](https://github.com/Nicogo1705/BepuPhysicIntegrationTest/assets/20603105/9f8a12bb-0ae7-4a1a-9f9d-77a8d9680c0c)
![Body Containers](https://github.com/Nicogo1705/BepuPhysicIntegrationTest/assets/20603105/079df952-eeee-4b18-a0dc-efe330731651)
![Colliders](https://github.com/Nicogo1705/BepuPhysicIntegrationTest/assets/20603105/b00cf981-f905-4bec-8f4a-be1f622c5daa)

3. **Add constraints** :

![Constraints](https://github.com/Nicogo1705/BepuPhysicIntegrationTest/assets/20603105/5a19b2bb-7786-4d79-8248-763ef505beb9)

4. **Use/Write Utility Scripts** :

![Utility Scripts](https://github.com/Nicogo1705/BepuPhysicIntegrationTest/assets/20603105/d36f7f30-128c-4166-a657-8f6bdad36ec8)

## Settings

### Description

Settings represent an instance of Bepu physics, defining various configurations for a simulation. You can create multiple settings to set up different simulations within your Stride project. These settings allow editing simulation global parameters, enabling customization and fine-tuning of the physics environment to suit specific requirements.

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

Containers serve as representations or links within Bepu physics for managing specific types of entities:

- **BodyContainer**: This container can contains in the same node or in childs nodes somes colliders provided in the "Colliders" section, representing physical bodies in the simulation.
- **StaticContainer**: Similar to the BodyContainer but represents static objects in the simulation.
- **BodyMeshContainer**: Specifically used for perfect mesh colliders in conjunction with the appropriate mesh collider, representing physical bodies.
- **StaticMeshContainer**: Like the MeshBodyContainer, this is used for perfect mesh colliders but represents static objects in the simulation.

These containers play a vital role in organizing and defining the properties and behaviors of entities within the Bepu physics simulation environment.

### Properties

1. SimulationIndex
- **Description:** Allow you to choose in which simulation the object is.
- **Type:** Integer

2. Spring frequency
- **Description:** Determines the oscillation rate or stiffness of a spring constraint.
- **Type:** Float
- **Range:** Positive values; higher values result in a stiffer spring.

3. Spring damping ratio
- **Description:** Controls the rate at which oscillations in a spring constraint decrease over time, affecting its responsiveness and stability.
- **Type:** Float
- **Range:** Values between 0 and 1; higher values dampen oscillations more quickly.

4. Friction coefficient
- **Description:** Specifies the resistance to motion between two colliding objects, affecting how much they slide against each other.
- **Type:** Float
- **Range:** Non-negative values; higher values increase friction.

5. Maximum recovery velocity
- **Description:** Sets the maximum speed at which objects can recover from penetration due to collisions or constraints.
- **Type:** Float
- **Range:** Positive values; higher values allow faster recovery from interpenetration.

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

Colliders define the shapes and properties of physical objects within a Bepu physics simulation. They play a crucial role in determining how objects interact with each other and the environment. There are various types of colliders available, each suited for different scenarios:

1. **Box Collider**: Represents a rectangular prism-shaped collider, often used for simple objects like crates or buildings.
2. **Capsule Collider**: Combines a cylinder and two half-spheres, useful for character models or objects with cylindrical shapes.
3. **Convex Hull Collider**: Defines a collider based on the convex hull of a mesh, providing a simpler collision shape approximation for more complex geometries.
4. **Cylinder Collider**: Shapes objects as cylinders, suitable for entities like pipes or cylindrical objects.
5. **Sphere Collider**: Shapes objects as spheres, suitable for entities like balls or spherical objects.
6. **Triangle Collider**: Uses triangles from a mesh to create a collider, often used for terrain or ground collision.
  
⚠️ A **Mesh Container**: Allows for collision based on the exact geometry of a mesh, enabling precise collision detection for irregular shapes but it cannot be compounded with regular colliders (so no colliders in the same/childs nodes of a **MeshContainer**).

Each collider type has its advantages and is chosen based on the specific requirements of the objects you're simulating. They come with parameters that can be adjusted such as Height. Note that you can compound any colliders by adding more component. Colliders must be in the same or in the child entities of a **BodyContainer** or a **StaticContainer**.
If in a child entity, you can move, rotate & scale* it using Transform.

*Only ConvexHullCollider & MeshContainer can be scaled.

### Properties

1. Dimensional (Height, radius, ..)
   - **Type:** float
   - **Description:** Allows you to choose the size of the collider *(Refer to [Bepu Docs](https://github.com/bepu/bepuphysics2).)*

2. Mass
   - **Type:** float
   - **Description:** Allows you to choose the mass of the collider volume *(Refer to [Bepu Docs](https://github.com/bepu/bepuphysics2).)*

3. Others
   - **Description:** *(Refer to [Bepu Docs](https://github.com/bepu/bepuphysics2) for more details on others properties.)*

## Constraints

### Description

Constraints define relationships or rules that govern how entities interact within the simulation. They establish connections between bodies, dictating their movement or behavior based on specific criteria or physical laws.

Some common types of constraints in BepuPhysicsV2 include:

- **BallSocketConstraint**: Restricts two entities to a fixed distance, allowing rotation around the connecting point.
- **DistanceLimitConstraint**: Constrains entities within a certain distance range, restricting their movement beyond defined limits.
- **HingeConstraint**: Permits rotation around a single axis, simulating a hinge-like movement between entities.
- **AngularMotorConstraint**: Applies rotational force to enforce desired angular motion between entities.
- **TwistMotorConstraint**: Controls twisting motion between entities, allowing controlled rotation around a specific axis.

These constraints play a crucial role in simulating realistic interactions and behaviors between entities within the physics environment, enabling the creation of complex and accurate simulations.

### Properties

1. Bodies
   - **Type:** List<BodyContainerComponent>
   - **Description:** Allows you to choose wich bodies to apply the constraint on. *(Refer to [Bepu Docs](https://github.com/bepu/bepuphysics2) for more details on how many entities for each constraints.)*

2. Others
   - **Description:** *(Refer to [Bepu Docs](https://github.com/bepu/bepuphysics2) for more details on others properties.)*

## Use/build Utility scripts

These scripts allow runtime modification of the simulation using keyboard inputs and serve as a good starting point to understand Bepu integration in Stride.

## Issues & To-Do

Check out the [TODO.txt](https://github.com/Nicogo1705/BepuPhysicIntegrationTest/blob/master/BepuPhysicIntegrationTest/Todo.txt) for pending tasks and issues.

## Videos
Note that in some videos, Bepu simulation settings had been tweaked to maximize performance.

- [Demo](https://www.youtube.com/watch?v=EfCq23aUThM)
- [Stress Test](https://www.youtube.com/watch?v=-3EgJr2k4uE)
- [The Mixer](https://www.youtube.com/watch?v=dMS5TSkN6q0)
- [Optimisations](https://youtu.be/71fn0AcVWng) 
- [Car & Ropes](https://youtu.be/Odmg_he3CQ4)
- [Super Car](https://youtu.be/IxJKTk29Nsw)
- [AllScenes (comming soon)](https://www.youtube.com/@Nicogo17)

*These videos were recorded in 1080p using OBS, demonstrating the integration in Debug mode on hardware featuring an i7 6700k & GTX 970.*
