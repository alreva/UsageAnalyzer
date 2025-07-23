namespace Processors.Tests;

using System.Text.Json;

public class UserProcessorTests
{
  [Fact]
  public void Process_ValidJson_WritesFormattedUserInfo()
  {
    // Arrange
    var json =
        """
            {
                "eventId": "12345",
                "timestamp": "2023-10-01T12:00:00Z",
                "source": "User Activity System",
                "message": "User has been imported.",
                "user": {
                    "userId": "user123",
                    "username": "johndoe",
                    "email": "john.doe@example.com",
                    "firstName": "John",
                    "lastName": "Doe",
                    "dateOfBirth": "1980-01-01",
                    "gender": "Male",
                    "phoneNumber": "+1234567890",
                    "address": {
                        "street": "123 Main St",
                        "city": "Anytown",
                        "state": "CA",
                        "zipCode": "12345",
                        "country": "USA"
                    },
                    "preferences": {
                        "theme": "dark",
                        "language": "en",
                        "notifications": true,
                        "newsletter": false,
                        "timezone": "UTC-8"
                    },
                    "lastLogin": "2023-09-30T10:00:00Z",
                    "accountStatus": "active",
                    "subscriptionPlan": "premium",
                    "paymentMethod": "credit card",
                    "lastPaymentDate": "2023-09-15",
                    "totalOrders": 15,
                    "favoriteCategories": ["electronics", "books", "clothing"],
                    "wishlist": ["item1", "item2", "item3"],
                    "recentSearches": ["laptop", "headphones", "smartphone"],
                    "cartItems": 3,
                    "loyaltyPoints": 500,
                    "referralCode": "REF123",
                    "socialMedia": {
                        "facebook": "facebook.com/johndoe",
                        "twitter": "twitter.com/johndoe",
                        "instagram": "instagram.com/johndoe"
                    },
                    "deviceInfo": {
                        "deviceType": "mobile",
                        "os": "iOS",
                        "browser": "Safari",
                        "ipAddress": "192.168.1.1"
                    },
                    "activityLog": [
                        {
                            "action": "login",
                            "timestamp": "2023-09-30T10:00:00Z"
                        },
                        {
                            "action": "viewProduct",
                            "productId": "prod123",
                            "timestamp": "2023-09-30T10:05:00Z"
                        }
                    ]
                }
            }
            """;

    var processor = new UserProcessor();
    var output = new StringWriter();

    // Act
    processor.Process(json, output);

    // Assert
    var result = output.ToString();
    Assert.Contains("User Information:", result);
    Assert.Contains("User ID: user123", result);
    Assert.Contains("Username: johndoe", result);
    Assert.Contains("Email: john.doe@example.com", result);
    Assert.Contains("Name: John Doe", result);
    Assert.Contains("Date of Birth: 1980-01-01", result);
    Assert.Contains("Gender: Male", result);
    Assert.Contains("Phone Number: +1234567890", result);
    Assert.Contains("Last Login: 2023-09-30 10:00:00", result);
    Assert.Contains("Account Status: active", result);
    Assert.Contains("Subscription Plan: premium", result);
    Assert.Contains("Payment Method: credit card", result);
    Assert.Contains("Last Payment Date: 2023-09-15", result);
    Assert.Contains("Total Orders: 15", result);
    Assert.Contains("Cart Items: 3", result);
    Assert.Contains("Loyalty Points: 500", result);
    Assert.Contains("Referral Code: REF123", result);
    Assert.Contains("Favorite Categories:", result);
    Assert.Contains("- electronics", result);
    Assert.Contains("- books", result);
    Assert.Contains("- clothing", result);
    Assert.Contains("Wishlist Items:", result);
    Assert.Contains("- item1", result);
    Assert.Contains("- item2", result);
    Assert.Contains("- item3", result);
    Assert.Contains("Recent Searches:", result);
    Assert.Contains("- laptop", result);
    Assert.Contains("- headphones", result);
    Assert.Contains("- smartphone", result);
  }

  [Fact]
  public void Process_NullUser_WritesNoDataMessage()
  {
    // Arrange
    var json =
        """
            {
                "eventId": "12345",
                "timestamp": "2023-10-01T12:00:00Z",
                "source": "User Activity System",
                "message": "User has been imported.",
                "user": null
            }
            """;

    var processor = new UserProcessor();
    var output = new StringWriter();

    // Act
    processor.Process(json, output);

    // Assert
    var result = output.ToString();
    Assert.Contains("No user information found.", result);
  }
}
