using System;

namespace ConfigurableReader.Services;

public class ReaderService
{
    private int _currentPosition = 0;
    private double _currentOffsetX = 0;
    private string _fullText = string.Empty;
    private bool _isPaused = true;
    private bool _isReversing = false;

    public int CurrentPosition 
    { 
        get => _currentPosition; 
        set { _currentPosition = value; OnStateChanged(); }
    }

    public double CurrentOffsetX 
    { 
        get => _currentOffsetX; 
        set { _currentOffsetX = value; OnStateChanged(); }
    }

    public string FullText 
    { 
        get => _fullText; 
        set { _fullText = value; OnStateChanged(); }
    }

    public bool IsPaused 
    { 
        get => _isPaused; 
        set { _isPaused = value; OnStateChanged(); }
    }

    public bool IsReversing 
    { 
        get => _isReversing; 
        set { _isReversing = value; OnStateChanged(); }
    }

    public event Action? StateChanged;
    public event Action? StartOfBookReached;
    public event Action? EndOfBookReached;

    public void Update(double deltaTime, double speed, Func<char, double> getCharWidth)
    {
        if (_isPaused || string.IsNullOrEmpty(_fullText)) return;

        double pixelsToMove = speed * deltaTime;

        if (_isReversing)
        {
            _currentOffsetX += pixelsToMove;
            while (_currentOffsetX > 0)
            {
                if (_currentPosition > 0)
                {
                    _currentPosition--;
                    _currentOffsetX -= getCharWidth(_fullText[_currentPosition]);
                }
                else
                {
                    _currentOffsetX = 0;
                    _isPaused = true;
                    StartOfBookReached?.Invoke();
                    break;
                }
            }
        }
        else
        {
            _currentOffsetX -= pixelsToMove;
            while (true)
            {
                if (_currentPosition >= _fullText.Length)
                {
                    _currentOffsetX = 0;
                    _isPaused = true;
                    EndOfBookReached?.Invoke();
                    break;
                }

                double charWidth = getCharWidth(_fullText[_currentPosition]);
                if (_currentOffsetX <= -charWidth)
                {
                    _currentOffsetX += charWidth;
                    _currentPosition++;
                }
                else
                {
                    break;
                }
            }
        }

        OnStateChanged();
    }

    private void OnStateChanged() => StateChanged?.Invoke();

    public void ResetPosition(int position = 0)
    {
        _currentPosition = position;
        _currentOffsetX = 0;
        OnStateChanged();
    }
}
