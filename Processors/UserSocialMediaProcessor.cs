using Dto;

namespace Processors;

public class UserSocialMediaProcessor : BaseProcessor<UserEventDto>
{
    public override void Process(string jsonInput, TextWriter output)
    {
        var userEventDto = Deserialize(jsonInput);
        if (userEventDto?.User?.SocialMedia != null)
        {
            var socialMedia = userEventDto.User.SocialMedia;
            output.WriteLine("Social Media Profiles:");
            output.WriteLine($"Facebook: {socialMedia.Facebook}");
            output.WriteLine($"Twitter: {socialMedia.Twitter}");
            output.WriteLine($"Instagram: {socialMedia.Instagram}");
        }
        else
        {
            WriteNoDataMessage(output, "social media profiles");
        }
    }
} 