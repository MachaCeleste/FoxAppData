using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using FoxCrypto;

namespace FoxAppData;

/// <summary>
/// Handles generic object serialization, deserialization, disk persistence, 
/// and optional encryption within the user's Application Data directory.
/// </summary>
/// <param name="shouldEncrypt">Indicates whether files should be encrypted before writing and decrypted after reading.</param>
/// <param name="jsonOptions">Optional custom <see cref="JsonSerializerOptions"/>. Defaults to indented output when unencrypted and case-insensitive property matching.</param>
public class DataManager(bool shouldEncrypt = false, JsonSerializerOptions? jsonOptions = null)
{
    private readonly bool _encryption = shouldEncrypt;
    private readonly JsonSerializerOptions _jsonOptions = jsonOptions ?? new JsonSerializerOptions
    {
        WriteIndented = !shouldEncrypt,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Reads, decrypts (if encryption is enabled), and deserializes a JSON file into an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target object type to deserialize into.</typeparam>
    /// <param name="fileName">The file name or relative path inside the Application Data folder.</param>
    /// <param name="password">The decryption password. Required if encryption is enabled on this manager.</param>
    /// <returns>The deserialized instance of <typeparamref name="T"/>, or <see langword="default"/> if the file does not exist or decryption fails.</returns>
    /// <exception cref="InvalidOperationException">Thrown when encryption is enabled but no password is provided.</exception>
    public T? LoadData<T>(string fileName, string? password = null)
    {
        ValidatePasswordRequirement(password);

        string filePath = GetAppDataFilePath(fileName);

        if (!File.Exists(filePath))
            return default;

        string data = File.ReadAllText(filePath);
        string? json = data;

        if (_encryption)
        {
            string passkey = Crypto.GetPasskey(password!);
            json = Crypto.Run(data, passkey, "dec");

            if (string.IsNullOrEmpty(json))
                return default;
        }

        return json != null ? JsonSerializer.Deserialize<T>(json, _jsonOptions) : default;
    }

    /// <summary>
    /// Serializes an object to JSON, encrypts it (if enabled), and saves it to disk atomically via a temporary file.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="fileName">The file name or relative path inside the Application Data folder.</param>
    /// <param name="data">The data object instance to serialize and save.</param>
    /// <param name="password">The encryption password. Required if encryption is enabled on this manager.</param>
    /// <returns><see langword="true"/> if the save operation succeeded; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fileName"/> is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when encryption is enabled but no password is provided.</exception>
    public bool SaveData<T>(string fileName, T data, string? password = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

        ValidatePasswordRequirement(password);

        string? output = JsonSerializer.Serialize(data, _jsonOptions) ?? "{}";

        if (_encryption)
        {
            string passkey = Crypto.GetPasskey(password!);
            output = Crypto.Run(output, passkey);

            if (output == null)
                return false;
        }

        string filePath = GetAppDataFilePath(fileName);

        string tempFilePath = $"{filePath}.tmp";
        File.WriteAllText(tempFilePath, output);
        File.Move(tempFilePath, filePath, overwrite: true);

        return true;
    }

    private void ValidatePasswordRequirement(string? password)
    {
        if (_encryption && string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Encryption is enabled: a password is required.");
    }

    private static string GetAppDataFilePath(string fileName)
    {
        string dataPath = GetAppDataPath();

        if (!Directory.Exists(dataPath))
            Directory.CreateDirectory(dataPath);

        return Path.Combine(dataPath, fileName);
    }

    private static string GetAppDataPath()
    {
        Assembly? entryAssembly = Assembly.GetEntryAssembly();

        string companyName = "Celestial Corp";
        string productName = "FoxAppData";

        if (entryAssembly != null && !string.IsNullOrEmpty(entryAssembly.Location))
        {
            FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(entryAssembly.Location);

            if (!string.IsNullOrWhiteSpace(fileInfo.CompanyName))
                companyName = fileInfo.CompanyName;

            if (!string.IsNullOrWhiteSpace(fileInfo.ProductName))
                productName = fileInfo.ProductName;
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), companyName, productName);
    }
}
