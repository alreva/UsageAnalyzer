// <copyright file="ActivityLogProcessorTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Processors.Tests;

using System.Text.Json;

public class ActivityLogProcessorTests
{
  [Fact]
  public void Process_ValidJson_WritesFormattedActivityLog()
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
    var processor = new ActivityLogProcessor();
    var output = new StringWriter();

    // Act
    processor.Process(json, output);
    var result = output.ToString();

    // Assert
    Assert.Contains("User Activity Log:", result);
    Assert.Contains("Action: viewProduct", result);
    Assert.Contains("Product ID: prod123", result);
    Assert.Contains("Action: login", result);
  }

  [Fact]
  public void Process_EmptyActivityLog_WritesNoDataMessage()
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
                    "activityLog": []
                }
            }
            """;
    var processor = new ActivityLogProcessor();
    var output = new StringWriter();

    // Act
    processor.Process(json, output);
    var result = output.ToString();

    // Assert
    Assert.Contains("No activity log found.", result);
  }
}
