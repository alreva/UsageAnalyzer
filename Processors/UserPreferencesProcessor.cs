using Dto;

namespace Processors;

public class UserPreferencesProcessor : BaseProcessor<UserEventDto>
{
    public override void Process(string jsonInput, TextWriter output)
    {
        var userEventDto = Deserialize(jsonInput);
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