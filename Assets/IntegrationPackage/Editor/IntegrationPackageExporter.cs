using System.Collections.Generic;
using System.IO;
using PartnerIntegration;
using UnityEditor;
using UnityEngine;

namespace PartnerIntegrationEditor
{
    public static class IntegrationPackageExporter
    {
        private const string SettingsAssetPath = "Assets/IntegrationPackage/Resources/IntegrationPackage/IntegrationSettings.asset";
        private const string BootstrapPrefabPath = "Assets/IntegrationPackage/Prefabs/IntegrationBootstrap.prefab";
        private const string ExportRelativePath = "Export/IntegrationPackage.unitypackage";

        private static readonly string[] PackageRoots =
        {
            "Assets/IntegrationPackage",
            "Assets/_Project",
            "Assets/Adjust",
            "Assets/GoogleMobileAds",
            "Assets/MaxSdk",
            "Assets/Plugins"
        };

        [MenuItem("Tools/Integration Package/Create Default Settings")]
        public static IntegrationSettings CreateDefaultSettingsAsset()
        {
            var settings = AssetDatabase.LoadAssetAtPath<IntegrationSettings>(SettingsAssetPath);
            if (settings != null)
            {
                Selection.activeObject = settings;
                return settings;
            }

            EnsureParentDirectory(SettingsAssetPath);
            settings = ScriptableObject.CreateInstance<IntegrationSettings>();
            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = settings;
            Debug.Log("[IntegrationPackage] Created settings asset: " + SettingsAssetPath);
            return settings;
        }

        [MenuItem("Tools/Integration Package/Create Bootstrap Prefab")]
        public static GameObject CreateBootstrapPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BootstrapPrefabPath);
            if (prefab != null)
            {
                Selection.activeObject = prefab;
                return prefab;
            }

            EnsureParentDirectory(BootstrapPrefabPath);
            var settings = CreateDefaultSettingsAsset();
            var gameObject = new GameObject("IntegrationBootstrap");
            var bootstrap = gameObject.AddComponent<IntegrationBootstrap>();
            var serializedObject = new SerializedObject(bootstrap);
            serializedObject.FindProperty("settings").objectReferenceValue = settings;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, BootstrapPrefabPath);
            Object.DestroyImmediate(gameObject);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = prefab;
            Debug.Log("[IntegrationPackage] Created bootstrap prefab: " + BootstrapPrefabPath);
            return prefab;
        }

        [MenuItem("Tools/Integration Package/Export UnityPackage")]
        public static void ExportUnityPackage()
        {
            CreateDefaultSettingsAsset();
            CreateBootstrapPrefab();
            AssetDatabase.Refresh();

            var exportPaths = new List<string>();
            for (var i = 0; i < PackageRoots.Length; i++)
            {
                if (AssetDatabase.IsValidFolder(PackageRoots[i]) || File.Exists(PackageRoots[i]))
                {
                    exportPaths.Add(PackageRoots[i]);
                }
            }

            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var exportPath = Path.Combine(projectRoot, ExportRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(exportPath));

            AssetDatabase.ExportPackage(exportPaths.ToArray(), exportPath, ExportPackageOptions.Recurse);
            Debug.Log("[IntegrationPackage] Exported package: " + exportPath);
        }

        private static void EnsureParentDirectory(string assetPath)
        {
            var parent = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }
        }
    }
}
