# Bepu Physics V2 Integration with Stride 3D - Work in Progress

[![Build nuget package](https://github.com/Nicogo1705/Stride.BepuPhysics/actions/workflows/dotnet-nuget.yml/badge.svg)](https://github.com/Nicogo1705/Stride.BepuPhysics/actions/workflows/dotnet-nuget.yml)

Integrating [Bepu Physics v2](https://github.com/bepu/bepuphysics2) into [Stride](https://github.com/stride3d/stride).

Check out the Docs in the [wiki](https://github.com/Nicogo1705/Stride.BepuPhysics/wiki). (WIP)

## Features

1. Simulations: Highly configurable simulation & multi-simulation
2. Bodies: Static, dynamic, and kinematic
3. Colliders: MeshCollider, ConvexHullCollider, Box, Sphere, Cylinder, Capsule, Triangle (Note: MeshCollider is categorized as a "body"; further details in the documentation)
4. Collision Handler System by body & RayCast system
5. CharacterController (Work in Progress)
6. CarController
7. Numerous utility scripts & scenes to aid understanding and usage of this Bepu implementation in Stride.

## Usage

-Clone this repository to your local machine and run the stride project. You will have some samples to look at.

-Use our nuget : https://www.nuget.org/packages/Stride.BepuPhysics/

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

## Issues & To-Do

Check out the [TODO.txt](https://github.com/Nicogo1705/Stride.BepuPhysics/blob/master/Stride.BepuPhysics.Demo/Todo.txt) for pending tasks and issues.

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
