// <copyright file="UserSocialMediaProcessorTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Processors.Tests;

using System.Text.Json;

public class UserSocialMediaProcessorTests
{
  [Fact]
  public void Process_ValidJson_WritesFormattedSocialMedia()
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
                        }
                    ]
                }
            }
            """;

    var processor = new UserSocialMediaProcessor();
    var output = new StringWriter();

    // Act
    processor.Process(json, output);

    // Assert
    var result = output.ToString();
    Assert.Contains("Social Media Profiles:", result);
    Assert.Contains("Facebook: facebook.com/johndoe", result);
    Assert.Contains("Twitter: twitter.com/johndoe", result);
    Assert.Contains("Instagram: instagram.com/johndoe", result);
  }

  [Fact]
  public void Process_NullSocialMedia_WritesNoDataMessage()
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
                    "socialMedia": null,
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
                        }
                    ]
                }
            }
            """;

    var processor = new UserSocialMediaProcessor();
    var output = new StringWriter();

    // Act
    processor.Process(json, output);

    // Assert
    var result = output.ToString();
    Assert.Contains("No social media profiles found.", result);
  }
}