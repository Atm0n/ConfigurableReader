using System;

namespace ConfigurableReader.Services;

public class ReaderService
{
    private double _absoluteScrollPixels = 0;
    private string _fullText = string.Empty;
    private bool _isPaused = true;
    private bool _isReversing = false;

    // Derived states
    private int _currentPosition = 0;
    private double _subCharOffset = 0;

    public int CurrentPosition => _currentPosition;
    public double CurrentOffsetX => -_subCharOffset;

    public string FullText 
    { 
        get => _fullText; 
        set { _fullText = value; ResetPosition(0); OnStateChanged(); }
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
            _absoluteScrollPixels -= pixelsToMove;
            if (_absoluteScrollPixels < 0)
            {
                _absoluteScrollPixels = 0;
                _isPaused = true;
                StartOfBookReached?.Invoke();
            }
        }
        else
        {
            _absoluteScrollPixels += pixelsToMove;
        }

        SyncDerivations(getCharWidth);
        OnStateChanged();
    }

    private void SyncDerivations(Func<char, double> getCharWidth)
    {
        if (string.IsNullOrEmpty(_fullText)) return;

        // Optimization: For small changes, move incrementally
        // For large jumps, recalculate from the beginning.
        // This handles smooth scrolling deeply into the book without lag.
        
        // Start from current if within reasonable range, otherwise 0
        double currentTotal = 0;
        int searchStart = 0;

        // For performance, we'll just always track incrementally from 0 during regular reading
        // but this logic is called every frame, so it must be fast.
        
        // Let's keep a cached absolute position of the current character
        // No, let's just use the absolute distance and find where it lands.
        
        double pixelsFound = 0;
        int pos = 0;

        while (pos < _fullText.Length)
        {
            double w = getCharWidth(_fullText[pos]);
            if (pixelsFound + w > _absoluteScrollPixels)
            {
                break;
            }
            pixelsFound += w;
            pos++;
        }

        if (pos >= _fullText.Length)
        {
            _currentPosition = _fullText.Length;
            _subCharOffset = 0;
            _absoluteScrollPixels = pixelsFound; // Cap at end
            _isPaused = true;
            EndOfBookReached?.Invoke();
        }
        else
        {
            _currentPosition = pos;
            _subCharOffset = _absoluteScrollPixels - pixelsFound;
        }
    }

    private void OnStateChanged() => StateChanged?.Invoke();

    public void ResetPosition(int charPosition, Func<char, double> getCharWidth)
    {
        _currentPosition = Math.Clamp(charPosition, 0, _fullText.Length);
        _absoluteScrollPixels = 0;
        for (int i = 0; i < _currentPosition; i++)
        {
            _absoluteScrollPixels += getCharWidth(_fullText[i]);
        }
        _subCharOffset = 0;
        OnStateChanged();
    }

    public void ResetPosition(int charPosition)
    {
        _currentPosition = Math.Clamp(charPosition, 0, _fullText.Length);
        _absoluteScrollPixels = 0;
        _subCharOffset = 0;
        OnStateChanged();
    }
}
