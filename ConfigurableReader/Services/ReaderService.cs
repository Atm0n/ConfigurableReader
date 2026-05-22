using System;
using System.Threading.Tasks;
using ConfigurableReader.Core;

namespace ConfigurableReader.Services;

public class ReaderService : IDisposable
{
    private IBookSource? _source;
    public IBookSource? CurrentSource => _source;
    private string _buffer = string.Empty;
    private int _bufferStartPosition = 0;
    public int BufferStartPosition => _bufferStartPosition;
    private const int BufferSize = 50000; // 50k chars buffer
    private const int BufferThreshold = 10000; // Reload when 10k from edge
    private long _latestLoadId = 0;

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

        long loadId = ++_latestLoadId;
        int newStartPosition = Math.Max(0, position - (BufferSize / 2));
        
        string newBuffer = await _source.GetTextAsync(newStartPosition, BufferSize);
        
        if (loadId == _latestLoadId)
        {
            _bufferStartPosition = newStartPosition;
            _buffer = newBuffer;
        }
    }

    /// <summary>
    /// Advances the reading position by the given number of pixels.
    /// The caller supplies a <paramref name="mapPixelsToPosition"/> delegate that translates a
    /// pixel offset (relative to the current character) into a new absolute character position,
    /// using the rendered text layout for accurate kerning-aware mapping.
    /// </summary>
    public void Advance(double pixels, Func<int, double, (int newPos, double newOffset, bool eof)> mapPixelsToPosition)
    {
        if (_isPaused || _source == null || string.IsNullOrEmpty(_buffer)) return;

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

    public async Task<int> FindNextAsync(string query, int startPosition)
    {
        if (string.IsNullOrEmpty(query) || _source == null)
            return -1;

        int totalLength = _source.TotalLength;
        if (startPosition < 0) startPosition = 0;
        if (startPosition >= totalLength) return -1;

        int chunkSize = 100000;
        int overlap = query.Length - 1;
        int currentStart = startPosition;

        while (currentStart < totalLength)
        {
            string chunk = await _source.GetTextAsync(currentStart, chunkSize);
            if (string.IsNullOrEmpty(chunk))
                break;

            int foundIndex = chunk.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (foundIndex != -1)
            {
                return currentStart + foundIndex;
            }

            if (chunk.Length <= overlap)
            {
                break;
            }
            currentStart += chunk.Length - overlap;
        }

        return -1;
    }

    public async Task<int> FindPreviousAsync(string query, int startPosition)
    {
        if (string.IsNullOrEmpty(query) || _source == null)
            return -1;

        int totalLength = _source.TotalLength;
        if (startPosition < 0) return -1;
        
        int startPos = Math.Min(startPosition, totalLength - 1);
        int chunkSize = 100000;
        int overlap = query.Length - 1;
        
        int currentEnd = startPos + 1;

        while (currentEnd > 0)
        {
            int currentStart = Math.Max(0, currentEnd - chunkSize);
            int readCount = currentEnd - currentStart;
            
            string chunk = await _source.GetTextAsync(currentStart, readCount);
            if (string.IsNullOrEmpty(chunk))
                break;

            int foundIndex = chunk.LastIndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (foundIndex != -1)
            {
                return currentStart + foundIndex;
            }

            if (readCount <= overlap)
            {
                break;
            }
            currentEnd = currentStart + overlap;
        }

        return -1;
    }
    


    public void Dispose()
    {
        _source?.Dispose();
    }
}
