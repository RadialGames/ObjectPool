ObjectPool
===========

## Concept

This is a Unity Package Manager (UPM)-compatible repository for easily managing MonoBehaviour ObjectPools in Unity.

## Requirements

- Tested in Unity 2019.4.0f1, should work in anything newer.

## Installation

Install it via the Unity Package Manager by:
- Opening your project in Unity
- Open the Package Manager window (`Window > Package Manager`)
- Click the `+` button in the top left corner of the window
- Select `Add package from git URL...`
- Enter the following url, and you'll be up to date: `https://github.com/RadialGames/ObjectPool.git`

## Dependencies

This package requires you also install our Singleton helper: https://github.com/RadialGames/Singleton

## Usage

All files in this package are in the `Radial.ObjectPool` namespace. Access them by adding the following to the top of your
files:

```c#
using Radial.ObjectPool;
```

The easiest way to use this package is to simply use the `Spawn()` and `Recycle()` overloads that have been added
to `MonoBehaviour`:

```c#
public GameObject myPrefab;

void Start()
{
    // Spawn a new instance of myPrefab
    GameObject pooledObject = myPrefab.Spawn();

    // Recycle the instance
    pooledObject.Recycle();
}
```

This will automatically pull from the pool when instances are available, and expand the pool as necessary if you do not
have enough available.

For performance-critical applications, you can pre-emptively specify a pool size:

```C#
pooledObject.CreatePool(10); // creates a pool with an initial size of 10.
```

Don't forget to "reset" the state of your pooled objects in `OnEnable`; `Start` and `Awake` don't get called when an 
object is pulled from a pool.

There are several other helper functions, such as `CountSpawned()` for tracking, `RecycleAll()` for cleanup,
`GetSpawned()` to return a list of all currently active elements, and other related maintenance functions. See the
source for more details.

## Credits

Special thanks to [Chevy Ray Johnston](https://github.com/chevyray), this script was originally pulled from their
(now defunct) website around 2013. The code has been heavily modified several times since.