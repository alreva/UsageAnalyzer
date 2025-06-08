using Dto;

namespace Processors;

public class UserProcessor : BaseProcessor<UserEventDto>
{
    public override void Process(string jsonInput, TextWriter output)
    {
        var userEventDto = Deserialize(jsonInput);
        if (userEventDto?.User != null)
        {
            var user = userEventDto.User;
            output.WriteLine("User Information:");
            output.WriteLine($"User ID: {user.UserId}");
            output.WriteLine($"Username: {user.Username}");
            output.WriteLine($"Email: {user.Email}");
            output.WriteLine($"Name: {user.FirstName} {user.LastName}");
            output.WriteLine($"Date of Birth: {user.DateOfBirth:yyyy-MM-dd}");
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
        else
        {
            WriteNoDataMessage(output, "user information");
        }
    }
} 