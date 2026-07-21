using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RuleSceneController : MonoBehaviour
{
    private static readonly Color Night = new Color(0.01f, 0.015f, 0.035f);

    private void Start()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.20f, 0.24f, 0.36f);
        RenderSettings.ambientEquatorColor = new Color(0.10f, 0.11f, 0.16f);
        RenderSettings.ambientGroundColor = new Color(0.04f, 0.04f, 0.05f);

        var cameraObject = new GameObject("Rule Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Night;
        camera.transform.position = new Vector3(0f, 4f, -10f);
        camera.transform.LookAt(Vector3.up * 2f);

        CreateRuleLights();
        CreatePreviewLauncher();
        CreateCanvas();
    }

    private static void CreateRuleLights()
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
        var canvasObject = new GameObject("Rule Canvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280f, 720f);
        canvasObject.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();

        var panel = CreatePanel(canvasObject.transform, new Vector2(0f, 30f), new Vector2(860f, 540f));
        CreateText(panel.transform, "遊び方", 30, FontStyle.Bold, new Vector2(0f, 226f), new Vector2(760f, 44f), TextAnchor.MiddleCenter, new Color(0.95f, 0.97f, 1f));

        CreateSection(panel.transform, "1. 色を装填する", "A/S/D/Fキーで、次に打ち上げる花火の色を選びます。", new Vector2(-220f, 150f), new Vector2(340f, 82f));
        CreateColorKey(panel.transform, "A", "赤", new Color(1f, 0.24f, 0.22f), new Vector2(126f, 163f));
        CreateColorKey(panel.transform, "S", "青", new Color(0.24f, 0.50f, 1f), new Vector2(262f, 163f));
        CreateColorKey(panel.transform, "D", "黄", new Color(1f, 0.88f, 0.24f), new Vector2(126f, 118f));
        CreateColorKey(panel.transform, "F", "緑", new Color(0.25f, 0.95f, 0.38f), new Vector2(262f, 118f));

        CreateSection(panel.transform, "2. 花火を打ち上げる", "左クリックで、装填中の花火を打ち上げます。", new Vector2(0f, 54f), new Vector2(740f, 64f));
        CreateSection(panel.transform, "3. タイミングを合わせる", "打ち上げ予定を確認し、正しい色をタイミングよく打ち上げます。", new Vector2(-190f, -34f), new Vector2(420f, 78f));
        CreateText(panel.transform, "PERFECT：3点\nGOOD：2点\nMISS：0点", 17, FontStyle.Bold, new Vector2(250f, -34f), new Vector2(220f, 78f), TextAnchor.MiddleLeft, new Color(0.90f, 0.94f, 1f));

        CreateSection(panel.transform, "4. その他の操作", "ドラッグ：カメラ回転\nマウスホイール：ズーム", new Vector2(-190f, -130f), new Vector2(360f, 78f));
        CreateText(panel.transform, "制限時間は30秒です。\n高得点と最大コンボを目指してください。", 15, FontStyle.Normal, new Vector2(230f, -130f), new Vector2(300f, 72f), TextAnchor.MiddleLeft, new Color(0.84f, 0.89f, 0.98f));

        CreateButton(canvasObject.transform, "ゲーム開始", new Vector2(-146f, -288f), () => SceneManager.LoadScene("GameScene"));
        CreateButton(canvasObject.transform, "タイトルへ戻る", new Vector2(146f, -288f), () => SceneManager.LoadScene("TitleScene"));
    }

    private static GameObject CreatePanel(Transform parent, Vector2 position, Vector2 dimensions)
    {
        var panelObject = new GameObject("Rule Description Panel");
        panelObject.transform.SetParent(parent, false);
        var rect = panelObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        var image = panelObject.AddComponent<Image>();
        image.color = new Color(0.02f, 0.025f, 0.04f, 0.70f);
        return panelObject;
    }

    private static void CreateSection(Transform parent, string heading, string body, Vector2 position, Vector2 dimensions)
    {
        CreateText(parent, heading, 20, FontStyle.Bold, new Vector2(position.x, position.y + 18f), new Vector2(dimensions.x, 28f), TextAnchor.MiddleLeft, new Color(0.96f, 0.98f, 1f));
        CreateText(parent, body, 16, FontStyle.Normal, new Vector2(position.x, position.y - 18f), new Vector2(dimensions.x, dimensions.y - 30f), TextAnchor.UpperLeft, new Color(0.84f, 0.89f, 0.98f));
    }

    private static void CreateColorKey(Transform parent, string key, string colorName, Color swatchColor, Vector2 position)
    {
        var keyObject = new GameObject(key + " " + colorName);
        keyObject.transform.SetParent(parent, false);
        var rect = keyObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(116f, 34f);

        var image = keyObject.AddComponent<Image>();
        image.color = new Color(0.11f, 0.14f, 0.22f, 0.92f);
        var outline = keyObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.42f, 0.50f, 0.64f, 0.48f);
        outline.effectDistance = new Vector2(1f, -1f);

        var swatch = new GameObject("Color Swatch");
        swatch.transform.SetParent(keyObject.transform, false);
        var swatchRect = swatch.AddComponent<RectTransform>();
        swatchRect.anchorMin = new Vector2(0f, 0.5f);
        swatchRect.anchorMax = new Vector2(0f, 0.5f);
        swatchRect.pivot = new Vector2(0f, 0.5f);
        swatchRect.anchoredPosition = new Vector2(10f, 0f);
        swatchRect.sizeDelta = new Vector2(16f, 16f);
        swatch.AddComponent<Image>().color = swatchColor;

        CreateText(keyObject.transform, key + "：" + colorName, 17, FontStyle.Bold, new Vector2(15f, 0f), new Vector2(86f, 30f), TextAnchor.MiddleCenter, new Color(0.92f, 0.95f, 1f));
    }

    private static Text CreateText(Transform parent, string value, int size, FontStyle style, Vector2 position, Vector2 dimensions)
    {
        return CreateText(parent, value, size, style, position, dimensions, TextAnchor.MiddleCenter, new Color(0.92f, 0.95f, 1f));
    }

    private static Text CreateText(Transform parent, string value, int size, FontStyle style, Vector2 position, Vector2 dimensions, TextAnchor alignment, Color color)
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
        text.alignment = alignment;
        text.color = color;
        return text;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = new GameObject(label + " Button");
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(240f, 52f);

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.13f, 0.17f, 0.25f, 0.96f);
        var outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.42f, 0.50f, 0.64f, 0.50f);
        outline.effectDistance = new Vector2(1f, -1f);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = new Color(0.13f, 0.17f, 0.25f, 0.96f);
        colors.highlightedColor = new Color(0.15f, 0.19f, 0.28f, 0.98f);
        colors.pressedColor = new Color(0.10f, 0.13f, 0.20f, 1f);
        colors.selectedColor = colors.normalColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(action);

        CreateText(buttonObject.transform, label, 22, FontStyle.Bold, Vector2.zero, rect.sizeDelta, TextAnchor.MiddleCenter, new Color(0.92f, 0.95f, 1f));
        return button;
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
}
