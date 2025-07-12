using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ChessModel;
using Exploder;
using Exploder.Utils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class BoardManager : MonoBehaviour
{
    private TileManager _tileManager;
    private PieceManager _pieceManager;
    private ObjectPool _objectPool;
    public PromotionUIScript _promotionScript;
    public EndGameUI EndGameUI;

    // غيرنا من private إلى public
    public ChessBoard _chessBoard;
    private Dictionary<ChessColor, Player.Player> _players;
    private List<Move> _legalMoves;
    public static bool _humanPlayer;
    public bool playing { get; set; }
    private bool paused;

    public GameObject whiteCam;
    public GameObject menuCam;

    private Dictionary<Piece, GameObject> _map;
    private Piece _selectedPiece;
    private List<Move> _currentLegalMoves;
    private bool _firstClick = true;

    void Start()
    {
        // Get required components
        _tileManager = GetComponentInChildren<TileManager>();
        _pieceManager = GetComponentInChildren<PieceManager>();
        _objectPool = GetComponentInChildren<ObjectPool>();

        // Initialize chess game
        _chessBoard = new ChessBoard(this);
        _chessBoard.Rock += RockDone;

        _map = new Dictionary<Piece, GameObject>(32);
        _humanPlayer = false;
        _firstClick = true;
        playing = false;
        paused = true;
        _legalMoves = new List<Move>();

        // Setup initial board
        _chessBoard.InitializeBoard();
        SetupInitialPieces();
    }

    private void SetupInitialPieces()
    {
        foreach (var piece in _chessBoard.Board)
        {
            if (piece.Type != ChessType.None)
            {
                _map.Add(piece, createPieceOnPlacement(piece.Type, piece.Color, piece.Position));
                if (piece.Color == ChessColor.Black)
                {
                    _map[piece].transform.Rotate(0, 180, 0);
                }
            }
        }
    }

    public void RestartGame()
    {
        _chessBoard.InitializeBoard();

        FragmentPool.Instance.DeactivateFragments();
        FragmentPool.Instance.DestroyFragments();
        FragmentPool.Instance.Reset(ExploderSingleton.Instance.Params);

        foreach (var piece in _map)
        {
            piece.Value.SetActive(false);
        }

        _humanPlayer = false;
        _legalMoves.Clear();
        _tileManager.updateLegalMoves(_legalMoves);
        _firstClick = true;

        _map.Clear();
        foreach (var piece in _chessBoard.Board)
        {
            if (piece.Type == ChessType.None) continue;
            GameObject gameObjectPiece = createPieceOnPlacement(piece.Type, piece.Color, piece.Position);
            gameObjectPiece.GetComponent<PiecePieces>().ResetMovement();
            _map.Add(piece, gameObjectPiece);
        }
        playing = true;
    }

    public void InitialisePlay(Dictionary<ChessColor, Player.Player> players)
    {
        if (players[ChessColor.White] != null && players[ChessColor.Black] == null)
        {
            whiteCam.SetActive(false);
        }

        _players = players;
        menuCam.SetActive(false);
        GetComponent<AudioSource>()?.Play();
        playing = true;
        paused = true;
    }

    private void MovePiece(GameObject piece, Move move, bool rock = false)
    {
        int position = move.EndPosition;
        if (move.Eat)
        {
            _pieceManager.AttackWithPiece(piece, _tileManager.getCoordinatesByTilePlacement(position), _tileManager.getCoordinatesByTilePlacement(move.EatenPiece.Position), _map[move.EatenPiece]);
        }
        else
        {
            _pieceManager.MovePiece(piece, _tileManager.getCoordinatesByTilePlacement(position), rock);
        }
    }

    public void ClickTile(int placement)
    {
        if (!_humanPlayer || !playing)
        {
            return;
        }

        // First interaction - selecting a piece
        if (_firstClick)
        {
            HandlePieceSelection(placement);
        }
        // Second interaction - moving the piece
        else
        {
            HandlePieceMovement(placement);
        }
    }

    private void HandlePieceSelection(int placement)
    {
        var piece = _chessBoard.GetPiece(placement);
        if (piece != null && piece.Color == _chessBoard.NextToPlay)
        {
            _selectedPiece = piece;
            _currentLegalMoves = _chessBoard.GetMoveFromPosition(placement);

            if (_currentLegalMoves != null && _currentLegalMoves.Any())
            {
                _firstClick = false;
                _legalMoves = _currentLegalMoves;
                _tileManager.updateLegalMoves(_legalMoves);
            }
            else
            {
                _selectedPiece = null;
            }
        }
    }

    private void HandlePieceMovement(int placement)
    {
        var validMove = _currentLegalMoves?.FirstOrDefault(move => move.EndPosition == placement);

        if (validMove != null)
        {
            _humanPlayer = false;
            _chessBoard.Play(validMove);
            MovePiece(_map[validMove.Piece], validMove);

            // Reset state
            ResetSelectionState();
        }
        else
        {
            // If invalid move, try selecting a new piece
            _firstClick = true;
            ClickTile(placement);
        }
    }

    private void ResetSelectionState()
    {
        _legalMoves.Clear();
        _tileManager.updateLegalMoves(_legalMoves);
        _currentLegalMoves = null;
        _selectedPiece = null;
        _firstClick = true;
    }

    public bool HasLegalMoves()
    {
        return _currentLegalMoves != null && _currentLegalMoves.Any();
    }

    private bool IsLegalMove(Move move)
    {
        return _currentLegalMoves?.Any(m => m.EndPosition == move.EndPosition) ?? false;
    }

    private void Update()
    {
        if (playing && paused)
        {
            paused = false;
            NextTurn();
        }

        if (!playing && !paused)
        {
            paused = true;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            SwapCam();
            //Debug.Log(_chessBoard.GetEvaluationScore());
        }
    }

    private void RockDone(object sender, Move move)
    {
        MovePiece(_map[move.Piece], move, true);
    }

    public void Promotion(Piece piece)
    {
        StartCoroutine(PromotionWait(piece));
        playing = false;
    }

    private IEnumerator PromotionWait(Piece piece)
    {
        yield return new WaitForSeconds(1f);
        yield return new WaitUntil(() => _map[piece].GetComponent<PiecePieces>()._arrived);
        if (_players[_chessBoard.NextToPlay.Reverse()] == null)
        {
            _promotionScript.show(piece);
        }
        else GivePromotion(piece, ChessType.Queen);
    }

    public void GivePromotion(Piece piece, ChessType chessType)
    {
        switch (chessType)
        {
            case ChessType.Bishop:
                _map[piece].SetActive(false);
                _map[piece] = _objectPool.getPooledPiece(ChessType.Bishop, piece.Color, _tileManager.getCoordinatesByTilePlacement(piece.Position));
                break;
            case ChessType.Rook:
                _map[piece].SetActive(false);
                _map[piece] = _objectPool.getPooledPiece(ChessType.Rook, piece.Color, _tileManager.getCoordinatesByTilePlacement(piece.Position));
                break;
            case ChessType.Queen:
                _map[piece].SetActive(false);
                _map[piece] = _objectPool.getPooledPiece(ChessType.Queen, piece.Color, _tileManager.getCoordinatesByTilePlacement(piece.Position));
                break;
            case ChessType.Knight:
                _map[piece].SetActive(false);
                _map[piece] = _objectPool.getPooledPiece(ChessType.Knight, piece.Color, _tileManager.getCoordinatesByTilePlacement(piece.Position));
                break;
        }
        _chessBoard.PromotePawn(piece, chessType);
        playing = true;
    }

    public void EndGameWin(ChessColor color, Piece piece)
    {
        StartCoroutine(EndGameWin2(color, piece));
    }

    public void EndGameNull(Piece piece)
    {
        StartCoroutine(EndGameNull2(piece));
    }

    private IEnumerator EndGameWin2(ChessColor color, Piece piece)
    {
        playing = false;
        yield return new WaitForSeconds(1f);
        yield return new WaitUntil(() => _map[piece].GetComponent<PiecePieces>()._arrived);
        EndGameUI.EndGameWin(color);
    }

    private IEnumerator EndGameNull2(Piece piece)
    {
        playing = false;
        yield return new WaitForSeconds(1f);
        yield return new WaitUntil(() => _map[piece].GetComponent<PiecePieces>()._arrived);
        EndGameUI.EndGameNull();
    }

    private GameObject createPieceOnPlacement(ChessType type, ChessColor color, int position)
    {
        Vector3 coordinates = _tileManager.getCoordinatesByTilePlacement(position);
        return _objectPool.getPooledPiece(type, color, coordinates);
    }

    public void NextTurn()
    {
        if (paused) return;

        var nextToPlay = _chessBoard.NextToPlay;
        var currentPlayer = _players[nextToPlay];

        if (currentPlayer == null)
        {
            whiteCam.SetActive(nextToPlay == ChessColor.White);
            _humanPlayer = true;
        }
        else
        {
            var move = currentPlayer.GetDesiredMove();
            if (move != null)
            {
                MovePiece(_map[move.Piece], move);
                _chessBoard.Play(move);
            }
        }
    }

    public bool HasPieceOnTile(int position)
    {
        var piece = _chessBoard.GetPiece(position);
        return piece != null && piece.Type != ChessType.None;
    }

    private void SwapCam()
    {
        if (whiteCam != null)
        {
            whiteCam.SetActive(!whiteCam.activeInHierarchy);
        }
    }

    // إضافة دالة GetPiece
    public Piece GetPiece(int position)
    {
        return _chessBoard?.GetPiece(position);
    }

    // إضافة دالة للحصول على اللاعب الحالي
    public ChessColor GetCurrentPlayer()
    {
        return _chessBoard?.NextToPlay ?? ChessColor.White;
    }
}