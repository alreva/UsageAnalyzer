namespace Processors;

using Dto;

public class UserProcessor : BaseProcessor<UserEventDto>
{
  /// <inheritdoc/>
  public override void Process(string jsonInput, TextWriter output)
  {
    var userEventDto = this.Deserialize(jsonInput);
    if (userEventDto?.User == null)
    {
      this.WriteNoDataMessage(output, "user information");
      return;
    }

    var user = userEventDto.User;
    output.WriteLine("User Information:");
    output.WriteLine($"User ID: {user.UserId}");
    output.WriteLine($"Username: {user.Username}");
    output.WriteLine($"Email: {user.Email}");
    output.WriteLine($"Name: {user.FirstName} {user.LastName}");
    output.WriteLine($"Date of Birth: {user.DateOfBirth:yyyy-MM-dd}");

#pragma warning disable S125
    // this is done intentionally to show how unused properties are displayed in the final report
    // if (user.CreatedAt.HasValue)
    // {
    //     output.WriteLine($"Created At: {user.CreatedAt.Value:yyyy-MM-dd HH:mm:ss}");
    // }
#pragma warning restore S125

    output.WriteLine($"Gender: {user.Gender}");
    output.WriteLine($"Phone Number: {user.PhoneNumber}");
    output.WriteLine($"Last Login: {user.LastLogin:yyyy-MM-dd HH:mm:ss}");
    output.WriteLine($"Account Status: {user.AccountStatus}");
    output.WriteLine($"Subscription Plan: {user.SubscriptionPlan}");
    output.WriteLine($"Payment Method: {user.PaymentMethod}");
    output.WriteLine($"Last Payment Date: {user.LastPaymentDate:yyyy-MM-dd}");
    output.WriteLine($"Total Orders: {user.TotalOrders}");
    output.WriteLine($"Cart Items: {user.CartItems}");
    output.WriteLine($"Loyalty Points: {user.LoyaltyPoints}");
    output.WriteLine($"Referral Code: {user.ReferralCode}");

    if (user.FavoriteCategories.Any())
    {
      output.WriteLine("\nFavorite Categories:");
      foreach (var category in user.FavoriteCategories)
      {
        output.WriteLine($"- {category}");
      }
    }

    if (user.Wishlist.Any())
    {
      output.WriteLine("\nWishlist Items:");
      foreach (var item in user.Wishlist)
      {
        output.WriteLine($"- {item}");
      }
    }

    if (user.RecentSearches.Any())
    {
      output.WriteLine("\nRecent Searches:");
      foreach (var search in user.RecentSearches)
      {
        output.WriteLine($"- {search}");
      }
    }
  }
}
