Welcome to the cross-platform editor for the Stride Game Engine!

Stride is an open-source C# game engine for realistic rendering and VR.

As of the writing of this document (2023-10-08), the stride engine editor (also known as *Game Studio*) can only run on Windows.
This repository is an attempt at bringing a full editing experience to more desktop platforms (namely Windows, Linux and Mac).

This is going to be a long journey. I hope you will ride along with us.

## FAQ

### Why a fork and not a branch on the main repo?

This is first and foremost a personal attempt, and insted of trying to somehow adapt the existing editor to use a different UI framework (which is the main blocker, as it is originally using WPF),
my goal is to do a (almost) complete rewrite, and try to get rid of some of the legacy architecture that might not fit modern standards

### Will it be merged to the main repo at some point?

Probably. If it gets stable enough, there is no reason not to share it with upstream.

###  What are the short term goals?

A minimum viable product (MVP) with the following features:
- being able to open a stride project
- being able to build a stride project from the editor UI
- being able to visualize the tree structure of a scene
- being able to visualize a rendering of a scene embedded in the editor

Optional goals:
- free camera on the scene editor

Non-goals for the MVP:
- full editing experience
- localization
- asset creation
- plugin system

### Who will work on this project?

Mostly me, though I will accept external contributions after the MVP has been completed.

### Who are you?

Nicolas Musset, I worked at Silicon Studio on the Paradox (then Xenko) game engine, between 2015 and 2017.
Mostly on the editor, so I'm partially responsible for the over-engineering on that part.
