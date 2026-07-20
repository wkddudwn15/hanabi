using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TitleSceneController : MonoBehaviour
{
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
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280f, 720f);
        canvasObject.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();

        CreateText(canvasObject.transform, "3D Fireworks Simulator", 44, FontStyle.Bold, new Vector2(0f, 142f), new Vector2(780f, 70f));
        CreateText(canvasObject.transform, "クリックで花火を打ち上げ、ドラッグとホイールでカメラを操作します。", 22, FontStyle.Normal, new Vector2(0f, 78f), new Vector2(820f, 48f));
        CreateButton(canvasObject.transform, "Start", new Vector2(0f, -12f), () => SceneManager.LoadScene("GameScene"));
        CreateButton(canvasObject.transform, "Quit", new Vector2(0f, -92f), QuitApplication);
    }

    private static Text CreateText(Transform parent, string value, int size, FontStyle style, Vector2 position, Vector2 dimensions)
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
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.92f, 0.95f, 1f);
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
        image.color = new Color(0.14f, 0.18f, 0.28f, 0.94f);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        CreateText(buttonObject.transform, label, 23, FontStyle.Bold, Vector2.zero, rect.sizeDelta);
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

    private static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
