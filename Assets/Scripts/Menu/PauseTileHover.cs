using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class PauseTileHover : MonoBehaviour
{
    public GameObject pauseMenu;
    public MainMenuScpirt mainMenu; // مرجع للـ MainMenuScpirt
    public float hoverDuration = 2f;
    
    private float hoverTimer = 0f;
    private bool isHovering = false;
    private BoardManager boardManager;

    private void Start()
    {
        boardManager = FindObjectOfType<BoardManager>();
        if (mainMenu == null)
            mainMenu = FindObjectOfType<MainMenuScpirt>();
    }

    public void OnHoverEntered(HoverEnterEventArgs args)
    {
        isHovering = true;
        hoverTimer = 0f;
    }

    public void OnHoverExited(HoverExitEventArgs args)
    {
        isHovering = false;
        hoverTimer = 0f;
    }

    private void Update()
    {
        if (isHovering)
        {
            hoverTimer += Time.deltaTime;
            
            if (hoverTimer >= hoverDuration && !pauseMenu.activeSelf)
            {
                pauseMenu.SetActive(true);
                if (boardManager != null)
                    boardManager.playing = false;
            }
        }
    }

    // دالة لإخفاء القائمة - يمكن استدعاؤها من أزرار المنيو
    public void HidePauseMenu()
    {
        pauseMenu.SetActive(false);
        if (boardManager != null)
            boardManager.playing = true;
    }

    // دالة استئناف اللعب
    public void Resume()
    {
        if (boardManager != null)
            boardManager.playing = true;
        pauseMenu.SetActive(false);
    }

    // دالة إعادة تشغيل اللعبة
    public void RestartGame()
    {
        if (boardManager != null)
        {
            boardManager.RestartGame();
            boardManager.playing = true;
        }
        pauseMenu.SetActive(false);
    }

    // دالة العودة للقائمة الرئيسية
    public void BackToMenu()
    {
        pauseMenu.SetActive(false);
        if (mainMenu != null)
        {
            // استخدام BackToMenu من MainMenuScpirt مباشرة
            mainMenu.BackToMenu();
        }
    }
}
