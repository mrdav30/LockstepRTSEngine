Lockstep RTS Engine
------------------------
The Lockstep RTS Engine (LRE) is an engine designed for 3d RTS games with lockstep simulations. It includes a deterministic 2D physics engine, pathfinding, behavior system, and more. LRE is integrated with Unity, but can be abstracted away.

Special thanks to John Pan (https://github.com/SnpM) for the hard work and dedication he put into his Lockstep Framework (https://github.com/SnpM/LockstepFramework). Also to Elgar Storm for the amazing tutorial he created for developing an RTS game (http://www.stormtek.geek.nz/rts_tutorial/part1.php).

Under development by mrdav30 (https://github.com/mrdav30).

Features
__________
- Deterministic math library and simulation logic
- 2D physics engine on the X-Z plane.
- Behaviour system for both individual agents and globally
- Lockstep variables - know when and where desyncs happen
- Size-based pathfinding (big units won't get stuck in those narrow gaps)
- Customizable database system
- Support for Forge Networking (DarkRift and Photon coming soon)

Quick Setup
-----------
1. Import the engine into a Unity project and open RTS-Engine/Example/ExampleScene
2. Set up the database and settings by navigating to the RTS-Engine/Database window or pressing Control - Shift - L.
3. In the Settings foldout of the database window, click Load and navigate to RTS-Engine/Example/ExampleDatabase/Example_Database.asset to load the preconfigured database for the example.
4. Play!

Note: The example only shows the basic functionality of the engine. Comprehensive examples will be added close to the end of core development.

To use in an existing scene, locate the Manager prefab in Core/Example/ and add that into your scene. This prefab comes with 3 components attached: LockstepManager, TestManager, and PlayerManager. LockstepManager contains settings for simulation and non-simulation related things that many other pieces of the LSF use.

TestManager is an example of the script you would write to interact with the LSF. It creates an AgentController, creates 256 agents under that AgentController, and adds the AgentController to PlayerManager for the player to interact with that controller. In FixedUpdate and Update, LockstepManager.Simulate () and LockstepManager.Visualize () are called, respectively. These distribute necessary information to the LSF for when to execute frames.

Click play and enjoy the lockstep simulation of group behaviors and collision responses.

TODO:
-------
These are high priority issues that are significantly big or complicated. Any help on these aspects (as well as on any other lacking parts of the framework) would be very appreciated.
- Interpolation. Currently, interpolation between the position of the last simualtion frame and the current simulation frame for a unit causes stuttering. To mitigate this issue, another layer of interpolation is used. The current code for smoothing interpolation and communicating the positions to Unity's transform system is in LSBody.Visualize, around line 506. Note that setting LerpDamping to 1 will remove the extra layer of interpolation and uncover the stuttering.
- Pathfinding around corners. Even explaining to me a solution to this problem will help a lot.
- (After Lockstep Variables are fully tested) Lockstep Variable integration. Currently, no abilities use Lockstep Variables which are used to track determinism and also reset values upon re-initialization of the unit. A lot of work must be done to mark as many value-type deterministic variables as possible [Lockstep] and move their initialization to Setup () since LSVariables automatically handle resetting.
- Integrations for various networking solutions (i.e. DarkRift, Photon, UNet, Bolt)
- Safe and scalable coding patterns. Anywhere you see something that might cause problems for a large-scale project, fixing it or raising an issue would be a big help. LSF started as a work of curiosity and passion. While it's introduced me to helpful experiences, I didn't always see the flaw in using statics everywhere. If there are any significantly limiting problems with a component's design, I'll do my best to fix it.
- Implement and test polygon colliders.

Road Map
---------
- Semi-3D physics (may be coming soon!). Physics calculations on the Y axis requires non-trivial changes but collision detection for objects like bullets may be able to be hacked in.
- Multi engine support. Customizable support for game engines in addition to Unity could make this framework viable for many more people.

Ability Pattern
--------
Abilities are moddable behaviors that can be easily attached, detached, and moddified on prefab game objects. They follow the following pattern:

- The overridable Initialize() method is called when the agent the ability belongs to is created and initialized. It provides an argument that is the agent the ability belongs to. Because LSF uses object pooling, the Ability must also be reset in Initialize().
- Simulate() is called every single simulation frame.
- Deactivate() is called when the ability's agent is deactivated (i.e. killed). Note that Simulate() will not be called until after Initialize() is called again.

Active Ability Pattern
--------
ActiveAbility inherits from Ability and includes all the patterns described above. In addition ActiveAbilitys can be interacted with by players through Commands.

- Execute () is called when a Command is received and activates the ability. This method provides an argument that is the Command responsible for the ability's activation.
- The ListenInput property is the input that the ability listens to. If a Command with the InputCode of ListenInput is received, Execute () is called on the ability.

Essential Abilities
---------
Currently, only movement with crowd behaviors is implemented. If you'd like to contribute, please explore Core/Game/Abities/Essential/ and help create more essential behaviors (i.e. Health, Energy, Attack, Stop).

Example Scene Requires:
---------

- [Post Processing Stack](https://assetstore.unity.com/packages/essentials/post-processing-stack-83912)
- [Legacy Image Effects](https://assetstore.unity.com/packages/essentials/legacy-image-effects-83913)
