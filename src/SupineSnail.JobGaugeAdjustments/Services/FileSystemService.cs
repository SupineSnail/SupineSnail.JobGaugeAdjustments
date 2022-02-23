using System.IO;
using SupineSnail.JobGaugeAdjustments.Abstractions;

namespace SupineSnail.JobGaugeAdjustments.Services;

public class FileSystemService : IFileSystemService
{
    public bool Exists(string filePath)
        => File.Exists(filePath);

    public string ReadFileText(string filePath)
        => File.ReadAllText(filePath);
}