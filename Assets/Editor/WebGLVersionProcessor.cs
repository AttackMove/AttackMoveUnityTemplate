using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

[Serializable]
public class WebGLMetaConfig
{
    public string siteUrl;
    public string ogImageUrl;
    public string metaDescription;
    public string ogImageAlt;
    public string appDescription;
}

public static class WebGLMetaConfigStorage
{
    public const string MetaConfigPath = "WebGLTemplates/AttackMoveTemplate/webgl-meta-config.json";

    public static string GetConfigFilePath()
    {
        return Path.Combine(Application.dataPath, MetaConfigPath);
    }

    public static WebGLMetaConfig Load()
    {
        string path = GetConfigFilePath();
        if (!File.Exists(path))
            return null;
        try
        {
            return JsonUtility.FromJson<WebGLMetaConfig>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }

    public static void Save(WebGLMetaConfig config)
    {
        if (config == null) return;
        string path = GetConfigFilePath();
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, JsonUtility.ToJson(config, true));
        AssetDatabase.Refresh();
    }

    public static bool ExistsAndIsCustomized()
    {
        var c = Load();
        if (c == null) return false;
        return !string.IsNullOrWhiteSpace(c.siteUrl) &&
               c.siteUrl.IndexOf("example.com", StringComparison.OrdinalIgnoreCase) < 0;
    }
}

public class WebGLVersionProcessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.WebGL)
            return;

        string buildPath = report.summary.outputPath;
        string indexPath = Path.Combine(buildPath, "index.html");

        if (!File.Exists(indexPath))
        {
            Debug.LogWarning($"WebGLVersionProcessor: index.html not found at {indexPath}");
            return;
        }

        string version = GenerateBuildVersion();
        WebGLMetaConfig metaConfig = WebGLMetaConfigStorage.Load();

        try
        {
            string content = File.ReadAllText(indexPath);
            content = content.Replace("BUILD_VERSION", version);
            ApplyMetaConfig(ref content, metaConfig);
            File.WriteAllText(indexPath, content);

            string manifestPath = Path.Combine(buildPath, "manifest.json");
            if (File.Exists(manifestPath) && metaConfig != null)
            {
                string manifestContent = File.ReadAllText(manifestPath);
                manifestContent = manifestContent.Replace("YOUR_APP_DESCRIPTION", metaConfig.appDescription ?? "");
                File.WriteAllText(manifestPath, manifestContent);
            }

            Debug.Log($"WebGLVersionProcessor: Updated version to {version} in {indexPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"WebGLVersionProcessor: Failed to update build: {ex.Message}");
        }
    }

    private static void ApplyMetaConfig(ref string content, WebGLMetaConfig config)
    {
        if (config == null) return;

        content = content.Replace("YOUR_SITE_URL", config.siteUrl ?? "");
        content = content.Replace("YOUR_OG_IMAGE_URL", config.ogImageUrl ?? "");
        content = content.Replace("YOUR_META_DESCRIPTION", config.metaDescription ?? "");
        content = content.Replace("YOUR_OG_IMAGE_ALT", config.ogImageAlt ?? "");
    }

    private string GenerateBuildVersion()
    {
        // Generate a unique version based on timestamp and a random component
        // Format: timestamp + random string (base36 encoded for shorter format)
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string random = Guid.NewGuid().ToString("N").Substring(0, 8);
        return $"{timestamp}_{random}";
    }
}

