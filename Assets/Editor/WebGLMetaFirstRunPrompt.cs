using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

[InitializeOnLoad]
public static class WebGLMetaFirstRunPrompt
{
    private const int DialogResultOpen = 0;
    private const int DialogResultLater = 1;
    private const int DialogResultDontShow = 2;

    static WebGLMetaFirstRunPrompt()
    {
        EditorApplication.delayCall += MaybeShowWelcome;
    }

    private static void MaybeShowWelcome()
    {
        if (WebGLMetaSettingsProvider.WelcomePromptAlreadyShown)
            return;
        if (WebGLMetaConfigStorage.ExistsAndIsCustomized())
            return;

        WebGLMetaSettingsProvider.WelcomePromptAlreadyShown = true;

        int result = EditorUtility.DisplayDialogComplex(
            "WebGL meta tags",
            "Set up meta tags and share image URL for your WebGL build? You can change these anytime under Edit → Project Settings → WebGL Meta.",
            "Open Settings",
            "Later",
            "Don't show again");

        switch (result)
        {
            case DialogResultOpen:
                SettingsService.OpenProjectSettings("Project/WebGL Meta");
                break;
            case DialogResultLater:
                WebGLMetaSettingsProvider.WelcomePromptAlreadyShown = false;
                break;
            case DialogResultDontShow:
                break;
        }
    }
}
