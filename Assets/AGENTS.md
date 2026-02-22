# Space Pixel Unity project

We are making a game about spaceships shooting at each other in a game. They are made of destroyable pixels, so the physics are fun!
This game aims to be a bit similar to Highfleet, but expands on it by making individual pixels destroyable.

## Contributing

Code quality is important. You can write comments, but 99% of comments should be deleted and code extracted to methods before presenting your work as done. Long method names are totally okay, as long as they are descriptive

If a component/anything is necessary for the script, don't silently fail, fail hard with an error!

Periodically check for errors - it sometimes takes a bit of time to auto compile .cs files after you write them

This project uses assemblies, so if you want to use code from other assembly, double check if you should. If you should, write the code as if you used the assembly and after compiling and seeing errors, just please let me know in the chat. I will link them manually in my IDE

Do a quick code review before handing off as "done", thanks!

## Architecture

All of the physically destructible objects are/inherit from PixelatedRigidbody.

Ship is the base class for ship, with PlayerShip and AIShip inheriting from it. AIShip has state machines: behaviour and navigation.

UI = UIToolkit

Assemblies:

- AI: just the basic state machine, don't touch
- Background: just the background, don't touch
- Context: when creating new services etc., make sure to add them to GameSceneInstaller (Zenject) and use it's dependency injection
- ContourTracer: calculating physical outlines of PixelatedRigidbodies, don't touch
- Core: mostly basic interfaces/enums
- EasyPool: just the pool, don't touch
- Editor: inspector extensions, tools, standalone debugger scripts
- Events: ScriptableObjects (Collision, etc.). When objects need to communicate with each other, they should use events. Don't forget to unsubscribe from events when the object is disabled and subscribe on enabled
- External: third party code, don't touch
- Gameplay: the actual gameplay code
- Grid: PixelGrid mostly (is inside PixelatedRigidbody), probably don't touch
- Instantiation: used for ZenjectInstantiator when you need to instantiate something with Zenject, don't touch
- LM: my personal utility scripts, feel free to use these utils (GameInput, SimpleTimer, Timer, VecExt, MathExt, DefaultDictionary)
- Pixelation: PixelatedRigidbody! And CollisionResolver and PixelCollisionHandler
- Services: many different services, like spawners, or to access ships, sectors etc. Don't forget to add new services to GameSceneInstaller (Zenject) and use it's dependency injection
- Ships: all ship related code, like Ship, PlayerShip, AIShip, and very importantly - Module (ships are made of modules, which have PixelatedRigidbody, and they are what make up the ship, they can be destroyed)
  - Current modules inheritors: CommandModule (it's position is the ship's position - don't use ship transform.position!). Cannon, Laser, Engine  
  - Modules use efficiency based on how many pixels they have left.
  - Modules have crew and power needs, and they can produce both
- UI: UI (use UIToolkit)

## Testing

I will run tests after you are done. Make sure to test critical functionality in PlayMode tests.
Tests are in AssemblyFolder/Tests in it's own assembly.