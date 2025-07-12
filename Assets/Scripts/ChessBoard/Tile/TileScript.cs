using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;
using System.Collections.Generic;

public class TileScript : MonoBehaviour
{
    [SerializeField] private GameObject highlight;      // الأخضر: تحديد القطعة أو البلاطة اللي عليها الدور
    private GameObject _tileHighlight;                  // الأحمر: الأماكن القانونية للحركة
    private TileManager _tileManager;
    private BoardManager _boardManager;
    public int TilePlacement { get; private set; }

    private static TileScript selectedTile; // البلاطة المختارة حالياً
    private float dwellTime = 2f;
    private float hoverStartTime;
    private bool isHovering;
    private bool isSelected;
    private bool isLegalMove;

    private static HashSet<int> legalMoves = new HashSet<int>();

    private static float lastMoveTime;
    private static bool hasSelectedPiece => selectedTile != null;
    private const float AUTO_DESELECT_TIME = 5f;

    void Start()
    {
        _tileManager = GetComponentInParent<TileManager>();
        _boardManager = GetComponentInParent<BoardManager>();
        _tileHighlight = transform.Find("TileHighlight")?.gameObject;

        // حساب ترتيب البلاطة
        Vector3 localPosition = transform.localPosition;
        Vector3 localScale = transform.localScale;
        TilePlacement = (int)(localPosition.z / (10 * localScale.z)) * 8 + (int)(localPosition.x / (10 * localScale.x));

        // غلق كل الهايلايت في الأول
        if (highlight) highlight.SetActive(false);
        if (_tileHighlight) _tileHighlight.SetActive(false);

        // XR Events
        var interactable = GetComponent<XRSimpleInteractable>();
        if (interactable != null)
        {
            interactable.hoverEntered.AddListener((args) => OnHoverEnter());
            interactable.hoverExited.AddListener((args) => OnHoverExit());
        }
    }

    void Update()
    {
        // التحقق من الوقت المنقضي منذ آخر حركة إذا كان هناك قطعة محددة
        if (hasSelectedPiece && Time.time - lastMoveTime > AUTO_DESELECT_TIME)
        {
            DeselectCurrentTile();
        }

        if (isHovering && BoardManager._humanPlayer)
        {
            float hoverDuration = Time.time - hoverStartTime;

            if (hoverDuration >= dwellTime)
            {
                if (selectedTile != null)
                {
                    ExecuteMove();
                }
                else
                {
                    SelectTile();
                }
                isHovering = false;
            }
        }
    }

    private void OnHoverEnter()
    {
        if (!BoardManager._humanPlayer) return;

        isHovering = true;
        hoverStartTime = Time.time;
        lastMoveTime = Time.time; // تحديث وقت آخر حركة

        // لو مفيش حاجة محددة، وإنت واقف على قطعة ينفع تتحرك، خلي البلاطة دي خضرا
        if (selectedTile == null)
        {
            var piece = _boardManager.GetPiece(TilePlacement);
            if (piece != null && piece.Color == _boardManager._chessBoard.NextToPlay)
            {
                ShowGreenHighlight();
            }
        }
        // لو في قطعة محددة وأنت واقف على مربع قانوني، اظهر التأثير الأخضر
        else if (isLegalMove)
        {
            ShowGreenHighlight();
        }
    }

    private void OnHoverExit()
    {
        isHovering = false;

        // لما تطلع من البلاطة، شيل الأخضر (إلا لو هي القطعة المختارة فعلاً)
        if (!isSelected)
            HideGreenHighlight();
    }

    private void ShowGreenHighlight()
    {
        if (highlight)
        {
            highlight.SetActive(true);
            highlight.transform.position = transform.position;
        }
    }

    private void HideGreenHighlight()
    {
        if (highlight) highlight.SetActive(false);
    }

    public void HighlightTile()
    {
        // تم نقل المنطق إلى ShowLegalMoves
    }

    public void UnHighlightTile()
    {
        // تم نقل المنطق إلى ClearLegalMoves
    }

    private void ExecuteMove()
    {
        if (_boardManager == null || !isLegalMove) return;

        _boardManager.ClickTile(TilePlacement);

        // شيل كل الهايلايتات بعد الحركة
        DeselectCurrentTile();
        isHovering = false;
        hoverStartTime = 0;
        HideGreenHighlight();
        if (_tileHighlight) _tileHighlight.SetActive(false);
    }

    private void ShowLegalMoves()
    {
        // نجيب كل الحركات القانونية للقطعة
        var moves = _boardManager._chessBoard.GetMoveFromPosition(TilePlacement);
        legalMoves = new HashSet<int>(moves.Select(m => m.EndPosition));

        // نظهر الأحمر في كل المربعات القانونية
        foreach (TileScript tile in FindObjectsOfType<TileScript>())
        {
            if (legalMoves.Contains(tile.TilePlacement))
            {
                tile.isLegalMove = true;
                if (tile._tileHighlight != null)
                    tile._tileHighlight.SetActive(true);
            }
            else
            {
                tile.isLegalMove = false;
                if (tile._tileHighlight != null)
                    tile._tileHighlight.SetActive(false);
            }
        }
    }

    private static void ClearLegalMoves()
    {
        foreach (TileScript tile in FindObjectsOfType<TileScript>())
        {
            tile.isLegalMove = false;
            if (tile._tileHighlight != null)
                tile._tileHighlight.SetActive(false);
        }
        legalMoves.Clear();
    }

    private void SelectTile()
    {
        if (_boardManager == null) return;

        var piece = _boardManager.GetPiece(TilePlacement);
        if (piece == null || piece.Color != _boardManager._chessBoard.NextToPlay) return;

        _boardManager.ClickTile(TilePlacement);

        if (_boardManager.HasLegalMoves())
        {
            // شيل كل الهايلايتات القديمة
            if (selectedTile != null) selectedTile.ClearSelection();

            selectedTile = this;
            isSelected = true;
            lastMoveTime = Time.time; // تحديث وقت الاختيار

            // أظهر الأخضر على القطعة دي بس
            ShowGreenHighlight();
            
            // أظهر الأحمر على المربعات القانونية
            ShowLegalMoves();
        }
    }

    public static void DeselectCurrentTile()
    {
        if (selectedTile != null)
        {
            selectedTile.ClearSelection();
            selectedTile = null;
            ClearLegalMoves();
        }
    }

    private void ClearSelection()
    {
        isSelected = false;
        HideGreenHighlight();
    }
}
