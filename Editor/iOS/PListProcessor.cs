#if UNITY_EDITOR && UNITY_IOS
using UnityEditor;
using UnityEditor.iOS.Xcode;

using UnityEditor.Build;
using UnityEditor.Build.Reporting;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class PListProcessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 99999;

    public void OnPostprocessBuild(BuildReport report)
    {
        string projectBundleId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS);
        PlistDocument[] plistFiles = AssetDatabase.FindAssets("glob:\"**/*.plist\"").Select((guid) =>
        {
            var doc = new PlistDocument();
            doc.ReadFromFile(AssetDatabase.GUIDToAssetPath(guid));
            return doc;
        }).Where((doc) =>
        {
            return doc.root.values.TryGetValue("BUNDLE_ID", out PlistElement element) && element.AsString() == projectBundleId;
        }).ToArray();

        if (!(plistFiles?.Length > 0))
            return;

        PlistDocument google = plistFiles.FirstOrDefault();

        if (!(google.root.values.TryGetValue("CLIENT_ID", out PlistElement CLIENT_ID) && CLIENT_ID?.AsString() is string clientID && clientID.EndsWith("googleusercontent.com")))
            throw new KeyNotFoundException("CLIENT_ID");
        if (!(google.root.values.TryGetValue("REVERSED_CLIENT_ID", out PlistElement REVERSED_CLIENT_ID) && REVERSED_CLIENT_ID?.AsString() is string reversedClientID && reversedClientID.StartsWith("com.googleusercontent")))
            throw new KeyNotFoundException("REVERSED_CLIENT_ID");

        string plistPath = Path.Combine(report.summary.outputPath, "Info.plist");

        PlistDocument info = new PlistDocument();
        info.ReadFromFile(plistPath);

        info.root.SetString("GIDClientID", clientID);
        PlistElementArray CFBundleURLTypes = (info.root.values.TryGetValue("CFBundleURLTypes", out PlistElement element) ? element.AsArray() : null) ?? info.root.CreateArray("CFBundleURLTypes");
        if (!(CFBundleURLTypes?.values?.Count > 0 && CFBundleURLTypes.values.OfType<PlistElementDict>().Select((dict) => dict.values.TryGetValue("CFBundleURLSchemes", out var value) ? value?.AsArray() : null).OfType<PlistElementArray>().SelectMany((array) => array.values).Any((item) => item?.AsString() == reversedClientID)))
        {
            PlistElementDict dict = CFBundleURLTypes.AddDict();
            dict.SetString("CFBundleTypeRole", "Editor");
            dict.CreateArray("CFBundleURLSchemes").AddString(reversedClientID);
        }

        if (google.root.values.TryGetValue("WEB_CLIENT_ID", out var WEB_CLIENT_ID) && WEB_CLIENT_ID?.AsString() is string webClientID && !string.IsNullOrWhiteSpace(webClientID))
        {
            if (webClientID.EndsWith("googleusercontent.com"))
                info.root.SetString("GIDServerClientID", webClientID);
            else throw new ArgumentException("WebClientID");
        }

        info.WriteToFile(plistPath);
    }
}
#endif