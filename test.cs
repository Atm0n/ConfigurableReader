using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        var epubAsm = Assembly.LoadFrom(@"ConfigurableReader.Parsers.Epub\bin\Release\net10.0\VersOne.Epub.dll");
        var pdfAsm = Assembly.LoadFrom(@"ConfigurableReader.Parsers.Pdf\bin\Release\net10.0\UglyToad.PdfPig.dll");

        var linkType = epubAsm.GetType("VersOne.Epub.EpubNavigationItemLink");
        if (linkType != null) {
            foreach (var p in linkType.GetProperties()) Console.WriteLine("Link: " + p.Name);
        }

        var nodeType = pdfAsm.GetType("UglyToad.PdfPig.Outline.BookmarkNode");
        if (nodeType != null) {
            foreach (var p in nodeType.GetProperties()) Console.WriteLine("Node: " + p.Name);
        }
    }
}
