// GazeInteractor.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class GazeInteractor : MonoBehaviour
{
    [Header("Raycasters")]
    public List<GraphicRaycaster> worldRaycasters;

    [Header("Indicator")]
    public GazeIndicator indicator;

    [Header("Video Playback")]
    public List<VideoClip> videoClips;
    public SkyboxVideoController skyboxController;

    [Header("Menu Fade Logic")]
    public MenuController menuController;
    [Tooltip("Время фейда оверлея")]
    public float overlayFadeDuration = 0.5f;

    [Header("Crosshair")]
    [Tooltip("CanvasGroup прицела, который нужно скрывать")]
    public CanvasGroup crosshairGroup;

    private PointerEventData pointerData;
    private List<RaycastResult> results = new List<RaycastResult>();
    private int currentTargetIndex = -1;
    private bool isVideoPlaying;

    void Start()
    {
        pointerData = new PointerEventData(EventSystem.current);
        indicator.onFilled.AddListener(OnIndicatorFilled);

        // убедимся, что оверлей и прицел прозрачны/видимы в начале
        if (menuController?.fadeOverlay != null)
            menuController.fadeOverlay.alpha = 0f;
        if (crosshairGroup != null)
            crosshairGroup.alpha = 1f;

        isVideoPlaying = false;
    }

    void Update()
    {
        if (isVideoPlaying)
            return;

        // центр экрана
        Vector2 center = Camera.main.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0));
        pointerData.position = center;

        // поиск под взглядом
        int hitIndex = -1;
        for (int i = 0; i < worldRaycasters.Count; i++)
        {
            results.Clear();
            worldRaycasters[i].Raycast(pointerData, results);
            foreach (var r in results)
            {
                if (r.gameObject.GetComponent<GazeTarget>() != null)
                {
                    hitIndex = i;
                    break;
                }
            }
            if (hitIndex != -1) break;
        }

        bool nowGazing = hitIndex != -1;
        indicator.SetGazeState(nowGazing);

        if (hitIndex != currentTargetIndex)
        {
            indicator.ForceReset();
            currentTargetIndex = hitIndex;
        }
    }

    private void OnIndicatorFilled()
    {
        StartCoroutine(PlayVideoWithFade());
    }

    private IEnumerator PlayVideoWithFade()
    {
        isVideoPlaying = true;

        // 1) Фейд ин оверлея
        menuController.FadeOverlay(1f, overlayFadeDuration);
        yield return new WaitForSeconds(overlayFadeDuration);

        // 2) Скрыть меню, прицел и сам индикатор мгновенно
        menuController.HideMenuInstant();
        indicator.ForceReset();
        indicator.gameObject.SetActive(false);
        if (crosshairGroup != null)
        {
            crosshairGroup.alpha = 0f;
            crosshairGroup.interactable = false;
            crosshairGroup.blocksRaycasts = false;
        }

        // 3) Запустить видео
        if (currentTargetIndex >= 0
            && currentTargetIndex < videoClips.Count
            && videoClips[currentTargetIndex] != null)
        {
            var vp = skyboxController.videoPlayer;
            vp.loopPointReached += OnVideoEnded;
            skyboxController.PlayVideo(videoClips[currentTargetIndex]);
        }
        else
        {
            skyboxController.ClearSkybox();
            OnVideoEnded(null);
        }

        // 4) Скрыть оверлей (fade-out) чтобы показать видео
        menuController.FadeOverlay(0f, overlayFadeDuration);
    }

    private void OnVideoEnded(VideoPlayer vp)
    {
        if (vp != null)
            vp.loopPointReached -= OnVideoEnded;

        StartCoroutine(EndVideoSequence());
    }

    private IEnumerator EndVideoSequence()
    {
        // 5) Фейд ин оверлея перед возвратом меню
        menuController.FadeOverlay(1f, overlayFadeDuration);
        yield return new WaitForSeconds(overlayFadeDuration);

        // 6) Показать меню, прицел и индикатор
        menuController.FadeInAll(0f);
        indicator.gameObject.SetActive(true);
        indicator.ForceReset();

        if (crosshairGroup != null)
        {
            crosshairGroup.alpha = 1f;
            crosshairGroup.interactable = true;
            crosshairGroup.blocksRaycasts = true;
        }

        // очистить skybox
        skyboxController.ClearSkybox();

        // 7) Фейд аут оверлея
        menuController.FadeOverlay(0f, overlayFadeDuration);
        yield return new WaitForSeconds(overlayFadeDuration);

        isVideoPlaying = false;
    }

    void OnDestroy()
    {
        indicator.onFilled.RemoveListener(OnIndicatorFilled);
    }
}
