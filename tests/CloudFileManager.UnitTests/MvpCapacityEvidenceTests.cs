using CloudFileManager.Domain;

namespace CloudFileManager.UnitTests;

/// <summary>
/// MvpCapacityEvidenceTests 類別，負責封裝該領域的核心資料與行為。
/// </summary>
public sealed class MvpCapacityEvidenceTests
{
    [Fact]
    public void Should_MatchManualCapacityCalculation_ForRootAndIntermediateDirectories()
    {
        DateTime now = DateTime.UtcNow;

        CloudDirectory root = new("Root", now);
        CloudDirectory projectDocs = root.AddDirectory("Project_Docs", now);
        CloudDirectory personalNotes = root.AddDirectory("Personal_Notes", now);
        CloudDirectory archive2025 = personalNotes.AddDirectory("Archive_2025", now);

        projectDocs.AddFile(new WordFile("需求規格書.docx", 500 * 1024L, now, 15));
        projectDocs.AddFile(new ImageFile("系統架構圖.png", 2 * 1024 * 1024L, now, 1920, 1080));
        personalNotes.AddFile(new TextFile("待辦清單.txt", 1 * 1024L, now, "UTF-8"));
        archive2025.AddFile(new WordFile("舊會議記錄.docx", 200 * 1024L, now, 5));
        root.AddFile(new TextFile("README.txt", 500L, now, "ASCII"));

        long projectDocsExpected = (500 * 1024L) + (2 * 1024 * 1024L);
        long personalNotesExpected = (1 * 1024L) + (200 * 1024L);
        long rootExpected = projectDocsExpected + personalNotesExpected + 500L;

        long projectDocsActual = projectDocs.CalculateTotalBytes();
        long personalNotesActual = personalNotes.CalculateTotalBytes();
        long rootActual = root.CalculateTotalBytes();

        Assert.Equal(2_609_152L, projectDocsExpected);
        Assert.Equal(205_824L, personalNotesExpected);
        Assert.Equal(2_815_476L, rootExpected);

        Assert.Equal(projectDocsExpected, projectDocsActual);
        Assert.Equal(personalNotesExpected, personalNotesActual);
        Assert.Equal(rootExpected, rootActual);
    }
}
