using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class FireworksSceneController : MonoBehaviour
{
    private const float GameDurationSeconds = 30f;
    private const float ResultDisplaySeconds = 0.75f;
    private const int ComboBaseFontSize = 28;
    private const int TargetQueueSize = 5;
    private const float PerfectStart = 0.25f;
    private const float PerfectEnd = 1.00f;
    private const float GoodEnd = 1.60f;
    private const float MissTimeout = 2.20f;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip launchSound;
    [SerializeField] private AudioClip perfectSound;
    [SerializeField] private AudioClip goodSound;
    [SerializeField] private AudioClip missSound;
    [SerializeField] private AudioClip resultSound;

    private readonly List<Rocket> rockets = new List<Rocket>();
    private readonly List<FireworkColor> targetQueue = new List<FireworkColor>();
    private readonly List<TargetSlotUi> targetSlotUis = new List<TargetSlotUi>();
    private readonly List<ColorControlUi> colorControlUis = new List<ColorControlUi>();
    private Camera mainCamera;
    private Transform cameraRig;
    private Text timeText;
    private Text scoreText;
    private Text comboText;
    private Text resultText;
    private RectTransform resultTextRect;
    private Text selectedColorText;
    private Text selectedColorNameText;
    private GameObject resultOverlayObject;
    private Text finalScoreText;
    private Text finalMaxComboText;
    private Text finalRankText;
    private Outline finalRankOutline;
    private Vector3 launcherPosition = new Vector3(0f, 1.15f, 0f);
    private Vector2 pointerDownPosition;
    private Vector2 lastPointerPosition;
    private bool pointerStartedOverUi;
    private bool resultShown;
    private FireworkColor selectedColor = FireworkColor.Red;
    private float targetStartedAt;
    private float resultDisplayTime;
    private float resultDisplayDuration = ResultDisplaySeconds;
    private float resultPopScale = 1f;
    private float remainingTime = GameDurationSeconds;
    private float yaw;
    private float pitch = 18f;
    private float distance = 34f;
    private int score;
    private int currentCombo;
    private int maxCombo;

    private void Start()
    {
        Time.timeScale = 1f;
        CreateCamera();
        CreateLighting();
        CreateWorld();
        CreateUi();
        EnsureAudioSource();
        InitializeTargetQueue();
    }

    private void Update()
    {
        UpdateTimer();
        UpdateResultDisplay();

        if (resultShown)
        {
            UpdateHud();
            return;
        }

        HandleColorInput();
        HandleCameraInput();
        HandleLaunchInput();
        UpdateTargetTimeout();
        UpdateRockets();
        UpdateHud();
    }

    private void OnGUI()
    {
        if (!resultShown)
        {
            return;
        }

        DrawResultRankValue();
    }

    private void CreateCamera()
    {
        cameraRig = new GameObject("Camera Orbit Rig").transform;
        cameraRig.position = new Vector3(0f, 6f, 0f);

        var cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(cameraRig);
        mainCamera = cameraObject.AddComponent<Camera>();
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.005f, 0.008f, 0.025f);
        mainCamera.fieldOfView = 55f;
        if (FindObjectOfType<AudioListener>() == null)
        {
            cameraObject.AddComponent<AudioListener>();
        }

        ApplyCameraOrbit();
    }

    private void CreateLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.22f, 0.26f, 0.38f);
        RenderSettings.ambientEquatorColor = new Color(0.10f, 0.12f, 0.17f);
        RenderSettings.ambientGroundColor = new Color(0.045f, 0.045f, 0.055f);

        var moon = new GameObject("Moon Light").AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = new Color(0.70f, 0.76f, 1f);
        moon.intensity = 1.25f;
        moon.transform.rotation = Quaternion.Euler(48f, -30f, 0f);

        var launcherFill = new GameObject("Launcher Fill Light").AddComponent<Light>();
        launcherFill.type = LightType.Point;
        launcherFill.color = new Color(1f, 0.70f, 0.42f);
        launcherFill.intensity = 6f;
        launcherFill.range = 16f;
        launcherFill.transform.position = new Vector3(0f, 3.1f, -2.2f);
    }

    private void CreateWorld()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(10f, 1f, 10f);
        ground.GetComponent<Renderer>().material = MakeStandardMaterial(new Color(0.055f, 0.075f, 0.11f), 0.05f, 0.32f);

        var baseObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObject.name = "Launcher Base";
        baseObject.transform.position = new Vector3(0f, 0.2f, 0f);
        baseObject.transform.localScale = new Vector3(1.8f, 0.4f, 1.8f);
        baseObject.GetComponent<Renderer>().material = MakeStandardMaterial(new Color(0.23f, 0.27f, 0.34f), 0.30f, 0.34f);

        var tube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tube.name = "Launcher Tube";
        tube.transform.position = new Vector3(0f, 1.35f, 0f);
        tube.transform.rotation = Quaternion.Euler(0f, 0f, -8f);
        tube.transform.localScale = new Vector3(0.42f, 1.55f, 0.42f);
        tube.GetComponent<Renderer>().material = MakeStandardMaterial(new Color(0.46f, 0.54f, 0.65f), 0.45f, 0.42f);

        for (var i = 0; i < 130; i++)
        {
            var star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.name = "Star";
            star.transform.position = Random.onUnitSphere * Random.Range(55f, 85f);
            star.transform.position = new Vector3(star.transform.position.x, Mathf.Abs(star.transform.position.y) + 18f, star.transform.position.z);
            star.transform.localScale = Vector3.one * Random.Range(0.035f, 0.075f);
            star.GetComponent<Renderer>().material = MakeEmissionMaterial(Color.white * Random.Range(0.55f, 1f), 0.65f);
            Destroy(star.GetComponent<Collider>());
        }
    }

    private void CreateUi()
    {
        var canvasObject = new GameObject("Game Canvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvasObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        CreateText(canvasObject.transform, "クリック: 花火発射 / ドラッグ: カメラ回転 / ホイール: ズーム", 18, FontStyle.Normal, new Vector2(18f, 18f), new Vector2(620f, 32f), TextAnchor.LowerLeft, new Vector2(0f, 0f));
        CreateText(canvasObject.transform, "打ち上げ予定", 24, FontStyle.Bold, new Vector2(0f, -14f), new Vector2(260f, 32f), TextAnchor.UpperCenter, new Vector2(0.5f, 1f));
        CreateTargetQueueUi(canvasObject.transform);
        resultText = CreateText(canvasObject.transform, string.Empty, 42, FontStyle.Bold, new Vector2(0f, 72f), new Vector2(360f, 70f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        resultTextRect = resultText.GetComponent<RectTransform>();
        timeText = CreateText(canvasObject.transform, string.Empty, 28, FontStyle.Bold, new Vector2(18f, -18f), new Vector2(220f, 40f), TextAnchor.UpperLeft, new Vector2(0f, 1f));
        scoreText = CreateText(canvasObject.transform, string.Empty, 28, FontStyle.Bold, new Vector2(18f, -58f), new Vector2(220f, 40f), TextAnchor.UpperLeft, new Vector2(0f, 1f));
        comboText = CreateText(canvasObject.transform, string.Empty, 28, FontStyle.Bold, new Vector2(18f, -98f), new Vector2(220f, 40f), TextAnchor.UpperLeft, new Vector2(0f, 1f));
        selectedColorText = CreateText(canvasObject.transform, "装填中：", 24, FontStyle.Bold, new Vector2(18f, -144f), new Vector2(116f, 34f), TextAnchor.UpperLeft, new Vector2(0f, 1f));
        selectedColorNameText = CreateText(canvasObject.transform, string.Empty, 24, FontStyle.Bold, new Vector2(134f, -144f), new Vector2(92f, 34f), TextAnchor.UpperLeft, new Vector2(0f, 1f));
        CreateColorControlUi(canvasObject.transform);
        CreateResultUi(canvasObject.transform);
        UpdateHud();

        CreateButton(canvasObject.transform, "Title", new Vector2(-184f, -30f), new Vector2(132f, 42f), () => SceneManager.LoadScene("TitleScene"));
        CreateButton(canvasObject.transform, "Quit", new Vector2(-40f, -30f), new Vector2(112f, 42f), QuitApplication);
    }

    private void CreateTargetQueueUi(Transform parent)
    {
        targetSlotUis.Clear();
        var positions = new[]
        {
            new Vector2(-254f, -66f),
            new Vector2(-104f, -74f),
            new Vector2(22f, -74f),
            new Vector2(148f, -74f),
            new Vector2(274f, -74f)
        };

        for (var i = 0; i < TargetQueueSize; i++)
        {
            var isCurrent = i == 0;
            var slot = CreateTargetSlot(parent, "Target Slot " + i.ToString(), positions[i], isCurrent);
            targetSlotUis.Add(slot);

            if (i < TargetQueueSize - 1)
            {
                var arrowX = i == 0 ? -170f : -42f + ((i - 1) * 126f);
                CreateText(parent, "→", 24, FontStyle.Bold, new Vector2(arrowX, -88f), new Vector2(28f, 30f), TextAnchor.MiddleCenter, new Vector2(0.5f, 1f));
            }
        }
    }

    private TargetSlotUi CreateTargetSlot(Transform parent, string name, Vector2 position, bool isCurrent)
    {
        var dimensions = isCurrent ? new Vector2(126f, 76f) : new Vector2(96f, 58f);
        var slotObject = new GameObject(name);
        slotObject.transform.SetParent(parent, false);

        var rect = slotObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        var image = slotObject.AddComponent<Image>();
        image.color = new Color(0.10f, 0.12f, 0.18f, 0.86f);

        var outline = slotObject.AddComponent<Outline>();
        outline.effectColor = isCurrent ? new Color(1f, 0.95f, 0.58f, 1f) : new Color(0.78f, 0.84f, 1f, 0.42f);
        outline.effectDistance = isCurrent ? new Vector2(4f, -4f) : new Vector2(2f, -2f);

        var swatchSize = isCurrent ? new Vector2(34f, 34f) : new Vector2(24f, 24f);
        var swatchObject = new GameObject(name + " Swatch");
        swatchObject.transform.SetParent(slotObject.transform, false);
        var swatchRect = swatchObject.AddComponent<RectTransform>();
        swatchRect.anchorMin = new Vector2(0f, 0.5f);
        swatchRect.anchorMax = new Vector2(0f, 0.5f);
        swatchRect.pivot = new Vector2(0f, 0.5f);
        swatchRect.anchoredPosition = isCurrent ? new Vector2(12f, 4f) : new Vector2(10f, 0f);
        swatchRect.sizeDelta = swatchSize;
        var swatch = swatchObject.AddComponent<Image>();

        var nameText = CreateText(slotObject.transform, string.Empty, isCurrent ? 27 : 22, FontStyle.Bold, isCurrent ? new Vector2(28f, 7f) : new Vector2(18f, 0f), isCurrent ? new Vector2(82f, 36f) : new Vector2(62f, 34f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        var labelText = CreateText(slotObject.transform, isCurrent ? "現在" : string.Empty, 13, FontStyle.Bold, new Vector2(0f, -25f), new Vector2(70f, 20f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        labelText.color = new Color(1f, 0.96f, 0.68f, 0.96f);

        return new TargetSlotUi
        {
            Background = image,
            Swatch = swatch,
            NameText = nameText,
            LabelText = labelText
        };
    }

    private void UpdateTargetQueueUi()
    {
        for (var i = 0; i < targetSlotUis.Count; i++)
        {
            if (i >= targetQueue.Count)
            {
                continue;
            }

            var slot = targetSlotUis[i];
            var color = targetQueue[i];
            var visualColor = GetFireworkColor(color);
            var isCurrent = i == 0;

            slot.Background.color = Color.Lerp(new Color(0.08f, 0.10f, 0.15f, 0.92f), visualColor, isCurrent ? 0.38f : 0.20f);
            slot.Swatch.color = visualColor;
            slot.NameText.text = GetColorName(color);
            slot.NameText.color = Color.Lerp(visualColor, Color.white, isCurrent ? 0.42f : 0.26f);
            slot.LabelText.text = isCurrent ? "現在" : string.Empty;
        }
    }

    private void CreateColorControlUi(Transform parent)
    {
        colorControlUis.Clear();
        CreateColorControl(parent, KeyCode.A, FireworkColor.Red, new Vector2(50f, -190f));
        CreateColorControl(parent, KeyCode.S, FireworkColor.Blue, new Vector2(142f, -190f));
        CreateColorControl(parent, KeyCode.D, FireworkColor.Yellow, new Vector2(234f, -190f));
        CreateColorControl(parent, KeyCode.F, FireworkColor.Green, new Vector2(326f, -190f));
    }

    private void CreateColorControl(Transform parent, KeyCode keyCode, FireworkColor color, Vector2 position)
    {
        var controlObject = new GameObject(keyCode.ToString() + " " + GetColorName(color));
        controlObject.transform.SetParent(parent, false);

        var rect = controlObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(78f, 34f);

        var image = controlObject.AddComponent<Image>();
        var outline = controlObject.AddComponent<Outline>();
        var label = CreateText(controlObject.transform, keyCode.ToString() + " " + GetColorName(color), 17, FontStyle.Bold, Vector2.zero, new Vector2(78f, 34f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));

        colorControlUis.Add(new ColorControlUi
        {
            Color = color,
            Background = image,
            Outline = outline,
            Label = label
        });
    }

    private void UpdateColorControlUi()
    {
        for (var i = 0; i < colorControlUis.Count; i++)
        {
            var control = colorControlUis[i];
            var isSelected = control.Color == selectedColor;
            var color = GetFireworkColor(control.Color);

            control.Background.color = Color.Lerp(new Color(0.10f, 0.12f, 0.18f, 0.92f), color, isSelected ? 0.50f : 0.20f);
            control.Outline.effectColor = isSelected ? Color.Lerp(color, Color.white, 0.35f) : new Color(0.62f, 0.70f, 0.86f, 0.34f);
            control.Outline.effectDistance = isSelected ? new Vector2(4f, -4f) : new Vector2(2f, -2f);
            control.Label.color = isSelected ? Color.white : new Color(0.90f, 0.94f, 1f, 0.88f);
        }
    }

    private void CreateResultUi(Transform parent)
    {
        resultOverlayObject = new GameObject("Result Overlay");
        resultOverlayObject.transform.SetParent(parent, false);

        var overlayRect = resultOverlayObject.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.pivot = new Vector2(0.5f, 0.5f);
        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.sizeDelta = Vector2.zero;

        var overlayImage = resultOverlayObject.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.68f);
        overlayImage.raycastTarget = true;

        var panelObject = new GameObject("Result Panel");
        panelObject.transform.SetParent(resultOverlayObject.transform, false);
        var panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(430f, 500f);

        var panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.10f, 0.16f, 0.98f);
        var panelOutline = panelObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.82f, 0.88f, 1f, 0.62f);
        panelOutline.effectDistance = new Vector2(3f, -3f);

        CreateText(panelObject.transform, "TIME UP", 42, FontStyle.Bold, new Vector2(0f, 198f), new Vector2(330f, 58f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        CreateText(panelObject.transform, "SCORE", 20, FontStyle.Bold, new Vector2(0f, 116f), new Vector2(220f, 28f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        finalScoreText = CreateText(panelObject.transform, "0", 36, FontStyle.Bold, new Vector2(0f, 76f), new Vector2(220f, 46f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        CreateText(panelObject.transform, "MAX COMBO", 20, FontStyle.Bold, new Vector2(0f, 12f), new Vector2(220f, 28f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        finalMaxComboText = CreateText(panelObject.transform, "0", 36, FontStyle.Bold, new Vector2(0f, -28f), new Vector2(220f, 46f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        CreateText(panelObject.transform, "RANK", 20, FontStyle.Bold, new Vector2(0f, -72f), new Vector2(220f, 28f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        finalRankText = CreateText(panelObject.transform, "C", 82, FontStyle.Bold, new Vector2(0f, -132f), new Vector2(260f, 96f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        finalRankText.color = Color.white;
        finalRankText.raycastTarget = false;
        finalRankOutline = finalRankText.gameObject.AddComponent<Outline>();
        finalRankOutline.effectColor = new Color(0f, 0f, 0f, 0.45f);
        finalRankOutline.effectDistance = new Vector2(2f, -2f);

        CreateResultButton(panelObject.transform, "RETRY", new Vector2(-92f, -212f), new Vector2(150f, 46f), RestartGame);
        CreateResultButton(panelObject.transform, "TITLE", new Vector2(92f, -212f), new Vector2(150f, 46f), ReturnToTitle);

        resultOverlayObject.SetActive(false);
    }

    private Button CreateResultButton(Transform parent, string label, Vector2 position, Vector2 dimensions, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = new GameObject(label + " Button");
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.23f, 0.34f, 1f);
        var outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.80f, 0.88f, 1f, 0.70f);
        outline.effectDistance = new Vector2(2f, -2f);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        CreateText(buttonObject.transform, label, 18, FontStyle.Bold, Vector2.zero, dimensions, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        return button;
    }

    private void ShowGameResult()
    {
        if (resultShown)
        {
            return;
        }

        resultShown = true;
        remainingTime = 0f;
        resultDisplayTime = 0f;
        var finalScore = score;
        if (resultText != null)
        {
            resultText.text = string.Empty;
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = finalScore.ToString();
        }

        if (finalMaxComboText != null)
        {
            finalMaxComboText.text = maxCombo.ToString();
        }

        var finalRank = GetRank(finalScore);
        Debug.Log($"Final Rank: {finalRank}");
        if (finalRankText != null)
        {
            finalRankText.text = finalRank;
            finalRankText.fontSize = finalRank == "S" ? 92 : 82;
            finalRankText.color = Color.white;
            finalRankText.transform.SetAsLastSibling();
        }

        if (finalRankOutline != null)
        {
            finalRankOutline.effectColor = finalRank == "S" ? new Color(1f, 0.52f, 0.10f, 0.82f) : new Color(0f, 0f, 0f, 0.65f);
            finalRankOutline.effectDistance = finalRank == "S" ? new Vector2(4f, -4f) : new Vector2(2f, -2f);
        }

        if (resultOverlayObject != null)
        {
            resultOverlayObject.transform.SetAsLastSibling();
            resultOverlayObject.SetActive(true);
        }

        PlaySound(resultSound);
    }

    private void DrawResultRankValue()
    {
        var previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.identity;
        var finalRank = GetRank(score);

        var scaleX = Screen.width / 1280f;
        var scaleY = Screen.height / 720f;
        var panelWidth = 430f * scaleX;
        var panelHeight = 500f * scaleY;
        var resultPanelRect = new Rect(
            (Screen.width - panelWidth) * 0.5f,
            (Screen.height - panelHeight) * 0.5f,
            panelWidth,
            panelHeight
        );
        var rankValueRect = new Rect(
            resultPanelRect.x + (20f * scaleX),
            resultPanelRect.y + (305f * scaleY),
            resultPanelRect.width - (40f * scaleX),
            92f * scaleY
        );

        var rankValueStyle = new GUIStyle(GUI.skin.label);
        rankValueStyle.alignment = TextAnchor.MiddleCenter;
        rankValueStyle.fontSize = Mathf.Max(48, Mathf.RoundToInt((finalRank == "S" ? 92f : 82f) * Mathf.Min(scaleX, scaleY)));
        rankValueStyle.fontStyle = FontStyle.Bold;
        rankValueStyle.normal.textColor = Color.white;

        if (finalRank == "S")
        {
            var outlineStyle = new GUIStyle(rankValueStyle);
            outlineStyle.normal.textColor = new Color(0.02f, 0.02f, 0.02f, 0.92f);
            var offset = Mathf.Max(2f, 4f * Mathf.Min(scaleX, scaleY));
            GUI.Label(new Rect(rankValueRect.x - offset, rankValueRect.y, rankValueRect.width, rankValueRect.height), finalRank, outlineStyle);
            GUI.Label(new Rect(rankValueRect.x + offset, rankValueRect.y, rankValueRect.width, rankValueRect.height), finalRank, outlineStyle);
            GUI.Label(new Rect(rankValueRect.x, rankValueRect.y - offset, rankValueRect.width, rankValueRect.height), finalRank, outlineStyle);
            GUI.Label(new Rect(rankValueRect.x, rankValueRect.y + offset, rankValueRect.width, rankValueRect.height), finalRank, outlineStyle);
        }

        rankValueStyle.normal.textColor = Color.white;
        GUI.Label(rankValueRect, finalRank, rankValueStyle);
        GUI.matrix = previousMatrix;
    }

    private string GetRank(int finalScore)
    {
        if (finalScore >= 60)
        {
            return "S";
        }

        if (finalScore >= 40)
        {
            return "A";
        }

        if (finalScore >= 20)
        {
            return "B";
        }

        return "C";
    }

    private void RestartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    private void ReturnToTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }

    private void EnsureAudioSource()
    {
        if (audioSource != null)
        {
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }

    private void UpdateTimer()
    {
        if (remainingTime <= 0f)
        {
            if (!resultShown)
            {
                ShowGameResult();
            }

            return;
        }

        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        if (remainingTime <= 0f)
        {
            ShowGameResult();
        }
    }

    private void UpdateHud()
    {
        if (timeText != null)
        {
            timeText.text = "Time: " + Mathf.CeilToInt(remainingTime).ToString();
        }

        if (scoreText != null)
        {
            scoreText.text = "Score: " + score.ToString();
        }

        if (comboText != null)
        {
            comboText.text = "COMBO " + currentCombo.ToString();
            comboText.fontSize = GetComboFontSize();
            comboText.color = GetComboColor();
        }

        if (selectedColorText != null)
        {
            selectedColorText.text = "装填中：";
        }

        if (selectedColorNameText != null)
        {
            selectedColorNameText.text = GetColorName(selectedColor);
            selectedColorNameText.color = GetFireworkColor(selectedColor);
        }

        UpdateTargetQueueUi();
        UpdateColorControlUi();
    }

    private void HandleColorInput()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            SelectFireworkColor(FireworkColor.Red);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            SelectFireworkColor(FireworkColor.Blue);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            SelectFireworkColor(FireworkColor.Yellow);
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            SelectFireworkColor(FireworkColor.Green);
        }
    }

    private void SelectFireworkColor(FireworkColor color)
    {
        selectedColor = color;
        UpdateHud();
    }

    private void InitializeTargetQueue()
    {
        if (remainingTime <= 0f)
        {
            return;
        }

        targetQueue.Clear();
        for (var i = 0; i < TargetQueueSize; i++)
        {
            targetQueue.Add(GetRandomFireworkColor());
        }

        targetStartedAt = Time.time;
        UpdateHud();
    }

    private void FinishCurrentTarget(JudgmentResult result)
    {
        if (remainingTime <= 0f)
        {
            return;
        }

        ShowJudgment(result);
        AdvanceTargetQueue();
    }

    private void AdvanceTargetQueue()
    {
        if (remainingTime <= 0f)
        {
            return;
        }

        if (targetQueue.Count > 0)
        {
            targetQueue.RemoveAt(0);
        }

        while (targetQueue.Count < TargetQueueSize)
        {
            targetQueue.Add(GetRandomFireworkColor());
        }

        targetStartedAt = Time.time;
        UpdateHud();
    }

    private void UpdateTargetTimeout()
    {
        if (remainingTime <= 0f)
        {
            return;
        }

        if (Time.time - targetStartedAt >= MissTimeout)
        {
            FinishCurrentTarget(JudgmentResult.Miss);
        }
    }

    private void UpdateResultDisplay()
    {
        if (resultDisplayTime <= 0f)
        {
            return;
        }

        resultDisplayTime = Mathf.Max(0f, resultDisplayTime - Time.deltaTime);
        UpdateJudgmentAnimation();
        if (resultDisplayTime <= 0f && resultText != null)
        {
            resultText.text = string.Empty;
            if (resultTextRect != null)
            {
                resultTextRect.localScale = Vector3.one;
            }
        }
    }

    private void ShowResult(string message, Color color, int fontSize, float popScale)
    {
        if (resultText == null)
        {
            return;
        }

        resultText.text = message;
        resultText.color = color;
        resultText.fontSize = fontSize;
        resultPopScale = popScale;
        resultDisplayDuration = ResultDisplaySeconds;
        resultDisplayTime = ResultDisplaySeconds;
        UpdateJudgmentAnimation();
    }

    private void UpdateJudgmentAnimation()
    {
        if (resultTextRect == null || resultDisplayDuration <= 0f)
        {
            return;
        }

        var normalized = 1f - Mathf.Clamp01(resultDisplayTime / resultDisplayDuration);
        var scale = Mathf.Lerp(resultPopScale, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalized * 1.8f)));
        resultTextRect.localScale = Vector3.one * scale;

        if (resultText != null && resultDisplayTime < 0.22f)
        {
            var color = resultText.color;
            color.a = Mathf.Clamp01(resultDisplayTime / 0.22f);
            resultText.color = color;
        }
    }

    private int GetComboFontSize()
    {
        if (currentCombo >= 20)
        {
            return 40;
        }

        if (currentCombo >= 10)
        {
            return 36;
        }

        if (currentCombo >= 5)
        {
            return 32;
        }

        return ComboBaseFontSize;
    }

    private Color GetComboColor()
    {
        if (currentCombo >= 20)
        {
            return new Color(1f, 0.90f, 0.20f, 1f);
        }

        if (currentCombo >= 10)
        {
            return new Color(0.55f, 0.92f, 1f, 1f);
        }

        if (currentCombo >= 5)
        {
            return new Color(0.72f, 1f, 0.58f, 1f);
        }

        return new Color(0.90f, 0.94f, 1f, 0.9f);
    }

    private void HandleLaunchInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            pointerDownPosition = Input.mousePosition;
            lastPointerPosition = pointerDownPosition;
            pointerStartedOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        if (!Input.GetMouseButtonUp(0) || pointerStartedOverUi)
        {
            return;
        }

        var movement = Vector2.Distance(pointerDownPosition, Input.mousePosition);
        if (movement < 8f)
        {
            LaunchRocket();
        }
    }

    private void HandleCameraInput()
    {
        if (Input.GetMouseButton(0) && !pointerStartedOverUi)
        {
            var currentPosition = (Vector2)Input.mousePosition;
            var movement = currentPosition - lastPointerPosition;
            lastPointerPosition = currentPosition;
            if (movement.sqrMagnitude > 0.0001f)
            {
                yaw += movement.x * 0.28f;
                pitch = Mathf.Clamp(pitch - movement.y * 0.22f, -4f, 58f);
            }
        }

        var scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            distance = Mathf.Clamp(distance - scroll * 2.4f, 12f, 74f);
        }

        ApplyCameraOrbit();
    }

    private void ApplyCameraOrbit()
    {
        cameraRig.rotation = Quaternion.Euler(pitch, yaw, 0f);
        mainCamera.transform.localPosition = new Vector3(0f, 0f, -distance);
        mainCamera.transform.LookAt(cameraRig.position);
    }

    private void LaunchRocket()
    {
        if (remainingTime <= 0f)
        {
            return;
        }

        var color = GetFireworkColor(selectedColor);
        PlaySound(launchSound);
        CreateLaunchFlash(color);
        FinishCurrentTarget(GetJudgmentResult());

        var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shell.name = "Firework Rocket";
        shell.transform.position = launcherPosition;
        shell.transform.localScale = Vector3.one * 0.28f;
        shell.GetComponent<Renderer>().material = MakeEmissionMaterial(color, 2.2f);
        Destroy(shell.GetComponent<Collider>());

        var light = shell.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = 3.5f;
        light.range = 7f;

        rockets.Add(new Rocket
        {
            Object = shell,
            Color = color,
            Velocity = new Vector3(Random.Range(-0.9f, 0.9f), Random.Range(15.5f, 20.5f), Random.Range(-0.9f, 0.9f)),
            Fuse = Random.Range(1.15f, 1.55f)
        });

        UpdateHud();
    }

    private void CreateLaunchFlash(Color color)
    {
        var flashObject = new GameObject("Launch Flash");
        flashObject.transform.position = launcherPosition;

        var light = flashObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = 7f;
        light.range = 9f;

        var particles = flashObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.duration = 0.35f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 4.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.gravityModifier = -0.12f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.Lerp(color, Color.white, 0.25f), color);

        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)36) });

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.28f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.material = MakeParticleMaterial(color);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        flashObject.AddComponent<LaunchFlashCleanup>().Initialize(light, 0.42f);
        particles.Play();
    }

    private JudgmentResult GetJudgmentResult()
    {
        if (selectedColor != CurrentTargetColor)
        {
            return JudgmentResult.Miss;
        }

        var elapsed = Time.time - targetStartedAt;
        if (elapsed >= PerfectStart && elapsed <= PerfectEnd)
        {
            return JudgmentResult.Perfect;
        }

        if (elapsed > PerfectEnd && elapsed <= GoodEnd)
        {
            return JudgmentResult.Good;
        }

        return JudgmentResult.Miss;
    }

    private FireworkColor CurrentTargetColor
    {
        get
        {
            if (targetQueue.Count == 0)
            {
                return FireworkColor.Red;
            }

            return targetQueue[0];
        }
    }

    private void ShowJudgment(JudgmentResult result)
    {
        switch (result)
        {
            case JudgmentResult.Perfect:
                score += 3;
                IncreaseCombo();
                ShowResult("PERFECT", new Color(1f, 0.96f, 0.25f, 1f), 54, 1.28f);
                PlaySound(perfectSound);
                break;
            case JudgmentResult.Good:
                score += 2;
                IncreaseCombo();
                ShowResult("GOOD", new Color(0.48f, 0.86f, 1f, 0.96f), 46, 1.14f);
                PlaySound(goodSound);
                break;
            default:
                ResetCombo();
                ShowResult("MISS", new Color(0.95f, 0.35f, 0.32f, 0.72f), 36, 1.04f);
                PlaySound(missSound);
                break;
        }
    }

    private void IncreaseCombo()
    {
        currentCombo++;
        maxCombo = Mathf.Max(maxCombo, currentCombo);
    }

    private void ResetCombo()
    {
        currentCombo = 0;
    }

    private void UpdateRockets()
    {
        for (var i = rockets.Count - 1; i >= 0; i--)
        {
            var rocket = rockets[i];
            rocket.Fuse -= Time.deltaTime;
            rocket.Velocity += Physics.gravity * (0.22f * Time.deltaTime);
            rocket.Object.transform.position += rocket.Velocity * Time.deltaTime;

            if (rocket.Fuse <= 0f || rocket.Velocity.y <= 1.5f)
            {
                CreateExplosion(rocket.Object.transform.position, rocket.Color);
                Destroy(rocket.Object);
                rockets.RemoveAt(i);
            }
            else
            {
                rockets[i] = rocket;
            }
        }
    }

    private void CreateExplosion(Vector3 position, Color color)
    {
        var explosionObject = new GameObject("Firework Explosion");
        explosionObject.transform.position = position;

        var particles = explosionObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.duration = 2.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.7f, 2.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5.5f, 12.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.gravityModifier = 0.85f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.35f));

        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Random.Range(170, 260)) });

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.22f;

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(color, 0.08f),
                new GradientColorKey(Color.Lerp(color, Color.black, 0.55f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.material = MakeParticleMaterial(color);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        var flash = explosionObject.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = color;
        flash.intensity = 8f;
        flash.range = 18f;

        explosionObject.AddComponent<ExplosionCleanup>().Initialize(flash, 2.8f);
        particles.Play();
    }

    private static Text CreateText(Transform parent, string value, int size, FontStyle style, Vector2 position, Vector2 dimensions, TextAnchor alignment, Vector2 anchor)
    {
        var textObject = new GameObject(value);
        textObject.transform.SetParent(parent, false);
        var rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        var text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = new Color(0.90f, 0.94f, 1f, 0.9f);
        return text;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 position, Vector2 dimensions, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = new GameObject(label + " Button");
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.15f, 0.23f, 0.94f);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        CreateText(buttonObject.transform, label, 18, FontStyle.Bold, Vector2.zero, dimensions, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        return button;
    }

    private static Material MakeStandardMaterial(Color color, float metallic, float smoothness)
    {
        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Glossiness", smoothness);
        return material;
    }

    private static Color GetFireworkColor(FireworkColor color)
    {
        switch (color)
        {
            case FireworkColor.Blue:
                return new Color(0.20f, 0.48f, 1f);
            case FireworkColor.Yellow:
                return new Color(1f, 0.88f, 0.18f);
            case FireworkColor.Green:
                return new Color(0.20f, 0.95f, 0.36f);
            default:
                return new Color(1f, 0.18f, 0.16f);
        }
    }

    private static string GetColorName(FireworkColor color)
    {
        switch (color)
        {
            case FireworkColor.Blue:
                return "青";
            case FireworkColor.Yellow:
                return "黄";
            case FireworkColor.Green:
                return "緑";
            default:
                return "赤";
        }
    }

    private static FireworkColor GetRandomFireworkColor()
    {
        return (FireworkColor)Random.Range(0, 4);
    }

    private static Material MakeEmissionMaterial(Color color, float intensity)
    {
        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * intensity);
        return material;
    }

    private static Material MakeParticleMaterial(Color color)
    {
        var material = new Material(Shader.Find("Particles/Standard Unlit"));
        material.SetColor("_Color", color);
        material.SetFloat("_Mode", 2f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = 3000;
        return material;
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private struct Rocket
    {
        public GameObject Object;
        public Vector3 Velocity;
        public Color Color;
        public float Fuse;
    }

    private struct TargetSlotUi
    {
        public Image Background;
        public Image Swatch;
        public Text NameText;
        public Text LabelText;
    }

    private struct ColorControlUi
    {
        public FireworkColor Color;
        public Image Background;
        public Outline Outline;
        public Text Label;
    }

    private enum FireworkColor
    {
        Red,
        Blue,
        Yellow,
        Green
    }

    private enum JudgmentResult
    {
        Perfect,
        Good,
        Miss
    }

    private sealed class ExplosionCleanup : MonoBehaviour
    {
        private Light flash;
        private float lifetime;
        private float age;

        public void Initialize(Light targetLight, float seconds)
        {
            flash = targetLight;
            lifetime = seconds;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (flash != null)
            {
                flash.intensity = Mathf.Lerp(8f, 0f, Mathf.Clamp01(age / 0.55f));
            }

            if (age >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }

    private sealed class LaunchFlashCleanup : MonoBehaviour
    {
        private Light flash;
        private float lifetime;
        private float age;

        public void Initialize(Light targetLight, float seconds)
        {
            flash = targetLight;
            lifetime = seconds;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (flash != null)
            {
                flash.intensity = Mathf.Lerp(7f, 0f, Mathf.Clamp01(age / 0.18f));
            }

            if (age >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
