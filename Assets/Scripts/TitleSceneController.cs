using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TitleSceneController : MonoBehaviour
{
    private const string HighScoreKey = "HighScore";
    private static readonly Color Night = new Color(0.01f, 0.015f, 0.035f);

    private void Start()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.20f, 0.24f, 0.36f);
        RenderSettings.ambientEquatorColor = new Color(0.10f, 0.11f, 0.16f);
        RenderSettings.ambientGroundColor = new Color(0.04f, 0.04f, 0.05f);

        var cameraObject = new GameObject("Title Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Night;
        camera.transform.position = new Vector3(0f, 4f, -10f);
        camera.transform.LookAt(Vector3.up * 2f);

        CreateTitleLights();
        CreatePreviewLauncher();
        CreateCanvas();
    }

    private static void CreateTitleLights()
    {
        var moon = new GameObject("Moon Light").AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = new Color(0.70f, 0.76f, 1f);
        moon.intensity = 1.1f;
        moon.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

        var fill = new GameObject("Launcher Fill Light").AddComponent<Light>();
        fill.type = LightType.Point;
        fill.color = new Color(1f, 0.72f, 0.42f);
        fill.intensity = 4.5f;
        fill.range = 14f;
        fill.transform.position = new Vector3(0f, 2.2f, -1.6f);
    }

    private static void CreatePreviewLauncher()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(4f, 1f, 4f);
        ground.GetComponent<Renderer>().material = MakeMaterial(new Color(0.06f, 0.08f, 0.12f), 0.08f);

        var baseObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObject.name = "Launcher Base";
        baseObject.transform.position = new Vector3(0f, 0.18f, 0f);
        baseObject.transform.localScale = new Vector3(1.8f, 0.36f, 1.8f);
        baseObject.GetComponent<Renderer>().material = MakeMaterial(new Color(0.23f, 0.27f, 0.34f), 0.35f);

        var tube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tube.name = "Launcher Tube";
        tube.transform.position = new Vector3(0f, 1.25f, 0f);
        tube.transform.rotation = Quaternion.Euler(0f, 0f, -8f);
        tube.transform.localScale = new Vector3(0.42f, 1.45f, 0.42f);
        tube.GetComponent<Renderer>().material = MakeMaterial(new Color(0.46f, 0.54f, 0.64f), 0.45f);
    }

    private static void CreateCanvas()
    {
        var canvasObject = new GameObject("Title Canvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();

        var panel = CreatePanel(canvasObject.transform, new Vector2(0.24f, 0.14f), new Vector2(0.76f, 0.86f), "Title Menu Panel");
        var highScore = PlayerPrefs.GetInt(HighScoreKey, 0);

        CreateText(panel.transform, "花火打ち上げゲーム", 36, FontStyle.Bold, 52f, TextAnchor.MiddleCenter, new Color(0.95f, 0.97f, 1f));
        CreateText(panel.transform, "打ち上げ予定を読み、\n正しい色を装填して花火を上げよう。", 17, FontStyle.Normal, 58f, TextAnchor.MiddleCenter, new Color(0.82f, 0.86f, 0.92f));
        CreateSeparator(panel.transform);
        CreateText(panel.transform, "HIGH SCORE", 18, FontStyle.Bold, 28f, TextAnchor.MiddleCenter, Color.white);
        CreateText(panel.transform, highScore.ToString(), 48, FontStyle.Bold, 62f, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.31f));
        CreateSeparator(panel.transform);
        CreateButton(panel.transform, "START", () => SceneManager.LoadScene("RuleScene"));
        CreateButton(panel.transform, "QUIT", QuitApplication);
    }

    private static GameObject CreatePanel(Transform parent, Vector2 anchorMin, Vector2 anchorMax, string name)
    {
        var panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);
        var rect = panelObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = panelObject.AddComponent<Image>();
        image.color = new Color(0.059f, 0.090f, 0.165f, 0.86f);
        var outline = panelObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.36f, 0.42f, 0.54f, 0.58f);
        outline.effectDistance = new Vector2(2f, -2f);

        var layout = panelObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 42, 34);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return panelObject;
    }

    private static void CreateSeparator(Transform parent)
    {
        var separator = new GameObject("Separator");
        separator.transform.SetParent(parent, false);
        var rect = separator.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 1f);
        var image = separator.AddComponent<Image>();
        image.color = new Color(0.36f, 0.42f, 0.54f, 0.44f);
        AddLayoutElement(separator, 1f);
    }

    private static Text CreateText(Transform parent, string value, int size, FontStyle style, Vector2 position, Vector2 dimensions)
    {
        return CreateText(parent, value, size, style, position, dimensions, new Color(0.92f, 0.95f, 1f));
    }

    private static Text CreateText(Transform parent, string value, int size, FontStyle style, float preferredHeight, TextAnchor alignment, Color color)
    {
        var text = CreateText(parent, value, size, style, Vector2.zero, Vector2.zero, color);
        text.alignment = alignment;
        AddLayoutElement(text.gameObject, preferredHeight);
        return text;
    }

    private static Text CreateText(Transform parent, string value, int size, FontStyle style, Vector2 position, Vector2 dimensions, Color color)
    {
        var textObject = new GameObject(value);
        textObject.transform.SetParent(parent, false);
        var rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        var text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.lineSpacing = 1.08f;
        return text;
    }

    private static Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = new GameObject(label + " Button");
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(320f, 56f);
        AddLayoutElement(buttonObject, 56f, 320f);

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.118f, 0.165f, 0.267f, 0.98f);
        var outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.36f, 0.42f, 0.54f, 0.56f);
        outline.effectDistance = new Vector2(1f, -1f);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = new Color(0.118f, 0.165f, 0.267f, 0.98f);
        colors.highlightedColor = new Color(0.13f, 0.18f, 0.29f, 1f);
        colors.pressedColor = new Color(0.08f, 0.11f, 0.18f, 1f);
        colors.selectedColor = colors.normalColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(action);

        CreateText(buttonObject.transform, label, 21, FontStyle.Bold, Vector2.zero, rect.sizeDelta, Color.white);
        return button;
    }

    private static void AddLayoutElement(GameObject target, float preferredHeight, float preferredWidth = -1f)
    {
        var layoutElement = target.AddComponent<LayoutElement>();
        layoutElement.minHeight = preferredHeight;
        layoutElement.preferredHeight = preferredHeight;
        if (preferredWidth > 0f)
        {
            layoutElement.minWidth = preferredWidth;
            layoutElement.preferredWidth = preferredWidth;
        }
    }

    private static Material MakeMaterial(Color color, float metallic)
    {
        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Glossiness", 0.28f);
        return material;
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null)
        {
            return;
        }

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    private static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
