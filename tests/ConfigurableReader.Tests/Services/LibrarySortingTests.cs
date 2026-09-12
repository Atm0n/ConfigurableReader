using ConfigurableReader.Models;
using Shouldly;

namespace ConfigurableReader.Tests.Services;

public class LibrarySortingTests
{
    private readonly List<BookRecord> _testBooks =
    [
        new()
        {
            FilePath = "C:\\books\\Zeta.epub",
            Title = "Zeta",
            LastReadDate = new DateTime(2025, 1, 10),
            ScrollPosition = 500,
            TotalLength = 1000 // 50%
        },
        new()
        {
            FilePath = "C:\\books\\Alpha.txt",
            Title = "Alpha",
            LastReadDate = new DateTime(2025, 2, 20),
            ScrollPosition = 900,
            TotalLength = 1000 // 90%
        },
        new()
        {
            FilePath = "C:\\books\\Beta.pdf",
            Title = "Beta",
            LastReadDate = new DateTime(2025, 3, 1),
            ScrollPosition = 100,
            TotalLength = 1000 // 10%
        }
    ];

    private static IEnumerable<BookRecord> SortBooks(IEnumerable<BookRecord> books, string sortKey)
    {
        return sortKey switch
        {
            "TitleAsc" => books.OrderBy(b => b.DisplayTitle, StringComparer.CurrentCultureIgnoreCase),
            "TitleDesc" => books.OrderByDescending(b => b.DisplayTitle, StringComparer.CurrentCultureIgnoreCase),
            "ProgressDesc" => books.OrderByDescending(b => b.ProgressPercentage).ThenByDescending(b => b.LastReadDate),
            "ProgressAsc" => books.OrderBy(b => b.ProgressPercentage).ThenByDescending(b => b.LastReadDate),
            _ => books.OrderByDescending(b => b.LastReadDate)
        };
    }

    [Fact]
    public void Sort_Recent_OrdersByLastReadDateDescending()
    {
        var sorted = SortBooks(_testBooks, "Recent").ToList();

        sorted[0].Title.ShouldBe("Beta");  // March 1
        sorted[1].Title.ShouldBe("Alpha"); // Feb 20
        sorted[2].Title.ShouldBe("Zeta");  // Jan 10
    }

    [Fact]
    public void Sort_TitleAsc_OrdersAlphabetically()
    {
        var sorted = SortBooks(_testBooks, "TitleAsc").ToList();

        sorted[0].Title.ShouldBe("Alpha");
        sorted[1].Title.ShouldBe("Beta");
        sorted[2].Title.ShouldBe("Zeta");
    }

    [Fact]
    public void Sort_TitleDesc_OrdersReverseAlphabetically()
    {
        var sorted = SortBooks(_testBooks, "TitleDesc").ToList();

        sorted[0].Title.ShouldBe("Zeta");
        sorted[1].Title.ShouldBe("Beta");
        sorted[2].Title.ShouldBe("Alpha");
    }

    [Fact]
    public void Sort_ProgressDesc_OrdersByProgressPercentageDescending()
    {
        var sorted = SortBooks(_testBooks, "ProgressDesc").ToList();

        sorted[0].Title.ShouldBe("Alpha"); // 90%
        sorted[1].Title.ShouldBe("Zeta");  // 50%
        sorted[2].Title.ShouldBe("Beta");  // 10%
    }

    [Fact]
    public void Sort_ProgressAsc_OrdersByProgressPercentageAscending()
    {
        var sorted = SortBooks(_testBooks, "ProgressAsc").ToList();

        sorted[0].Title.ShouldBe("Beta");  // 10%
        sorted[1].Title.ShouldBe("Zeta");  // 50%
        sorted[2].Title.ShouldBe("Alpha"); // 90%
    }
}
