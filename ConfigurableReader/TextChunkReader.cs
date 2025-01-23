using System.IO;

namespace ConfigurableReader;

internal class TextChunkReader
{
    public string[] ReadChunks(string filePath, int chunkSize)
    {
        List<string> chunks = [];

        using var reader = new StreamReader(filePath);
        char[] buffer = new char[chunkSize];
        int readChars;

        while ((readChars = reader.Read(buffer, 0, chunkSize)) > 0)
        {
            chunks.Add(new string(buffer, 0, readChars).Replace("\n", " ").Replace("\r", " ").Replace("  ", ""));
        }

        return chunks.ToArray();
    }
}
