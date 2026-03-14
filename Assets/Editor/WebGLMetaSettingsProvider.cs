using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WebGLMetaSettingsProvider : SettingsProvider
{
    private WebGLMetaConfig _config;
    private bool _loaded;
    private const string PrefKeyWelcomeShown = "WebGLMetaConfig.WelcomeShown";

    public static bool WelcomePromptAlreadyShown
    {
        get => EditorPrefs.GetBool(PrefKeyWelcomeShown, false);
        set => EditorPrefs.SetBool(PrefKeyWelcomeShown, value);
    }

    public WebGLMetaSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
        : base(path, scopes, keywords) { }

    public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
    {
        _loaded = false;
    }

    public override void OnGUI(string searchContext)
    {
        if (!_loaded)
        {
            _config = WebGLMetaConfigStorage.Load() ?? new WebGLMetaConfig
            {
                siteUrl = "https://example.com/your-game",
                ogImageUrl = "https://example.com/og-image.png",
                metaDescription = "A short description for search and social sharing.",
                ogImageAlt = "Screenshot or key art for the game",
                appDescription = "Short description for the PWA manifest (e.g. same as metaDescription)."
            };
            _loaded = true;
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "These values are written into the WebGL build (index.html and manifest.json) for social sharing and SEO. " +
            "Company name in the page title comes from Edit → Project Settings → Player → Company Name.",
            MessageType.Info);
        EditorGUILayout.Space(4);

        _config.siteUrl = EditorGUILayout.TextField(new GUIContent("Site URL", "Canonical URL of the game page (og:url)."), _config.siteUrl);
        _config.ogImageUrl = EditorGUILayout.TextField(new GUIContent("Share Image URL", "Full URL to the image used when shared (og:image, twitter:image)."), _config.ogImageUrl);
        _config.ogImageAlt = EditorGUILayout.TextField(new GUIContent("Share Image Alt Text", "Alt text for the share image."), _config.ogImageAlt);
        _config.metaDescription = EditorGUILayout.TextField(new GUIContent("Meta Description", "Short description for search and social."), _config.metaDescription);
        _config.appDescription = EditorGUILayout.TextField(new GUIContent("App Description (PWA)", "Description in the Web App Manifest."), _config.appDescription);

        EditorGUILayout.Space(12);
        if (GUILayout.Button("Save", GUILayout.Height(28)))
        {
            WebGLMetaConfigStorage.Save(_config);
            Debug.Log("WebGL meta config saved.");
        }
    }

    [SettingsProvider]
    public static SettingsProvider CreateProvider()
    {
        return new WebGLMetaSettingsProvider("Project/WebGL Meta", SettingsScope.Project,
            new HashSet<string> { "WebGL", "meta", "og", "twitter", "share", "SEO" });
    }

    [MenuItem("Edit/WebGL Meta Settings", false, 500)]
    public static void OpenFromMenu()
    {
        SettingsService.OpenProjectSettings("Project/WebGL Meta");
    }
}
