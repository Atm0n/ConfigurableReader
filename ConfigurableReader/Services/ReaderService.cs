using System;

namespace ConfigurableReader.Services;

public class ReaderService
{
    private string _fullText = string.Empty;
    private bool _isPaused = true;
    private bool _isReversing = false;

    // Master state: Character-centric coordinates
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

    // In the new layout-driven model, we don't pass getCharWidth to Update.
    // Instead, the UI/Renderer tells us how much we progressed based on its layout.
    public void Advance(double pixels, Func<int, double, (int newPos, double newOffset, bool eof)> mapPixelsToPosition)
    {
        if (_isPaused || string.IsNullOrEmpty(_fullText)) return;

        var result = mapPixelsToPosition(_currentPosition, _isReversing ? -pixels + _subCharOffset : pixels + _subCharOffset);
        
        _currentPosition = result.newPos;
        _subCharOffset = result.newOffset;

        if (result.eof)
        {
            _isPaused = true;
            if (_isReversing) StartOfBookReached?.Invoke();
            else EndOfBookReached?.Invoke();
        }

        OnStateChanged();
    }

    private void OnStateChanged() => StateChanged?.Invoke();

    public void ResetPosition(int charPosition)
    {
        _currentPosition = Math.Clamp(charPosition, 0, _fullText.Length);
        _subCharOffset = 0;
        OnStateChanged();
    }
    
    // Compatibility shim for existing calls while we refactor
    public void ResetPosition(int charPosition, Func<char, double> unused) => ResetPosition(charPosition);
}
