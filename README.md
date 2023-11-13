Welcome to the cross-platform editor for the Stride Game Engine!

Stride is an open-source C# game engine for realistic rendering and VR.

As of the writing of this document (2023-10-08), the stride engine editor (also known as *Game Studio*) can only run on Windows.
This branch is an attempt at bringing a full editing experience to more desktop platforms (namely Windows, Linux and Mac).

This is going to be a long journey. I hope you will ride along with us.

## FAQ

### Why a separate branch on the main repo?

This is first and foremost an attempt, and instead of trying to somehow adapt the existing editor to use a different UI framework (which is the main blocker, as it is originally using WPF),
the goal is to do a (almost) complete rewrite, and try to get rid of some of the legacy architecture that might not fit modern standards.

In order to avoid conflicting with on-going development, on the `master` branch, we will have an alternate *maoin* branch for that effort, and PR will target it instead of `master`.

After each official release on `master` (i.e. tagged version available from the Launcher), we will merge it into our branch, solve any conflicts and backport any changes from the old editor to the new one.

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
- plugin system (though we will try to separate the libraries cleanly in order to prepare for that)

### Who will work on this project?

Mostly me ([@Kryptos-FR](https://github.com/Kryptos-FR/)), though we will accept external contributions after the MVP has been completed.

### Who are you?

Nicolas Musset (aka [@Kryptos-FR](https://github.com/Kryptos-FR/)), I worked at Silicon Studio on the Paradox (then Xenko) game engine, between 2015 and 2017.
Mostly on the editor, so I'm partially responsible for the over-engineering on that part :sweat_smile: .

### How can I contribute?

You can contribute in two ways:

* either donating to the corresponding [Open Collective project](https://opencollective.com/stride3d/projects/editor-rewrite-avalonia).
* or by working on features/requests that are listed on the [dedicated GitHub project](https://github.com/orgs/stride3d/projects/6/).
	* consider joining our [Discord community](https://discord.gg/f6aerfE) as well to participate in discussions
