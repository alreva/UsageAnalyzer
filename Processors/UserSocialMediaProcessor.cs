namespace Processors;

using Dto;

public class UserSocialMediaProcessor : BaseProcessor<UserEventDto>
{
  /// <inheritdoc/>
  public override void Process(string jsonInput, TextWriter output)
  {
    var userEventDto = this.Deserialize(jsonInput);
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
      this.WriteNoDataMessage(output, "social media profiles");
    }
  }
}
