using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class FireworksSceneController : MonoBehaviour
{
    private readonly List<Rocket> rockets = new List<Rocket>();
    private Camera mainCamera;
    private Transform cameraRig;
    private Vector3 launcherPosition = new Vector3(0f, 1.15f, 0f);
    private Vector2 pointerDownPosition;
    private Vector2 lastPointerPosition;
    private bool pointerStartedOverUi;
    private float yaw;
    private float pitch = 18f;
    private float distance = 34f;

    private void Start()
    {
        Time.timeScale = 1f;
        CreateCamera();
        CreateLighting();
        CreateWorld();
        CreateUi();
    }

    private void Update()
    {
        HandleCameraInput();
        HandleLaunchInput();
        UpdateRockets();
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
        CreateButton(canvasObject.transform, "Title", new Vector2(-184f, -30f), new Vector2(132f, 42f), () => SceneManager.LoadScene("TitleScene"));
        CreateButton(canvasObject.transform, "Quit", new Vector2(-40f, -30f), new Vector2(112f, 42f), QuitApplication);
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
        var color = Color.HSVToRGB(Random.value, 0.92f, 1f);
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
        var main = particles.main;
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
}
