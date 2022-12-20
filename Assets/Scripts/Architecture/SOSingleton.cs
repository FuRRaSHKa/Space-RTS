using System;
using System.Reflection;
using UnityEngine;

public abstract class SOSingleton : ScriptableObject
{
    private const string GlobalFolder = "Singletons/";
    protected static string GetPath(Type type) => GlobalFolder + GetName(type);
    protected static string GetName(Type type) => type.Name;

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Create Missing Singleton Assets")]   
    private static void CreateMissingSingletonAssets()
    {
        var baseType = typeof(SOSingleton);
        foreach (var type in Assembly.GetAssembly(baseType).GetTypes())
            if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(baseType))
            {
                var path = GetPath(type);
                if (!Resources.Load(path))
                {
                    Debug.Log($"Creating {path}");
                    var asset = CreateInstance(type) as UnityEngine.Object;
                    asset.name = GetName(type);
                    UnityEditor.AssetDatabase.CreateAsset(asset, $"Assets/Resources/{path}.asset");
                    UnityEditor.AssetDatabase.SaveAssets();
                }
            }
    }
#endif
}

public abstract class SOSingleton<TSelf> : SOSingleton
  where TSelf : SOSingleton<TSelf>
{
    private static TSelf _instance;
 
    public static TSelf Instance
    {
        get
        {
            if (!_instance)
                _instance = Resources.Load<TSelf>(GetPath(typeof(TSelf)));

            return _instance;
        }
    }
}
