// <copyright file="UserEventDtoTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Dto.Tests;

using System.Text.Json;

public class UserEventDtoTests
{
  [Fact]
  public void SerializeDeserialize_UserEventDto_ShouldMatch()
  {
    // Arrange
    var userEventDto = new UserEventDto
    {
      EventId = "12345",
      Timestamp = DateTime.Parse("2023-10-01T12:00:00Z"),
      Source = "User Activity System",
      Message = "User has been imported.",
      User = new User
      {
        UserId = "user123",
        Username = "johndoe",
        Email = "john.doe@example.com",
        FirstName = "John",
        LastName = "Doe",
        DateOfBirth = DateTime.Parse("1980-01-01"),
        Gender = "Male",
        PhoneNumber = "+1234567890",
        Address = new Address
        {
          Street = "123 Main St",
          City = "Anytown",
          State = "CA",
          ZipCode = "12345",
          Country = "USA",
        },
        Preferences = new Preferences
        {
          Theme = "dark",
          Language = "en",
          Notifications = true,
          Newsletter = false,
          Timezone = "UTC-8",
        },
        LastLogin = DateTime.Parse("2023-09-30T10:00:00Z"),
        AccountStatus = "active",
        SubscriptionPlan = "premium",
        PaymentMethod = "credit card",
        LastPaymentDate = DateTime.Parse("2023-09-15"),
        TotalOrders = 15,
        FavoriteCategories = new List<string> { "electronics", "books", "clothing" },
        Wishlist = new List<string> { "item1", "item2", "item3" },
        RecentSearches = new List<string> { "laptop", "headphones", "smartphone" },
        CartItems = 3,
        LoyaltyPoints = 500,
        ReferralCode = "REF123",
        SocialMedia = new SocialMedia
        {
          Facebook = "facebook.com/johndoe",
          Twitter = "twitter.com/johndoe",
          Instagram = "instagram.com/johndoe",
        },
        DeviceInfo = new DeviceInfo
        {
          DeviceType = "mobile",
          Os = "iOS",
          Browser = "Safari",
          IpAddress = "192.168.1.1",
        },
        ActivityLog = new List<ActivityLog>
                {
                    new()
                    {
                        Action = "login",
                        Timestamp = DateTime.Parse("2023-09-30T10:00:00Z"),
                    },
                    new()
                    {
                        Action = "viewProduct",
                        ProductId = "prod123",
                        Timestamp = DateTime.Parse("2023-09-30T10:05:00Z")
                    },
                },
      },
    };

    // Act
    var jsonString = JsonSerializer.Serialize(userEventDto);
    var deserializedUserEventDto = JsonSerializer.Deserialize<UserEventDto>(jsonString);

    // Assert
    Assert.NotNull(deserializedUserEventDto);
    Assert.NotNull(deserializedUserEventDto.User);
    Assert.NotNull(deserializedUserEventDto.User.Address);
    Assert.NotNull(deserializedUserEventDto.User.Preferences);
    Assert.NotNull(deserializedUserEventDto.User.SocialMedia);
    Assert.NotNull(deserializedUserEventDto.User.DeviceInfo);
    Assert.NotNull(deserializedUserEventDto.User.ActivityLog);
    Assert.Equal(2, deserializedUserEventDto.User.ActivityLog.Count);

    Assert.Equal(userEventDto.EventId, deserializedUserEventDto.EventId);
    Assert.Equal(userEventDto.Timestamp, deserializedUserEventDto.Timestamp);
    Assert.Equal(userEventDto.Source, deserializedUserEventDto.Source);
    Assert.Equal(userEventDto.Message, deserializedUserEventDto.Message);
    Assert.Equal(userEventDto.User.UserId, deserializedUserEventDto.User.UserId);
    Assert.Equal(userEventDto.User.Username, deserializedUserEventDto.User.Username);
    Assert.Equal(userEventDto.User.Email, deserializedUserEventDto.User.Email);
    Assert.Equal(userEventDto.User.FirstName, deserializedUserEventDto.User.FirstName);
    Assert.Equal(userEventDto.User.LastName, deserializedUserEventDto.User.LastName);
    Assert.Equal(userEventDto.User.DateOfBirth, deserializedUserEventDto.User.DateOfBirth);
    Assert.Equal(userEventDto.User.Gender, deserializedUserEventDto.User.Gender);
    Assert.Equal(userEventDto.User.PhoneNumber, deserializedUserEventDto.User.PhoneNumber);
    Assert.Equal(userEventDto.User.Address.Street, deserializedUserEventDto.User.Address.Street);
    Assert.Equal(userEventDto.User.Address.City, deserializedUserEventDto.User.Address.City);
    Assert.Equal(userEventDto.User.Address.State, deserializedUserEventDto.User.Address.State);
    Assert.Equal(userEventDto.User.Address.ZipCode, deserializedUserEventDto.User.Address.ZipCode);
    Assert.Equal(userEventDto.User.Address.Country, deserializedUserEventDto.User.Address.Country);
    Assert.Equal(userEventDto.User.Preferences.Theme, deserializedUserEventDto.User.Preferences.Theme);
    Assert.Equal(userEventDto.User.Preferences.Language, deserializedUserEventDto.User.Preferences.Language);
    Assert.Equal(
      userEventDto.User.Preferences.Notifications,
      deserializedUserEventDto.User.Preferences.Notifications);
    Assert.Equal(userEventDto.User.Preferences.Newsletter, deserializedUserEventDto.User.Preferences.Newsletter);
    Assert.Equal(userEventDto.User.Preferences.Timezone, deserializedUserEventDto.User.Preferences.Timezone);
    Assert.Equal(userEventDto.User.LastLogin, deserializedUserEventDto.User.LastLogin);
    Assert.Equal(userEventDto.User.AccountStatus, deserializedUserEventDto.User.AccountStatus);
    Assert.Equal(userEventDto.User.SubscriptionPlan, deserializedUserEventDto.User.SubscriptionPlan);
    Assert.Equal(userEventDto.User.PaymentMethod, deserializedUserEventDto.User.PaymentMethod);
    Assert.Equal(userEventDto.User.LastPaymentDate, deserializedUserEventDto.User.LastPaymentDate);
    Assert.Equal(userEventDto.User.TotalOrders, deserializedUserEventDto.User.TotalOrders);
    Assert.Equal(userEventDto.User.FavoriteCategories, deserializedUserEventDto.User.FavoriteCategories);
    Assert.Equal(userEventDto.User.Wishlist, deserializedUserEventDto.User.Wishlist);
    Assert.Equal(userEventDto.User.RecentSearches, deserializedUserEventDto.User.RecentSearches);
    Assert.Equal(userEventDto.User.CartItems, deserializedUserEventDto.User.CartItems);
    Assert.Equal(userEventDto.User.LoyaltyPoints, deserializedUserEventDto.User.LoyaltyPoints);
    Assert.Equal(userEventDto.User.ReferralCode, deserializedUserEventDto.User.ReferralCode);
    Assert.Equal(userEventDto.User.SocialMedia.Facebook, deserializedUserEventDto.User.SocialMedia.Facebook);
    Assert.Equal(userEventDto.User.SocialMedia.Twitter, deserializedUserEventDto.User.SocialMedia.Twitter);
    Assert.Equal(userEventDto.User.SocialMedia.Instagram, deserializedUserEventDto.User.SocialMedia.Instagram);
    Assert.Equal(userEventDto.User.DeviceInfo.DeviceType, deserializedUserEventDto.User.DeviceInfo.DeviceType);
    Assert.Equal(userEventDto.User.DeviceInfo.Os, deserializedUserEventDto.User.DeviceInfo.Os);
    Assert.Equal(userEventDto.User.DeviceInfo.Browser, deserializedUserEventDto.User.DeviceInfo.Browser);
    Assert.Equal(userEventDto.User.DeviceInfo.IpAddress, deserializedUserEventDto.User.DeviceInfo.IpAddress);
    Assert.Equal(userEventDto.User.ActivityLog[0].Action, deserializedUserEventDto.User.ActivityLog[0].Action);
    Assert.Equal(
      userEventDto.User.ActivityLog[0].Timestamp,
      deserializedUserEventDto.User.ActivityLog[0].Timestamp);
    Assert.Equal(userEventDto.User.ActivityLog[1].Action, deserializedUserEventDto.User.ActivityLog[1].Action);
    Assert.Equal(
      userEventDto.User.ActivityLog[1].ProductId,
      deserializedUserEventDto.User.ActivityLog[1].ProductId);
    Assert.Equal(
      userEventDto.User.ActivityLog[1].Timestamp,
      deserializedUserEventDto.User.ActivityLog[1].Timestamp);
  }
}