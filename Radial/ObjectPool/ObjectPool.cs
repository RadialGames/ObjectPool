using UnityEngine;
using System.Collections.Generic;
using Radial.Singleton;

// Special thanks to Chevy Ray Johnston, this script was originally pulled from their
// (now defunct) website around ten years ago. Heavily modified since then.
// http://chevyray.ca/

namespace Radial.ObjectPool
{
	/// <summary>
	/// ObjectPool is a singleton class that helps "Recycle" and "Spawn" gameobjects rather than
	/// destroy or instantiate them.
	/// 
	/// It "just works" by simply calling yourPrefab.Spawn() or yourPrefab.Recycle() (we utilize overload
	/// methods for monobehaviours), but if you want more control over things you can place this script in a scene and
	/// pre-populate it, though this is entirely optional.
	///
	/// A common use case is to:
	/// myPrefab.CreatePool(5); // create an initial pool of 5 objects. This step is optional.
	/// myPrefab.Spawn(); // Place a prefab in the world from the object pool
	/// myPrefab.Recycle(); // Disable a prefab and place it in the pool for future use
	///
	/// The pool will automatically increase in size as necessary, and will survive scene changes.
	///
	/// Don't forget to "reset" the state of your pooled objects in OnEnable; Start and Awake don't get
	/// called when an object is pulled from a pool.
	///
	/// If you heavily utilize ObjectPool you might want to keep an eye on memory size and do some manual
	/// maintenance and upkeep.
	/// </summary>
	public sealed class ObjectPool : Singleton<ObjectPool>
	{
		public enum StartupPoolMode
		{
			Awake,
			Start,
			CallManually
		};

		[System.Serializable]
		public class StartupPool
		{
			public int size;
			public GameObject prefab;
		}

		private static List<GameObject> tempList = new List<GameObject>();

		private Dictionary<GameObject, List<GameObject>> pooledObjects = new Dictionary<GameObject, List<GameObject>>();
		private Dictionary<GameObject, GameObject> spawnedObjects = new Dictionary<GameObject, GameObject>();

		public StartupPoolMode startupPoolMode = StartupPoolMode.Awake;
		public StartupPool[] startupPools;

		private bool startupPoolsCreated;

		// This prevents people from spawning this class in a non-Singleton way.
		private ObjectPool () {}
		
		void Awake()
		{
			if (startupPoolMode == StartupPoolMode.Awake)
			{
				CreateStartupPools();
			}
		}

		void Start()
		{
			if (startupPoolMode == StartupPoolMode.Start)
			{
				CreateStartupPools();
			}
		}

		public static void CreateStartupPools()
		{
			if (!Instance.startupPoolsCreated)
			{
				Instance.startupPoolsCreated = true;
				var pools = Instance.startupPools;
				if (pools != null && pools.Length > 0)
				{
					for (int i = 0; i < pools.Length; ++i)
					{
						CreatePool(pools[i].prefab, pools[i].size);
					}
				}
			}
		}

		public static void CreatePool<T>(T prefab, int initialPoolSize) where T : Component
		{
			CreatePool(prefab.gameObject, initialPoolSize);
		}

		public static void CreatePool(GameObject prefab, int initialPoolSize)
		{
			if (prefab != null && !Instance.pooledObjects.ContainsKey(prefab))
			{
				var list = new List<GameObject>();
				Instance.pooledObjects.Add(prefab, list);

				if (initialPoolSize > 0)
				{
					bool active = prefab.activeSelf;
					prefab.SetActive(false);
					Transform parent = Instance.transform;
					while (list.Count < initialPoolSize)
					{
						var obj = (GameObject) Object.Instantiate(prefab);
						obj.transform.SetParent(parent);
						list.Add(obj);
					}

					prefab.SetActive(active);
				}
			}
		}

		public static T Spawn<T>(T prefab, Transform parent, Vector3 position, Quaternion rotation) where T : Component
		{
			return Spawn(prefab.gameObject, parent, position, rotation).GetComponent<T>();
		}

		public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
		{
			return Spawn(prefab.gameObject, null, position, rotation).GetComponent<T>();
		}

		public static T Spawn<T>(T prefab, Transform parent, Vector3 position) where T : Component
		{
			return Spawn(prefab.gameObject, parent, position, Quaternion.identity).GetComponent<T>();
		}

		public static T Spawn<T>(T prefab, Vector3 position) where T : Component
		{
			return Spawn(prefab.gameObject, null, position, Quaternion.identity).GetComponent<T>();
		}

		public static T Spawn<T>(T prefab, Transform parent) where T : Component
		{
			return Spawn(prefab.gameObject, parent, Vector3.zero, Quaternion.identity).GetComponent<T>();
		}

		public static T Spawn<T>(T prefab) where T : Component
		{
			return Spawn(prefab.gameObject, null, Vector3.zero, Quaternion.identity).GetComponent<T>();
		}

		public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
		{
			List<GameObject> list;
			Transform trans;
			GameObject obj;
			if (Instance.pooledObjects.TryGetValue(prefab, out list))
			{
				obj = null;
				if (list.Count > 0)
				{
					while (obj == null && list.Count > 0)
					{
						obj = list[0];
						list.RemoveAt(0);
					}

					if (obj != null)
					{
						trans = obj.transform;
						trans.SetParent(parent);
						trans.localPosition = position;
						trans.localRotation = rotation;
						trans.localScale = prefab.transform.localScale;
						obj.SetActive(true);
						Instance.spawnedObjects.Add(obj, prefab);
						return obj;
					}
				}

				obj = Object.Instantiate<GameObject>(prefab);
				trans = obj.transform;
				trans.SetParent(parent);
				trans.localPosition = position;
				trans.localRotation = rotation;
				trans.localScale = prefab.transform.localScale;
				Instance.spawnedObjects.Add(obj, prefab);
				return obj;
			}
			else
			{
				Debug.LogWarning(
					"Object was Spawned, but wasn't pooled in the first place. (" + prefab.name +
					"). Creating a pool automatically as a safeguard, but you should probably fix this.");
				CreatePool(prefab, 1);
				return Spawn(prefab, parent, position, rotation);
			}
		}

		public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position)
		{
			return Spawn(prefab, parent, position, Quaternion.identity);
		}

		public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
		{
			return Spawn(prefab, null, position, rotation);
		}

		public static GameObject Spawn(GameObject prefab, Transform parent)
		{
			return Spawn(prefab, parent, Vector3.zero, Quaternion.identity);
		}

		public static GameObject Spawn(GameObject prefab, Vector3 position)
		{
			return Spawn(prefab, null, position, Quaternion.identity);
		}

		public static GameObject Spawn(GameObject prefab)
		{
			return Spawn(prefab, null, Vector3.zero, Quaternion.identity);
		}

		public static void Recycle<T>(T obj) where T : Component
		{
			Recycle(obj.gameObject);
		}

		public static void Recycle(GameObject obj)
		{
			GameObject prefab;
			if (Instance.spawnedObjects.TryGetValue(obj, out prefab))
			{
				Recycle(obj, prefab);
			}
			else
			{
				Debug.LogError("Un-pooled object recycled. Might have been recycled earlier, or maybe it Instantiated without the Spawn command: " + obj.name, obj);
				Destroy(obj);
			}
		}

		static void Recycle(GameObject obj, GameObject prefab)
		{
			Instance.pooledObjects[prefab].Add(obj);
			Instance.spawnedObjects.Remove(obj);
			obj.transform.SetParent(Instance.transform);
			obj.SetActive(false);
		}

		public static void RecycleAll<T>(T prefab) where T : Component
		{
			RecycleAll(prefab.gameObject);
		}

		public static void RecycleAll(GameObject prefab)
		{
			foreach (var item in Instance.spawnedObjects)
			{
				if (item.Value == prefab)
				{
					tempList.Add(item.Key);
				}
			}

			for (int i = 0; i < tempList.Count; ++i)
			{
				Recycle(tempList[i]);
			}

			tempList.Clear();
		}

		public static void RecycleAll()
		{
			tempList.AddRange(Instance.spawnedObjects.Keys);
			for (int i = 0; i < tempList.Count; ++i)
			{
				Recycle(tempList[i]);
			}

			tempList.Clear();
		}

		public static bool IsSpawned(GameObject obj)
		{
			return Instance.spawnedObjects.ContainsKey(obj);
		}

		public static int CountPooled<T>(T prefab) where T : Component
		{
			return CountPooled(prefab.gameObject);
		}

		public static int CountPooled(GameObject prefab)
		{
			List<GameObject> list;
			if (Instance.pooledObjects.TryGetValue(prefab, out list))
			{
				return list.Count;
			}

			return 0;
		}

		public static int CountSpawned<T>(T prefab) where T : Component
		{
			return CountSpawned(prefab.gameObject);
		}

		public static int CountSpawned(GameObject prefab)
		{
			int count = 0;
			foreach (var instancePrefab in Instance.spawnedObjects.Values)
			{
				if (prefab == instancePrefab)
				{
					++count;
				}
			}

			return count;
		}

		public static int CountAllPooled()
		{
			int count = 0;
			foreach (var list in Instance.pooledObjects.Values)
			{
				count += list.Count;
			}

			return count;
		}

		public static List<GameObject> GetPooled(GameObject prefab, List<GameObject> list, bool appendList)
		{
			if (list == null)
			{
				list = new List<GameObject>();
			}

			if (!appendList)
			{
				list.Clear();
			}

			List<GameObject> pooled;
			if (Instance.pooledObjects.TryGetValue(prefab, out pooled))
			{
				list.AddRange(pooled);
			}

			return list;
		}

		public static List<T> GetPooled<T>(T prefab, List<T> list, bool appendList) where T : Component
		{
			if (list == null)
			{
				list = new List<T>();
			}

			if (!appendList)
			{
				list.Clear();
			}

			List<GameObject> pooled;
			if (Instance.pooledObjects.TryGetValue(prefab.gameObject, out pooled))
			{
				for (int i = 0; i < pooled.Count; ++i)
				{
					list.Add(pooled[i].GetComponent<T>());
				}
			}

			return list;
		}

		public static List<GameObject> GetSpawned(GameObject prefab, List<GameObject> list, bool appendList)
		{
			if (list == null)
			{
				list = new List<GameObject>();
			}

			if (!appendList)
			{
				list.Clear();
			}

			foreach (var item in Instance.spawnedObjects)
			{
				if (item.Value == prefab)
				{
					list.Add(item.Key);
				}
			}

			return list;
		}

		public static List<T> GetSpawned<T>(T prefab, List<T> list, bool appendList) where T : Component
		{
			if (list == null)
			{
				list = new List<T>();
			}

			if (!appendList)
			{
				list.Clear();
			}

			var prefabObj = prefab.gameObject;
			foreach (var item in Instance.spawnedObjects)
			{
				if (item.Value == prefabObj)
				{
					list.Add(item.Key.GetComponent<T>());
				}
			}

			return list;
		}
	}

	public static class ObjectPoolExtensions
	{
		public static void CreatePool<T>(this T prefab) where T : Component
		{
			ObjectPool.CreatePool(prefab, 0);
		}

		public static void CreatePool<T>(this T prefab, int initialPoolSize) where T : Component
		{
			ObjectPool.CreatePool(prefab, initialPoolSize);
		}

		public static void CreatePool(this GameObject prefab)
		{
			ObjectPool.CreatePool(prefab, 0);
		}

		public static void CreatePool(this GameObject prefab, int initialPoolSize)
		{
			ObjectPool.CreatePool(prefab, initialPoolSize);
		}

		public static T Spawn<T>(this T prefab, Transform parent, Vector3 position, Quaternion rotation) where T : Component
		{
			return ObjectPool.Spawn(prefab, parent, position, rotation);
		}

		public static T Spawn<T>(this T prefab, Vector3 position, Quaternion rotation) where T : Component
		{
			return ObjectPool.Spawn(prefab, null, position, rotation);
		}

		public static T Spawn<T>(this T prefab, Transform parent, Vector3 position) where T : Component
		{
			return ObjectPool.Spawn(prefab, parent, position, Quaternion.identity);
		}

		public static T Spawn<T>(this T prefab, Vector3 position) where T : Component
		{
			return ObjectPool.Spawn(prefab, null, position, Quaternion.identity);
		}

		public static T Spawn<T>(this T prefab, Transform parent) where T : Component
		{
			return ObjectPool.Spawn(prefab, parent, Vector3.zero, Quaternion.identity);
		}

		public static T Spawn<T>(this T prefab) where T : Component
		{
			return ObjectPool.Spawn(prefab, null, Vector3.zero, Quaternion.identity);
		}

		public static GameObject Spawn(this GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
		{
			return ObjectPool.Spawn(prefab, parent, position, rotation);
		}

		public static GameObject Spawn(this GameObject prefab, Vector3 position, Quaternion rotation)
		{
			return ObjectPool.Spawn(prefab, null, position, rotation);
		}

		public static GameObject Spawn(this GameObject prefab, Transform parent, Vector3 position)
		{
			return ObjectPool.Spawn(prefab, parent, position, Quaternion.identity);
		}

		public static GameObject Spawn(this GameObject prefab, Vector3 position)
		{
			return ObjectPool.Spawn(prefab, null, position, Quaternion.identity);
		}

		public static GameObject Spawn(this GameObject prefab, Transform parent)
		{
			return ObjectPool.Spawn(prefab, parent, Vector3.zero, Quaternion.identity);
		}

		public static GameObject Spawn(this GameObject prefab)
		{
			return ObjectPool.Spawn(prefab, null, Vector3.zero, Quaternion.identity);
		}

		public static void Recycle<T>(this T obj) where T : Component
		{
			ObjectPool.Recycle(obj);
		}

		public static void Recycle(this GameObject obj)
		{
			ObjectPool.Recycle(obj);
		}

		public static void RecycleAll<T>(this T prefab) where T : Component
		{
			ObjectPool.RecycleAll(prefab);
		}

		public static void RecycleAll(this GameObject prefab)
		{
			ObjectPool.RecycleAll(prefab);
		}

		public static int CountPooled<T>(this T prefab) where T : Component
		{
			return ObjectPool.CountPooled(prefab);
		}

		public static int CountPooled(this GameObject prefab)
		{
			return ObjectPool.CountPooled(prefab);
		}

		public static int CountSpawned<T>(this T prefab) where T : Component
		{
			return ObjectPool.CountSpawned(prefab);
		}

		public static int CountSpawned(this GameObject prefab)
		{
			return ObjectPool.CountSpawned(prefab);
		}

		public static List<GameObject> GetSpawned(this GameObject prefab, List<GameObject> list, bool appendList)
		{
			return ObjectPool.GetSpawned(prefab, list, appendList);
		}

		public static List<GameObject> GetSpawned(this GameObject prefab, List<GameObject> list)
		{
			return ObjectPool.GetSpawned(prefab, list, false);
		}

		public static List<GameObject> GetSpawned(this GameObject prefab)
		{
			return ObjectPool.GetSpawned(prefab, null, false);
		}

		public static List<T> GetSpawned<T>(this T prefab, List<T> list, bool appendList) where T : Component
		{
			return ObjectPool.GetSpawned(prefab, list, appendList);
		}

		public static List<T> GetSpawned<T>(this T prefab, List<T> list) where T : Component
		{
			return ObjectPool.GetSpawned(prefab, list, false);
		}

		public static List<T> GetSpawned<T>(this T prefab) where T : Component
		{
			return ObjectPool.GetSpawned(prefab, null, false);
		}

		public static List<GameObject> GetPooled(this GameObject prefab, List<GameObject> list, bool appendList)
		{
			return ObjectPool.GetPooled(prefab, list, appendList);
		}

		public static List<GameObject> GetPooled(this GameObject prefab, List<GameObject> list)
		{
			return ObjectPool.GetPooled(prefab, list, false);
		}

		public static List<GameObject> GetPooled(this GameObject prefab)
		{
			return ObjectPool.GetPooled(prefab, null, false);
		}

		public static List<T> GetPooled<T>(this T prefab, List<T> list, bool appendList) where T : Component
		{
			return ObjectPool.GetPooled(prefab, list, appendList);
		}

		public static List<T> GetPooled<T>(this T prefab, List<T> list) where T : Component
		{
			return ObjectPool.GetPooled(prefab, list, false);
		}

		public static List<T> GetPooled<T>(this T prefab) where T : Component
		{
			return ObjectPool.GetPooled(prefab, null, false);
		}
	}
}