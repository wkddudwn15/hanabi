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
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();

        var panel = CreatePanel(canvasObject.transform);
        CreateText(panel.transform, "遊び方", 32, FontStyle.Bold, 42f, TextAnchor.MiddleCenter, new Color(0.95f, 0.97f, 1f));
        CreateSeparator(panel.transform);

        CreateSection(panel.transform, "1. 色を装填する", "A / S / D / Fキーで、打ち上げる花火の色を選びます。", 52f);
        CreateColorKeyGrid(panel.transform);
        CreateSection(panel.transform, "2. 花火を打ち上げる", "左クリックで、選んだ色の花火を打ち上げます。", 48f);
        CreateSection(panel.transform, "3. タイミングを合わせる", "打ち上げ予定を確認し、\n正しい色をタイミングよく打ち上げよう！", 66f);
        CreateJudgmentRow(panel.transform);
        CreateSection(panel.transform, "4. その他の操作", "ドラッグ：カメラ回転\nマウスホイール：ズーム", 62f);
        CreateSection(panel.transform, "5. 制限時間と目標", "制限時間は30秒です。\n高得点と最大コンボを目指しましょう！", 62f);
        CreateButtonRow(panel.transform);
    }

    private static GameObject CreatePanel(Transform parent)
    {
        var panelObject = new GameObject("Rule Description Panel");
        panelObject.transform.SetParent(parent, false);
        var rect = panelObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.16f, 0.06f);
        rect.anchorMax = new Vector2(0.84f, 0.94f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = panelObject.AddComponent<Image>();
        image.color = new Color(0.059f, 0.090f, 0.165f, 0.88f);
        var outline = panelObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.36f, 0.42f, 0.54f, 0.58f);
        outline.effectDistance = new Vector2(2f, -2f);

        var layout = panelObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(34, 34, 22, 22);
        layout.spacing = 4f;
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

    private static void CreateSection(Transform parent, string heading, string body, float height)
    {
        var sectionObject = new GameObject(heading);
        sectionObject.transform.SetParent(parent, false);
        AddLayoutElement(sectionObject, height);

        var layout = sectionObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 3f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateText(sectionObject.transform, heading, 20, FontStyle.Bold, 22f, TextAnchor.MiddleLeft, new Color(0.96f, 0.98f, 1f));
        CreateText(sectionObject.transform, body, 17, FontStyle.Normal, height - 25f, TextAnchor.UpperLeft, new Color(0.84f, 0.89f, 0.98f));
    }

    private static void CreateColorKeyGrid(Transform parent)
    {
        var gridObject = new GameObject("Color Key Grid");
        gridObject.transform.SetParent(parent, false);
        AddLayoutElement(gridObject, 74f);

        var grid = gridObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(190f, 30f);
        grid.spacing = new Vector2(22f, 9f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.MiddleCenter;

        CreateColorKey(gridObject.transform, "A", "赤", new Color(1f, 0.24f, 0.22f));
        CreateColorKey(gridObject.transform, "S", "青", new Color(0.24f, 0.50f, 1f));
        CreateColorKey(gridObject.transform, "D", "黄", new Color(1f, 0.88f, 0.24f));
        CreateColorKey(gridObject.transform, "F", "緑", new Color(0.25f, 0.95f, 0.38f));
    }

    private static void CreateColorKey(Transform parent, string key, string colorName, Color swatchColor)
    {
        var keyObject = new GameObject(key + " " + colorName);
        keyObject.transform.SetParent(parent, false);
        var rect = keyObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(190f, 30f);

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
        swatchRect.anchoredPosition = new Vector2(14f, 0f);
        swatchRect.sizeDelta = new Vector2(16f, 16f);
        swatch.AddComponent<Image>().color = swatchColor;

        CreateText(keyObject.transform, key + "：" + colorName, 17, FontStyle.Bold, Vector2.zero, new Vector2(150f, 30f), TextAnchor.MiddleCenter, new Color(0.92f, 0.95f, 1f));
    }

    private static void CreateJudgmentRow(Transform parent)
    {
        var rowObject = new GameObject("Judgment Row");
        rowObject.transform.SetParent(parent, false);
        AddLayoutElement(rowObject, 48f);

        var layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateJudgmentBadge(rowObject.transform, "PERFECT", "3点", new Color(0.18f, 0.56f, 0.31f, 0.95f));
        CreateJudgmentBadge(rowObject.transform, "GOOD", "2点", new Color(0.18f, 0.36f, 0.65f, 0.95f));
        CreateJudgmentBadge(rowObject.transform, "MISS", "0点", new Color(0.55f, 0.20f, 0.22f, 0.95f));
    }

    private static void CreateJudgmentBadge(Transform parent, string label, string score, Color backgroundColor)
    {
        var badgeObject = new GameObject(label + " Badge");
        badgeObject.transform.SetParent(parent, false);
        var rect = badgeObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(168f, 38f);
        AddLayoutElement(badgeObject, 38f, 168f);

        var image = badgeObject.AddComponent<Image>();
        image.color = backgroundColor;
        var outline = badgeObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.74f, 0.80f, 0.92f, 0.36f);
        outline.effectDistance = new Vector2(1f, -1f);

        CreateText(badgeObject.transform, label + "：" + score, 16, FontStyle.Bold, Vector2.zero, rect.sizeDelta, TextAnchor.MiddleCenter, Color.white);
    }

    private static void CreateButtonRow(Transform parent)
    {
        var rowObject = new GameObject("Rule Button Row");
        rowObject.transform.SetParent(parent, false);
        AddLayoutElement(rowObject, 52f);

        var layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 22f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateButton(rowObject.transform, "ゲーム開始", () => SceneManager.LoadScene("GameScene"));
        CreateButton(rowObject.transform, "タイトルへ戻る", () => SceneManager.LoadScene("TitleScene"));
    }

    private static Text CreateText(Transform parent, string value, int size, FontStyle style, float preferredHeight, TextAnchor alignment, Color color)
    {
        var text = CreateText(parent, value, size, style, Vector2.zero, Vector2.zero, alignment, color);
        AddLayoutElement(text.gameObject, preferredHeight);
        return text;
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
        rect.sizeDelta = new Vector2(240f, 52f);
        AddLayoutElement(buttonObject, 52f, 240f);

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

        CreateText(buttonObject.transform, label, 21, FontStyle.Bold, Vector2.zero, rect.sizeDelta, TextAnchor.MiddleCenter, Color.white);
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
}
