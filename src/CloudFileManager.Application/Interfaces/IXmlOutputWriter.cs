namespace CloudFileManager.Application.Interfaces;

public interface IXmlOutputWriter
{
    string Write(string outputPath, string xmlContent);
}
