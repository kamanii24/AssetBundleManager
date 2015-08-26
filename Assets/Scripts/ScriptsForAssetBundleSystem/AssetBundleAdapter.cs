using UnityEngine;
#if UNITY_EDITOR	
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;

/*
 	In this demo, we demonstrate:
	1.	Automatic asset bundle dependency resolving & loading.
		It shows how to use the manifest assetbundle like how to get the dependencies etc.
	2.	Automatic unloading of asset bundles (When an asset bundle or a dependency thereof is no longer needed, the asset bundle is unloaded)
	3.	Editor simulation. A bool defines if we load asset bundles from the project or are actually using asset bundles(doesn't work with assetbundle variants for now.)
		With this, you can player in editor mode without actually building the assetBundles.
	4.	Optional setup where to download all asset bundles
	5.	Build pipeline build postprocessor, integration so that building a player builds the asset bundles and puts them into the player data (Default implmenetation for loading assetbundles from disk on any platform)
	6.	Use WWW.LoadFromCacheOrDownload and feed 128 bit hash to it when downloading via web
		You can get the hash from the manifest assetbundle.
	7.	AssetBundle variants. A prioritized list of variants that should be used if the asset bundle with that variant exists, first variant in the list is the most preferred etc.
*/

// Loaded assetBundle contains the references count which can be used to unload dependent assetBundles automatically.
public class LoadedAssetBundle
{
	public AssetBundle m_AssetBundle;
	public int m_ReferencedCount;
	
	public LoadedAssetBundle(AssetBundle assetBundle)
	{
		m_AssetBundle = assetBundle;
		m_ReferencedCount = 1;
	}
}

// Class takes care of loading assetBundle and its dependencies automatically, loading variants automatically.
public class AssetBundleAdapter : MonoBehaviour
{
	static string m_BaseDownloadingURL = "";
	static string[] m_Variants =  {  };
	static AssetBundleManifest m_AssetBundleManifest = null;
#if UNITY_EDITOR	
	static int m_SimulateAssetBundleInEditor = -1;
	const string kSimulateAssetBundles = "SimulateAssetBundles";
#endif

	static Dictionary<string, LoadedAssetBundle> m_LoadedAssetBundles = new Dictionary<string, LoadedAssetBundle> ();
	static Dictionary<string, WWW> m_DownloadingWWWs = new Dictionary<string, WWW> ();
	static Dictionary<string, string> m_DownloadingErrors = new Dictionary<string, string> ();
	static List<AssetBundleLoadOperation> m_InProgressOperations = new List<AssetBundleLoadOperation> ();
	static Dictionary<string, string[]> m_Dependencies = new Dictionary<string, string[]> ();

	// The base downloading url which is used to generate the full downloading url with the assetBundle names.
	public static string BaseDownloadingURL
	{
		get { return m_BaseDownloadingURL; }
		set { m_BaseDownloadingURL = value; }
	}

	// Variants which is used to define the active variants.
	public static string[] Variants
	{
		get { return m_Variants; }
		set { m_Variants = value; }
	}

	// AssetBundleManifest object which can be used to load the dependecies and check suitable assetBundle variants.
	public static AssetBundleManifest AssetBundleManifestObject
	{
		set {m_AssetBundleManifest = value; }
	}

#if UNITY_EDITOR
	// Flag to indicate if we want to simulate assetBundles in Editor without building them actually.
	public static bool SimulateAssetBundleInEditor 
	{
		get
		{
			if (m_SimulateAssetBundleInEditor == -1)
				m_SimulateAssetBundleInEditor = EditorPrefs.GetBool(kSimulateAssetBundles, true) ? 1 : 0;
			
			return m_SimulateAssetBundleInEditor != 0;
		}
		set
		{
			int newValue = value ? 1 : 0;
			if (newValue != m_SimulateAssetBundleInEditor)
			{
				m_SimulateAssetBundleInEditor = newValue;
				EditorPrefs.SetBool(kSimulateAssetBundles, value);
			}
		}
	}
#endif

	// Get loaded AssetBundle, only return vaild object when all the dependencies are downloaded successfully.
	static public LoadedAssetBundle GetLoadedAssetBundle (string assetBundleName, out string error)
	{
		if (m_DownloadingErrors.TryGetValue(assetBundleName, out error) )
			return null;
	
		LoadedAssetBundle bundle = null;
		m_LoadedAssetBundles.TryGetValue(assetBundleName, out bundle);
		if (bundle == null)
			return null;
		
		// No dependencies are recorded, only the bundle itself is required.
		string[] dependencies = null;
		if (!m_Dependencies.TryGetValue(assetBundleName, out dependencies) )
			return bundle;
		
		// Make sure all dependencies are loaded
		foreach(var dependency in dependencies)
		{
			if (m_DownloadingErrors.TryGetValue(assetBundleName, out error) )
				return bundle;

			// Wait all the dependent assetBundles being loaded.
			LoadedAssetBundle dependentBundle;
			m_LoadedAssetBundles.TryGetValue(dependency, out dependentBundle);
			if (dependentBundle == null)
				return null;
		}

		return bundle;
	}

	// Load AssetBundleManifest.
	static public AssetBundleLoadManifestOperation Initialize (string manifestAssetBundleName)
	{
		var go = new GameObject("AssetBundleAdapter", typeof(AssetBundleAdapter));
		DontDestroyOnLoad(go);
	
#if UNITY_EDITOR	
		// If we're in Editor simulation mode, we don't need the manifest assetBundle.
		if (SimulateAssetBundleInEditor)
			return null;
#endif

		LoadAssetBundle(manifestAssetBundleName, true);
		var operation = new AssetBundleLoadManifestOperation (manifestAssetBundleName, "AssetBundleManifest", typeof(AssetBundleManifest));
		m_InProgressOperations.Add (operation);
		return operation;
	}
	
	// Load AssetBundle and its dependencies.
	static protected void LoadAssetBundle(string assetBundleName, bool isLoadingAssetBundleManifest = false)
	{
#if UNITY_EDITOR
		// If we're in Editor simulation mode, we don't have to really load the assetBundle and its dependencies.
		if (SimulateAssetBundleInEditor)
			return;
#endif

		if (!isLoadingAssetBundleManifest)
			assetBundleName = RemapVariantName (assetBundleName);

		// Check if the assetBundle has already been processed.
		bool isAlreadyProcessed = LoadAssetBundleInternal(assetBundleName, isLoadingAssetBundleManifest);

		// Load dependencies.
		if (!isAlreadyProcessed && !isLoadingAssetBundleManifest)
			LoadDependencies(assetBundleName);
	}
	
	// Remaps the asset bundle name to the best fitting asset bundle variant.
	static protected string RemapVariantName(string assetBundleName)
	{
		string[] bundlesWithVariant = m_AssetBundleManifest.GetAllAssetBundlesWithVariant();

		// If the asset bundle doesn't have variant, simply return.
		if (System.Array.IndexOf(bundlesWithVariant, assetBundleName) < 0 )
			return assetBundleName;

		string[] split = assetBundleName.Split('.');

		int bestFit = int.MaxValue;
		int bestFitIndex = -1;
		// Loop all the assetBundles with variant to find the best fit variant assetBundle.
		for (int i = 0; i < bundlesWithVariant.Length; i++)
		{
			string[] curSplit = bundlesWithVariant[i].Split('.');
			if (curSplit[0] != split[0])
				continue;
			
			int found = System.Array.IndexOf(m_Variants, curSplit[1]);
			if (found != -1 && found < bestFit)
			{
				bestFit = found;
				bestFitIndex = i;
			}
		}

		if (bestFitIndex != -1)
			return bundlesWithVariant[bestFitIndex];
		else
			return assetBundleName;
	}

	// Where we actuall call WWW to download the assetBundle.
	static protected bool LoadAssetBundleInternal (string assetBundleName, bool isLoadingAssetBundleManifest)
	{
		// Already loaded.
		LoadedAssetBundle bundle = null;
		m_LoadedAssetBundles.TryGetValue(assetBundleName, out bundle);
		if (bundle != null)
		{
			bundle.m_ReferencedCount++;
			return true;
		}

		// @TODO: Do we need to consider the referenced count of WWWs?
		// In the demo, we never have duplicate WWWs as we wait LoadAssetAsync()/LoadLevelAsync() to be finished before calling another LoadAssetAsync()/LoadLevelAsync().
		// But in the real case, users can call LoadAssetAsync()/LoadLevelAsync() several times then wait them to be finished which might have duplicate WWWs.
		if (m_DownloadingWWWs.ContainsKey(assetBundleName) )
			return true;

		WWW download = null;
		string url = m_BaseDownloadingURL + assetBundleName;
		Debug.Log (url);

		// For manifest assetbundle, always download it as we don't have hash for it.
		if (isLoadingAssetBundleManifest)
			download = new WWW(url);
		else
			download = WWW.LoadFromCacheOrDownload(url, m_AssetBundleManifest.GetAssetBundleHash(assetBundleName), 0); 

		m_DownloadingWWWs.Add(assetBundleName, download);

		return false;
	}

	// Where we get all the dependencies and load them all.
	static protected void LoadDependencies(string assetBundleName)
	{
		if (m_AssetBundleManifest == null)
		{
			Debug.LogError("Please initialize AssetBundleManifest by calling AssetBundleAdapter.Initialize()");
			return;
		}

		// Get dependecies from the AssetBundleManifest object..
		string[] dependencies = m_AssetBundleManifest.GetAllDependencies(assetBundleName);
		if (dependencies.Length == 0)
			return;
			
		for (int i=0;i<dependencies.Length;i++)
			dependencies[i] = RemapVariantName (dependencies[i]);
			
		// Record and load all dependencies.
		m_Dependencies.Add(assetBundleName, dependencies);
		for (int i=0;i<dependencies.Length;i++)
			LoadAssetBundleInternal(dependencies[i], false);
	}

	// Unload assetbundle and its dependencies.
	static public void UnloadAssetBundle(string assetBundleName)
	{
#if UNITY_EDITOR
		// If we're in Editor simulation mode, we don't have to load the manifest assetBundle.
		if (SimulateAssetBundleInEditor)
			return;
#endif

		//Debug.Log(m_LoadedAssetBundles.Count + " assetbundle(s) in memory before unloading " + assetBundleName);

		UnloadAssetBundleInternal(assetBundleName);
		UnloadDependencies(assetBundleName);

		//Debug.Log(m_LoadedAssetBundles.Count + " assetbundle(s) in memory after unloading " + assetBundleName);
	}

	static protected void UnloadDependencies(string assetBundleName)
	{
		string[] dependencies = null;
		if (!m_Dependencies.TryGetValue(assetBundleName, out dependencies) )
			return;

		// Loop dependencies.
		foreach(var dependency in dependencies)
		{
			UnloadAssetBundleInternal(dependency);
		}

		m_Dependencies.Remove(assetBundleName);
	}

	static protected void UnloadAssetBundleInternal(string assetBundleName)
	{
		string error;
		LoadedAssetBundle bundle = GetLoadedAssetBundle(assetBundleName, out error);
		if (bundle == null)
			return;

		if (--bundle.m_ReferencedCount == 0)
		{
			bundle.m_AssetBundle.Unload(false);
			m_LoadedAssetBundles.Remove(assetBundleName);
			//Debug.Log("AssetBundle " + assetBundleName + " has been unloaded successfully");
		}
	}

	void Update()
	{
		// Collect all the finished WWWs.
		var keysToRemove = new List<string>();
		foreach (var keyValue in m_DownloadingWWWs)
		{
			WWW download = keyValue.Value;

			// If downloading fails.
			if (download.error != null)
			{
				m_DownloadingErrors.Add(keyValue.Key, download.error);
				keysToRemove.Add(keyValue.Key);
				continue;
			}

			// If downloading succeeds.
			if(download.isDone)
			{
				//Debug.Log("Downloading " + keyValue.Key + " is done at frame " + Time.frameCount);
				m_LoadedAssetBundles.Add(keyValue.Key, new LoadedAssetBundle(download.assetBundle) );
				keysToRemove.Add(keyValue.Key);
			}
		}

		// Remove the finished WWWs.
		foreach( var key in keysToRemove)
		{
			WWW download = m_DownloadingWWWs[key];
			m_DownloadingWWWs.Remove(key);
			download.Dispose();
		}

		// Update all in progress operations
		for (int i=0;i<m_InProgressOperations.Count;)
		{
			if (!m_InProgressOperations[i].Update())
			{
				m_InProgressOperations.RemoveAt(i);
			}
			else
				i++;
		}
	}

	// Load asset from the given assetBundle.
	static public AssetBundleLoadAssetOperation LoadAssetAsync (string assetBundleName, string assetName, System.Type type)
	{
		AssetBundleLoadAssetOperation operation = null;
#if UNITY_EDITOR
		if (SimulateAssetBundleInEditor)
		{
			string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(assetBundleName, assetName);
			if (assetPaths.Length == 0)
			{
				Debug.LogError("There is no asset with name \"" + assetName + "\" in " + assetBundleName);
				return null;
			}

			// @TODO: Now we only get the main object from the first asset. Should consider type also.
			Object target = AssetDatabase.LoadMainAssetAtPath(assetPaths[0]);
			operation = new AssetBundleLoadAssetOperationSimulation (target);
		}
		else
#endif
		{
			LoadAssetBundle (assetBundleName);
			operation = new AssetBundleLoadAssetOperationFull (assetBundleName, assetName, type);

			m_InProgressOperations.Add (operation);
		}

		return operation;
	}

	// Load level from the given assetBundle.
	static public AssetBundleLoadOperation LoadLevelAsync (string assetBundleName, string levelName, bool isAdditive)
	{
		AssetBundleLoadOperation operation = null;
#if UNITY_EDITOR
		if (SimulateAssetBundleInEditor)
		{
			string[] levelPaths = AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(assetBundleName, levelName);
			if (levelPaths.Length == 0)
			{
				///@TODO: The error needs to differentiate that an asset bundle name doesn't exist
				//        from that there right scene does not exist in the asset bundle...
			
				Debug.LogError("There is no scene with name \"" + levelName + "\" in " + assetBundleName);
				return null;
			}

			if (isAdditive)
				EditorApplication.LoadLevelAdditiveInPlayMode(levelPaths[0]);
			else
				EditorApplication.LoadLevelInPlayMode(levelPaths[0]);

			operation = new AssetBundleLoadLevelSimulationOperation();
		}
		else
#endif
		{
			LoadAssetBundle (assetBundleName);
			operation = new AssetBundleLoadLevelOperation (assetBundleName, levelName, isAdditive);

			m_InProgressOperations.Add (operation);
		}

		return operation;
	}
} // End of AssetBundleAdapter.