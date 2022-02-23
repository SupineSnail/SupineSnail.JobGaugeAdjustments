namespace SupineSnail.JobGaugeAdjustments.Abstractions;

/// <summary>
/// Wrappers around methods on <see cref="System.IO.File"/> for unit testability of implementers
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// Returns true if the file exists
    /// </summary>
    /// <param name="filePath">Path of the file</param>
    /// <returns>True if the file exists</returns>
    public bool Exists(string filePath);

    /// <summary>
    /// Loads a file and reads all of it's contents as a string
    /// </summary>
    /// <param name="filePath">Path of the file</param>
    /// <returns>Text of the file</returns>
    public string ReadFileText(string filePath);
}