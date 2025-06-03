using System;
using System.Text.Json;
using System.IO;
using Dto;

namespace Processors
{
    public class UserPreferencesProcessor
    {
        public void ProcessUserPreferences(string jsonInput, TextWriter output)
        {
            var userEventDto = JsonSerializer.Deserialize<UserEventDto>(
                jsonInput,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            if (userEventDto?.User?.Preferences != null)
            {
                var prefs = userEventDto.User.Preferences;
                output.WriteLine($"User Preferences:");
                output.WriteLine($"Theme: {prefs.Theme}");
                output.WriteLine($"Language: {prefs.Language}");
                output.WriteLine($"Notifications: {prefs.Notifications}");
                output.WriteLine($"Newsletter: {prefs.Newsletter}");
                output.WriteLine($"Timezone: {prefs.Timezone}");
            }
            else
            {
                output.WriteLine("No user preferences found.");
            }
        }
    }
} 