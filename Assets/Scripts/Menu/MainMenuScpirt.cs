using System;
using System.Collections;
using System.Collections.Generic;
using ChessModel;
using Player;
using UnityEngine;

public class MainMenuScpirt : MonoBehaviour
{
    public GameObject firstMenu;
    public GameObject modeSelection;
    public GameObject colorSelection;
    public GameObject aiDifficulty;
    
    public BoardManager boardManager;

    private GameMode _gameMode;
    private Dictionary<ChessColor, Player.Player> _players;
    private ChessColor _playerColor;

    void Start()
    {
        firstMenu.SetActive(true);
        modeSelection.SetActive(false);
        colorSelection.SetActive(false);
        aiDifficulty.SetActive(false);

        _players = new Dictionary<ChessColor, Player.Player>
        {
            [ChessColor.Black] = null,
            [ChessColor.White] = null
        };

        _playerColor = ChessColor.White;
    }

    private IEnumerator TravelToMenu()
    {
        yield return new WaitForSeconds(2f);
        boardManager.RestartGame();
        boardManager.playing = false;
        firstMenu.SetActive(true);
        _players = new Dictionary<ChessColor, Player.Player>
        {
            [ChessColor.Black] = null,
            [ChessColor.White] = null
        };

        _playerColor = ChessColor.White;
    }
    public void BackToMenu()
    {
        boardManager.menuCam.SetActive(true);
        boardManager.GetComponent<AudioSource>().Stop();
        GetComponent<AudioSource>().Play();
        boardManager.whiteCam.SetActive(true);
        StartCoroutine(TravelToMenu());
    }

    public void SelectColorWhite()
    {
        SelectColor(ChessColor.Black); // لاحظ، لو في خطأ هنا عدله للعكس المطلوب
    }

    public void SelectColorBlack()
    {
        SelectColor(ChessColor.White); // لاحظ، لو في خطأ هنا عدله للعكس المطلوب
    }

    private void SelectColor(ChessColor color)
    {
        _playerColor = color;
        colorSelection.SetActive(false);
        ShowAiDifficulty();
    }

    private void ShowColorSelection()
    {
        if (_gameMode == GameMode.Pvai)
        {
            colorSelection.SetActive(true);
        }
        else
        {
            ShowAiDifficulty();
            _playerColor = ChessColor.White;
        }
    }

    private void StartGame()
    {
        GetComponent<AudioSource>().Stop();
        boardManager.InitialisePlay(_players);
    }

    private void ShowAiDifficulty()
    {
        if (_gameMode == GameMode.Pvai || _gameMode == GameMode.Aivai)
        {
            aiDifficulty.SetActive(true);
        }
        else
        {
            StartGame();
        }
    }

    public void SelectDifficultyRandom()
    {
        AssignDifficulty(Difficulty.Random);
    }
    
    public void SelectDifficultyEasy()
    {
        AssignDifficulty(Difficulty.Easy);
    }
    
    public void SelectDifficultyMedium()
    {
        AssignDifficulty(Difficulty.Medium);
    }
    
    public void SelectDifficultyHard()
    {
        AssignDifficulty(Difficulty.Hard);
    }

    private void AssignDifficulty(Difficulty difficulty)
    {
        Player.Player player = difficulty == Difficulty.Random ? new RandomPlayer(_playerColor) : (Player.Player) new MinmaxPlayer(_playerColor, (int)difficulty);

        if (_playerColor == ChessColor.White)
        {
            _players[ChessColor.White] = player;
        }
        else
        {
            _players[ChessColor.Black] = player;
        }

        if (_gameMode == GameMode.Aivai && _playerColor == ChessColor.White)
        {
            _playerColor = ChessColor.Black;
        }
        else
        {
            StartGame();
            aiDifficulty.SetActive(false);
        }
    }

    private void SelectGameMode(GameMode gameMode)
    {
        modeSelection.SetActive(false);
        _gameMode = gameMode;
        ShowColorSelection();
    }

    public void SelectPvpGameMode()
    {
        SelectGameMode(GameMode.Pvp);
    }

    public void SelectPvAiGameMode()
    {
        SelectGameMode(GameMode.Pvai);
    }

    public void SelectAivaiGameMode()
    {
        SelectGameMode(GameMode.Aivai);
    }

    public void ToMainMenuFromModeSelection()
    {
        firstMenu.SetActive(true);
        modeSelection.SetActive(false);
    }

    public void ToGameModeSelectionFromMainMenu()
    {
        firstMenu.SetActive(false);
        modeSelection.SetActive(true);
    }
    
    public void Exit()
    {
        Application.Quit();
    }

    public enum GameMode
    {
        Pvp,
        Pvai,
        Aivai
    }

    private enum Difficulty
    {
        Random,
        Easy = 1,
        Medium = 2,
        Hard = 3
    }
}
