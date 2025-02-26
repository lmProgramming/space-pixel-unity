using UnityEngine;

public class StarBackgroundController : MonoBehaviour
{
    private static readonly int Seed = Shader.PropertyToID("_Seed");
    private static readonly int StarDensity = Shader.PropertyToID("_StarDensity");
    private static readonly int TwinkleVariation = Shader.PropertyToID("_TwinkleVariation");
    private static readonly int TwinkleSpeed = Shader.PropertyToID("_TwinkleSpeed");
    private static readonly int StarSizeMax = Shader.PropertyToID("_StarSizeMax");
    private static readonly int StarSizeMin = Shader.PropertyToID("_StarSizeMin");
    private static readonly int ParallaxStrength = Shader.PropertyToID("_ParallaxStrength");
    private static readonly int NoiseSpeed = Shader.PropertyToID("_NoiseSpeed");
    private static readonly int NoiseStrength = Shader.PropertyToID("_NoiseStrength");
    private static readonly int NoiseScale = Shader.PropertyToID("_NoiseScale");
    private static readonly int MinZoom = Shader.PropertyToID("_MinZoom");
    private static readonly int MaxZoom = Shader.PropertyToID("_MaxZoom");
    private static readonly int GradientColor3 = Shader.PropertyToID("_GradientColor3");
    private static readonly int GradientColor2 = Shader.PropertyToID("_GradientColor2");
    private static readonly int GradientColor1 = Shader.PropertyToID("_GradientColor1");
    private static readonly int BackgroundColor = Shader.PropertyToID("_BackgroundColor");
    private static readonly int StarColor3 = Shader.PropertyToID("_StarColor3");
    private static readonly int StarColor2 = Shader.PropertyToID("_StarColor2");
    private static readonly int StarColor1 = Shader.PropertyToID("_StarColor1");
    private static readonly int PlayerPosition = Shader.PropertyToID("_PlayerPosition");
    private static readonly int CurrentZoom = Shader.PropertyToID("_CurrentZoom");
    public Material starBackgroundMaterial;
    public Transform playerTransform;
    public Camera mainCamera;

    [Header("Parallax Settings")] [Range(0.001f, 0.1f)]
    public float parallaxStrength = 0.02f;

    [Header("Background Gradient")] public Color backgroundColor = Color.black;

    public Color gradientColor1 = new(0.05f, 0f, 0.1f, 1f);
    public Color gradientColor2 = new(0f, 0.05f, 0.1f, 1f);
    public Color gradientColor3 = new(0.02f, 0.02f, 0.05f, 1f);

    [Header("Background Noise")] [Range(0.001f, 0.1f)]
    public float noiseScale = 0.01f;

    [Range(0f, 1f)] public float noiseStrength = 0.5f;

    [Range(0f, 0.1f)] public float noiseSpeed = 0.01f;

    [Header("Star Colors")] public Color primaryStarColor = Color.white;

    public Color secondaryStarColor = new(0.2f, 0.5f, 1f);
    public Color tertiaryStarColor = new(1f, 0.5f, 0.2f);

    [Header("Star Appearance")] [Range(0.001f, 10f)]
    public float minStarSize = 0.005f;

    [Range(0.001f, 10f)] public float maxStarSize = 0.02f;

    [Range(0f, 2f)] public float twinkleSpeed = 0.5f;

    [Range(0f, 1f)] public float twinkleVariation = 0.7f;

    [Range(0.1f, 10f)] public float starDensity = 2.0f;

    [Header("Zoom Settings")] public bool trackCameraZoom = true;

    [Range(0.1f, 100f)] public float minZoom = 0.5f;

    [Range(0.1f, 100f)] public float maxZoom = 5f;

    [Range(0, 1000)] public float randomSeed = 42f;

    private float _initialCameraSize;
    private Vector3 _initialPlayerPosition;

    private void Start()
    {
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (playerTransform == null)
                Debug.LogWarning("Player transform not assigned to StarBackgroundController.");
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;

            if (mainCamera == null)
                Debug.LogWarning("Main camera not found for StarBackgroundController.");
        }

        if (starBackgroundMaterial == null)
        {
            var rendererComponent = GetComponent<Renderer>();
            if (rendererComponent != null)
                starBackgroundMaterial = rendererComponent.sharedMaterial;
            else
                Debug.LogError("No star background material assigned and no renderer found.");
        }

        _initialPlayerPosition = playerTransform != null ? playerTransform.position : Vector3.zero;
        _initialCameraSize = mainCamera != null
            ? mainCamera.orthographic ? mainCamera.orthographicSize : mainCamera.fieldOfView
            : 5f;

        UpdateShaderProperties();
    }

    private void Update()
    {
        if (!starBackgroundMaterial)
            return;

        if (playerTransform)
        {
            Vector4 playerPos = playerTransform.position - _initialPlayerPosition;
            starBackgroundMaterial.SetVector(PlayerPosition, playerPos);
        }

        if (mainCamera && trackCameraZoom)
        {
            var currentSize = mainCamera.orthographic ? mainCamera.orthographicSize : mainCamera.fieldOfView;
            var zoomFactor = currentSize / _initialCameraSize;
            starBackgroundMaterial.SetFloat(CurrentZoom, zoomFactor);
        }
        else
        {
            starBackgroundMaterial.SetFloat(CurrentZoom, 1.0f);
        }
    }

    private void OnDestroy()
    {
        if (starBackgroundMaterial == null || !starBackgroundMaterial.name.Contains("(Instance)")) return;

        starBackgroundMaterial.SetVector(PlayerPosition, Vector4.zero);
        starBackgroundMaterial.SetFloat(CurrentZoom, 1.0f);
    }

    private void OnValidate()
    {
        UpdateShaderProperties();
    }

    private void UpdateShaderProperties()
    {
        if (starBackgroundMaterial == null) return;

        starBackgroundMaterial.SetColor(StarColor1, primaryStarColor);
        starBackgroundMaterial.SetColor(StarColor2, secondaryStarColor);
        starBackgroundMaterial.SetColor(StarColor3, tertiaryStarColor);
        starBackgroundMaterial.SetColor(BackgroundColor, backgroundColor);

        starBackgroundMaterial.SetColor(GradientColor1, gradientColor1);
        starBackgroundMaterial.SetColor(GradientColor2, gradientColor2);
        starBackgroundMaterial.SetColor(GradientColor3, gradientColor3);

        starBackgroundMaterial.SetFloat(NoiseScale, noiseScale);
        starBackgroundMaterial.SetFloat(NoiseStrength, noiseStrength);
        starBackgroundMaterial.SetFloat(NoiseSpeed, noiseSpeed);

        starBackgroundMaterial.SetFloat(ParallaxStrength, parallaxStrength);
        starBackgroundMaterial.SetFloat(StarSizeMin, minStarSize);
        starBackgroundMaterial.SetFloat(StarSizeMax, maxStarSize);
        starBackgroundMaterial.SetFloat(TwinkleSpeed, twinkleSpeed);
        starBackgroundMaterial.SetFloat(TwinkleVariation, twinkleVariation);
        starBackgroundMaterial.SetFloat(StarDensity, starDensity);
        starBackgroundMaterial.SetFloat(Seed, randomSeed);

        starBackgroundMaterial.SetFloat(MinZoom, minZoom);
        starBackgroundMaterial.SetFloat(MaxZoom, maxZoom);
    }
}