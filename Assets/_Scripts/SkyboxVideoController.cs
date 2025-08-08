using UnityEngine;
using UnityEngine.Video;

public class SkyboxVideoController : MonoBehaviour
{
    [Header("Video Setup")]
    public VideoPlayer videoPlayer;
    public RenderTexture renderTexture;

    [Header("Skybox Materials")]
    public Material videoSkyboxMaterial; // Шейдер Skybox/Panoramic или 6-Sided, с любым placeholder _MainTex
    public Material blackSkyboxMaterial; // Материал Skybox/Panoramic или Procedural, настроенный под чисто чёрный

    private Material originalSkybox;
    private Camera mainCam;

    void Awake()
    {
        mainCam = Camera.main;
        originalSkybox = RenderSettings.skybox;
    }

    void Start()
    {
        ClearSkybox();
    }

    /// <summary>
    /// Запустить видеоклип из VideoClip поля.
    /// </summary>
    public void PlayVideo(VideoClip clip)
    {
        // Сброс подписчиков, чтобы не дублировать вызов
        videoPlayer.prepareCompleted -= OnPrepared;
        videoPlayer.prepareCompleted += OnPrepared;

        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = clip;
        SetupAndPlay();
    }

    /// <summary>
    /// Запустить видео по URL (например, из StreamingAssets).
    /// </summary>
    public void PlayVideo(string url)
    {
        videoPlayer.prepareCompleted -= OnPrepared;
        videoPlayer.prepareCompleted += OnPrepared;

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        SetupAndPlay();
    }

    /// <summary>
    /// Остановить видео и сделать чёрный skybox.
    /// </summary>
    public void ClearSkybox()
    {
        // Остановим видео, если играет
        if (videoPlayer.isPlaying) videoPlayer.Stop();

        // Поставим чёрный skybox или просто чистый фон
        if (blackSkyboxMaterial != null)
        {
            RenderSettings.skybox = blackSkyboxMaterial;
            mainCam.clearFlags = CameraClearFlags.Skybox;
        }
        else
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = Color.black;
        }
    }

    /// <summary>
    /// Общая логика подготовки RenderTexture и skybox перед Play().
    /// </summary>
    private void SetupAndPlay()
    {
        // 1) Настроить VideoPlayer на вывод в RT
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.isLooping = false;

        // 2) Подменить skybox на видео-материал и впихнуть туда RT
        videoSkyboxMaterial.SetTexture("_MainTex", renderTexture);
        RenderSettings.skybox = videoSkyboxMaterial;

        // 3) Камера должна чистить в Skybox
        mainCam.clearFlags = CameraClearFlags.Skybox;

        // 4) Подготовить и дождаться готовности
        videoPlayer.Prepare();
    }

    /// <summary>
    /// Вызывается, когда VideoPlayer подготовился — тогда стартуем.
    /// </summary>
    private void OnPrepared(VideoPlayer vp)
    {
        vp.Play();
    }

    void OnDestroy()
    {
        // Убираем подписку, чтобы не было утечек
        videoPlayer.prepareCompleted -= OnPrepared;
    }
}
