using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDKBase.Editor.Api;
using VRC.SDKBase.Editor.BuildPipeline;
using VRC.SDK3A.Editor;

[InitializeOnLoad]
public class AutoCopyVRCAvatars : MonoBehaviour
{
    private const string mEditorPrefsKey = "AutoCopyVRCAvatars_DestPath";

    private static string DefaultPath
    {
        get
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(userProfile, "AppData", "LocalLow", "VRChat", "VRChat", "Avatars");
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor)
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                return Path.Combine(home, ".steam/steam/steamapps/compatdata/438100/pfx/drive_c/users/steamuser/AppData/LocalLow/VRChat/VRChat/Avatars");
            }
            else
            {
                Debug.LogWarning("AutoCopyVRCAvatars: Unsupported platform for default path.");
                return "";
            }
        }
    }


    private static string mDestDir => EditorPrefs.GetString(mEditorPrefsKey, DefaultPath);

    static AutoCopyVRCAvatars()
    {
        VRCSdkControlPanel.OnSdkPanelEnable += AddBuildHook;
    }

    private static void AddBuildHook(object sender, EventArgs e)
    {
        if (VRCSdkControlPanel.TryGetBuilder<IVRCSdkAvatarBuilderApi>(out var builder))
        {
            builder.OnSdkBuildStart -= OnBuildStarted;
            builder.OnSdkBuildSuccess -= OnBuildSuccess;

            builder.OnSdkBuildStart += OnBuildStarted;
            builder.OnSdkBuildSuccess += OnBuildSuccess;
        }
    }

    private static GameObject mLastBuiltAvatar;

    private static void OnBuildStarted(object sender, object target)
    {
        if (target is GameObject go)
        {
            mLastBuiltAvatar = go;
            Debug.Log("Build started for: " + go.name);
        }
    }

    private static void OnBuildSuccess(object sender, object bundlePathObj)
    {
        if (bundlePathObj is string bundlePath && mLastBuiltAvatar != null)
        {
            string destPath = Path.Combine(mDestDir, mLastBuiltAvatar.name + ".vrca");

            try
            {
                if (!Directory.Exists(mDestDir))
                    Directory.CreateDirectory(mDestDir);

                File.Copy(bundlePath, destPath, true);
                Debug.Log($"Copied .vrca to: {destPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to copy bundle: " + ex);
            }
        }
        else
        {
            Debug.LogWarning("Build succeeded but target or path was null.");
        }

        mLastBuiltAvatar = null;
    }

    [MenuItem("Tools/Auto Avatar Copy Settings...")]
    private static void OpenSettings()
    {
        AutoCopySettingsWindow.ShowWindow();
    }

    public class AutoCopySettingsWindow : EditorWindow
    {
        private string newPath;

        public static void ShowWindow()
        {
            var window = GetWindow<AutoCopySettingsWindow>("Auto Copy VRCA Settings");
            window.minSize = new Vector2(400, 80);
            window.Show();
        }

        private void OnEnable()
        {
            newPath = mDestDir;
        }

        private void OnGUI()
        {
            GUILayout.Label("Set Destination Folder for Copied VRCA Files", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            newPath = EditorGUILayout.TextField("Output Path", newPath);

            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Output Folder", newPath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    newPath = selected;
                }
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (GUILayout.Button("Save", GUILayout.Height(30)))
            {
                EditorPrefs.SetString(mEditorPrefsKey, newPath);
                Debug.Log("AutoCopyVRCAvatars path set to: " + newPath);
                Close();
            }
        }
    }
}
