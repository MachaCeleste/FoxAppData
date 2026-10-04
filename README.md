Depends on:

- [FoxCrypto](https://github.com/MachaCeleste/FoxCrypto)

## Quick Start

```csharp
using FoxAppData;

// 1. Define data model
public class UserSettings
{
    public string Theme { get; set; } = "Dark";
    public int Volume { get; set; } = 80;
    public bool NotificationsEnabled { get; set; } = true;
}

// 2. Initialize DataManager (with optional encryption)
var manager = new DataManager(shouldEncrypt: true);
string secretKey = "MySecurePassword123";

// 3. Save object
var settings = new UserSettings { Theme = "Cynical", Volume = 100 };
bool success = manager.SaveData("settings.dat", settings, secretKey); // Use extension .json for unencrypted

if (success)
{
    Console.WriteLine("Data saved successfully!");
}

// 4. Load object
UserSettings? loadedSettings = manager.LoadData<UserSettings>("settings.dat", secretKey);

if (loadedSettings != null)
{
    Console.WriteLine($"Data Loaded. Theme: {loadedSettings.Theme}, Volume: {loadedSettings.Volume}");
}
```
