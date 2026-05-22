using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConfigurableReader.Core;
using ConfigurableReader.Models;

namespace ConfigurableReader.Services;

public class ReaderController
{
    private readonly DocumentRegistry _documentRegistry;
    private readonly ReaderService _readerService;
    private List<BookRecord> _bookRecords = [];

    public string? CurrentBookFilePath { get; private set; }
    public BookRecord? CurrentRecord => CurrentBookFilePath != null ? GetOrCreateRecord(CurrentBookFilePath) : null;

    private int _suppressCodeUpdatesCount;
    public bool IsUpdatingFromCode => _suppressCodeUpdatesCount > 0;

    public IDisposable SuppressCodeUpdates()
    {
        return new UpdateGuard(this);
    }

    private class UpdateGuard : IDisposable
    {
        private readonly ReaderController _controller;
        private bool _disposed;

        public UpdateGuard(ReaderController controller)
        {
            _controller = controller;
            System.Threading.Interlocked.Increment(ref _controller._suppressCodeUpdatesCount);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                System.Threading.Interlocked.Decrement(ref _controller._suppressCodeUpdatesCount);
                _disposed = true;
            }
        }
    }

    public ReaderController(DocumentRegistry documentRegistry, ReaderService readerService)
    {
        _documentRegistry = documentRegistry ?? throw new ArgumentNullException(nameof(documentRegistry));
        _readerService = readerService ?? throw new ArgumentNullException(nameof(readerService));
        LoadBookRecords();
    }

    public void LoadBookRecords()
    {
        _bookRecords = BookRecordStore.Load();
    }

    public List<BookRecord> BookRecords => _bookRecords;

    public BookRecord GetOrCreateRecord(string filePath)
    {
        var record = _bookRecords.FirstOrDefault(r => r.FilePath == filePath);
        if (record == null)
        {
            record = new BookRecord { FilePath = filePath };
            _bookRecords.Add(record);
        }
        return record;
    }

    public void SaveCurrentPosition()
    {
        if (CurrentBookFilePath != null)
        {
            var record = GetOrCreateRecord(CurrentBookFilePath);
            record.ScrollPosition = _readerService.CurrentPosition;
            record.LastReadDate = DateTime.Now;
            BookRecordStore.Save(_bookRecords);
        }
    }

    public async Task<string> OpenBookAsync(string filePath)
    {
        CurrentBookFilePath = filePath;
        var source = await _documentRegistry.CreateSourceAsync(filePath);
        var record = GetOrCreateRecord(filePath);
        
        record.LastReadDate = DateTime.Now;
        record.TotalLength = source.TotalLength;
        // Optionally extract title if supported by the parser in the future, for now fallback to filename
        if (string.IsNullOrEmpty(record.Title))
        {
            record.Title = Path.GetFileNameWithoutExtension(filePath);
        }

        using (SuppressCodeUpdates())
        {
            await _readerService.SetSourceAsync(source, record.ScrollPosition);
        }

        BookRecordStore.Save(_bookRecords);
        return record.Title;
    }

    public void RemoveBookRecord(BookRecord record)
    {
        _bookRecords.Remove(record);
        BookRecordStore.Save(_bookRecords);
        if (CurrentBookFilePath == record.FilePath)
        {
            CurrentBookFilePath = null;
        }
    }
}
