using System;
using System.IO;
using Dto;

namespace Processors
{
    public class UserPreferencesProcessor : BaseProcessor
    {
        public void ProcessUserPreferences(string jsonInput, TextWriter output)
        {
            var userEventDto = DeserializeUserEvent(jsonInput);
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
                WriteNoDataMessage(output, "user preferences");
            }
        }
    }
} 