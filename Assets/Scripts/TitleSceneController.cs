using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TitleSceneController : MonoBehaviour
{
    private static readonly Color Night = new Color(0.01f, 0.015f, 0.035f);
    private GameObject titlePanel;
    private GameObject rulePanel;

    private void Start()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.20f, 0.24f, 0.36f);
        RenderSettings.ambientEquatorColor = new