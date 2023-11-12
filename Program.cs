using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;

class Program
{
    static async Task Main()
    {
        Settings settings;

        if (File.Exists("settings.json"))
        {
            Console.Write("Existing settings found. Do you want to use them? (y/n): ");
            string useExisting = Console.ReadLine();

            if (useExisting.ToLower() == "y")
            {
                settings = LoadData<Settings>("settings.json");
            }
            else
            {
                settings = GetUserData();
                SaveData(settings);
            }
        }
        else
        {
            settings = GetUserData();
            SaveData(settings);
        }
        Console.WriteLine("Press Enter to stop");
        await MonitorMessagesAsync(settings);
    }

    static async Task MonitorMessagesAsync(Settings settings)
    {
        string past = await GetMessagesAsync(settings.Token, settings.ChannelID);

        while (true)
        {
            Console.WriteLine("Waiting For New Message");

            string current = await GetMessagesAsync(settings.Token, settings.ChannelID);

            if (past != current)
            {
                Console.WriteLine("New message");
                Console.Beep();
                CopyToClipboard(current);
                OpenLink();

                break;
            }

            await Task.Delay(1000);

            if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Enter)
            {
                break;
            }
        }
    }

    static Settings GetUserData()
    {
        Console.Write("Enter your Discord channel ID: ");
        string channelId = Console.ReadLine();

        Console.Write("Enter your Discord token: ");
        string token = Console.ReadLine();

        return new Settings
        {
            Token = token,
            ChannelID = channelId
        };
    }

    static async Task<string> GetMessagesAsync(string token, string channelId)
    {
        using (var client = new HttpClient())
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://discord.com/api/v9/channels/{channelId}/messages");
            request.Headers.Add("Authorization", $"{token}");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }

    static void SaveData<T>(T data)
    {
        try
        {
            string jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("settings.json", jsonData);
            Console.WriteLine("Data saved.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving data: {ex.Message}");
        }
    }

    static T LoadData<T>(string filePath)
    {
        try
        {
            string jsonData = File.ReadAllText(filePath);

            if (IsValidJson(jsonData))
            {
                T loadedData = JsonSerializer.Deserialize<T>(jsonData);
                Console.WriteLine("Data loaded.");
                return loadedData;
            }
            else
            {
                Console.WriteLine("Invalid JSON format in the data file.");
                return default(T);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading data: {ex.Message}");
            return default(T);
        }
    }

    static void DeleteData(string filePath)
    {
        try
        {
            File.Delete(filePath);
            Console.WriteLine("Data deleted.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting data: {ex.Message}");
        }
    }

    static bool IsValidJson(string input)
    {
        try
        {
            JsonDocument.Parse(input);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    static void OpenLink()
    {
        string url = "http://google.com";
        try
        {
            Process.Start(url);
        }
        catch
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }

    static void CopyToClipboard(string text)
    {
        try
        {
            var jsonDocument = JsonDocument.Parse(text);

            if (jsonDocument.RootElement.ValueKind == JsonValueKind.Array)
            {
                var firstElement = jsonDocument.RootElement.EnumerateArray().FirstOrDefault();
                if (firstElement.ValueKind != JsonValueKind.Null)
                {
                    var content = firstElement.GetProperty("content").GetString();

                    TextCopy.ClipboardService.SetText(content);

                    Console.WriteLine("Message content copied to clipboard.");
                }
                else
                {
                    Console.WriteLine("Error: No content found in the array.");
                }

            }
            else if (jsonDocument.RootElement.ValueKind == JsonValueKind.Object)
            {
                var content = jsonDocument.RootElement.GetProperty("content").GetString();

                TextCopy.ClipboardService.SetText(content);

                Console.WriteLine("Message content copied to clipboard.");
            }
            else
            {
                Console.WriteLine("Error: Unsupported JSON structure.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error copying message content to clipboard: {ex.Message}");
        }
    }

    class Settings
    {
        public string ChannelID { get; set; }
        public string Token { get; set; }
    }
}
