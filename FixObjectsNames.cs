using System.IO;
using UnityEngine;
using UnityEditor;

public static class FixObjectsNames
{
    [MenuItem("Tools/More/Fix Objects Names")]
    public static void FindAndFixAssets()
    {
        string dialogTitle =
            ObjectNames.NicifyVariableName(nameof(FixObjectsNames));

        int dialogResult =
            EditorUtility.DisplayDialogComplex(
                dialogTitle,
                message: "Find and fix assets with incorrect main object " +
                    "names?\n\nThis loads every asset in the project and " +
                    "can be very slow in large projects.",
                ok: "Dry Run",
                cancel: "Cancel",
                alt: "Fix All"
            );

        const int DialogResultOK = 0;
        const int DialogResultCancel = 1;

        if (dialogResult == DialogResultCancel)
        {
            return;
        }

        bool isDryRun = dialogResult == DialogResultOK;
        if (isDryRun)
        {
            dialogTitle += " (Dry Run)";
        }

        string[] allGuidsInProject = AssetDatabase.FindAssets("");

        System.Type[] assetTypeBlacklist = new[]
        {
            typeof(Shader),
            typeof(DefaultAsset),  
        };

        bool didCancel = false;

        try
        {
            int assetCount = allGuidsInProject.Length;
            Debug.Log($"Starting scan over {assetCount} assets...");
            for (int i = 0; i < assetCount; i++)
            {
                string assetGuid = allGuidsInProject[i];
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

                Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                string mainObjectName = asset.name;
                string expectedMainObjectName = Path.GetFileNameWithoutExtension(assetPath);

                didCancel =
                    EditorUtility.DisplayCancelableProgressBar(
                        dialogTitle + $" - {i + 1} of {assetCount}",
                        info: assetPath,
                        progress: (float)(i + 1) / assetCount
                    );

                if (didCancel)
                {
                    break;
                }

                int blacklistedTypeIndex =
                    System.Array.IndexOf(assetTypeBlacklist, asset.GetType());

                if (blacklistedTypeIndex >= 0)
                {
                    continue;
                }

                if (mainObjectName == expectedMainObjectName)
                {
                    continue;
                }

                if (isDryRun)
                {
                    Debug.Log("Would fix: " + assetPath, asset);
                }
                else
                {
                    Undo.RecordObject(asset, dialogTitle);
                    using SerializedObject serializedAsset = new SerializedObject(asset);
                    asset.name = expectedMainObjectName;
                    Debug.Log("Fixed: " + assetPath, asset);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();

            Debug.Log(didCancel ? "Canceled." : "Finished.");
        }
    }
}	