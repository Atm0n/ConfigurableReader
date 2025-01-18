using System.IO;

namespace ConfigurableReader;

internal class TextChunkReader
{
    public IEnumerable<string> ReadChunks(string filePath, int chunkSize)
    {
        using var reader = new StreamReader(filePath);
        char[] buffer = new char[chunkSize];
        int readChars;
        while ((readChars = reader.Read(buffer, 0, chunkSize)) > 0)
        {
            yield return new string(buffer, 0, readChars);
        }
    }
}
