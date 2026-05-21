using System;
using System.Threading.Tasks;
using ConfigurableReader.Core;

namespace ConfigurableReader.Services;

public class ReaderService : IDisposable
{
    private IBookSource? _source;
    private string _buffer = string.Empty;
    private int _bufferStartPosition = 0;
    public int BufferStartPosition => _bufferStartPosition;
    private const int BufferSize = 50000; // 50k chars buffer
    private const int BufferThreshold = 10000; // Reload when 10k from edge

    private bool _isPaused = true;
    private bool _isReversing = false;

    // Master state: Character-centric coordinates
    private int _currentPosition = 0;
    private double _subCharOffset = 0;

    public int CurrentPosition => _currentPosition;
    public double CurrentOffsetX => -_subCharOffset;
    public int TotalLength => _source?.TotalLength ?? 0;

    public string BufferText
    { 
        get => _buffer; 
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

    public async Task SetSourceAsync(IBookSource source, int initialPosition = 0)
    {
        _source?.Dispose();
        _source = source;
        await LoadBufferAsync(initialPosition);
        _currentPosition = initialPosition;
        _subCharOffset = 0;
        OnStateChanged();
    }

    private async Task LoadBufferAsync(int position)
    {
        if (_source == null) return;

        // Center the buffer around the position
        _bufferStartPosition = Math.Max(0, position - (BufferSize / 2));
        _buffer = await _source.GetTextAsync(_bufferStartPosition, BufferSize);
    }

    // In the new layout-driven model, we don't pass getCharWidth to Update.
    // Instead, the UI/Renderer tells us how much we progressed based on its layout.
    public void Advance(double pixels, Func<int, double, (int newPos, double newOffset, bool eof)> mapPixelsToPosition)
    {
        if (_isPaused || _source == null || string.IsNullOrEmpty(_buffer)) return;

        // mapPixelsToPosition expects positions relative to the buffer or absolute?
        // Existing code used _currentPosition directly into _fullText.
        // We'll keep _currentPosition as absolute and map it.
        
        var result = mapPixelsToPosition(_currentPosition, _isReversing ? -pixels + _subCharOffset : pixels + _subCharOffset);
        
        _currentPosition = result.newPos;
        _subCharOffset = result.newOffset;

        // Check if we need to slide the buffer
        CheckBufferBoundaries();

        if (result.eof || _currentPosition >= TotalLength || (_isReversing && _currentPosition <= 0))
        {
            _isPaused = true;
            if (_isReversing) StartOfBookReached?.Invoke();
            else EndOfBookReached?.Invoke();
        }

        OnStateChanged();
    }

    private void CheckBufferBoundaries()
    {
        // Simple sliding window check
        bool nearEnd = _currentPosition > _bufferStartPosition + BufferSize - BufferThreshold;
        bool nearStart = _currentPosition < _bufferStartPosition + BufferThreshold && _bufferStartPosition > 0;

        if (nearEnd || nearStart)
        {
            // Trigger async load (fire and forget for now, or await in Advance?)
            // To keep it simple and safe, we'll block briefly for the first iteration
            _ = LoadBufferAsync(_currentPosition);
        }
    }

    private void OnStateChanged() => StateChanged?.Invoke();

    public void ResetPosition(int charPosition)
    {
        _currentPosition = Math.Clamp(charPosition, 0, TotalLength);
        _subCharOffset = 0;
        
        // If we jump outside current buffer, reload
        if (_currentPosition < _bufferStartPosition || _currentPosition > _bufferStartPosition + _buffer.Length)
        {
            _ = LoadBufferAsync(_currentPosition);
        }

        OnStateChanged();
    }

    public int FindNext(string query, int startPosition)
    {
        // For search, we still have a problem: we only search in the buffer.
        // A truly virtualized search would need to be implemented in the Source.
        if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(_buffer))
            return -1;

        int relativeStart = startPosition - _bufferStartPosition;
        if (relativeStart < 0) relativeStart = 0;
        if (relativeStart >= _buffer.Length) return -1;

        int found = _buffer.IndexOf(query, relativeStart, StringComparison.OrdinalIgnoreCase);
        return found != -1 ? found + _bufferStartPosition : -1;
    }

    public int FindPrevious(string query, int startPosition)
    {
        if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(_buffer))
            return -1;

        int relativeStart = startPosition - _bufferStartPosition;
        if (relativeStart < 0) return -1;
        int searchStart = Math.Min(relativeStart, _buffer.Length - 1);
        
        int found = _buffer.LastIndexOf(query, searchStart, StringComparison.OrdinalIgnoreCase);
        return found != -1 ? found + _bufferStartPosition : -1;
    }
    


    public void Dispose()
    {
        _source?.Dispose();
    }
}
