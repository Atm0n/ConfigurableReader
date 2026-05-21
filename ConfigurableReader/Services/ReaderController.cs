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
    public bool IsUpdatingFromCode { get; set; }

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
            BookRecordStore.Save(_bookRecords);
        }
    }

    public async Task<string> OpenBookAsync(string filePath)
    {
        CurrentBookFilePath = filePath;
        var source = await _documentRegistry.CreateSourceAsync(filePath);
        var record = GetOrCreateRecord(filePath);
        
        IsUpdatingFromCode = true;
        try
        {
            await _readerService.SetSourceAsync(source, record.ScrollPosition);
        }
        finally
        {
            IsUpdatingFromCode = false;
        }

        return Path.GetFileName(filePath);
    }
}
