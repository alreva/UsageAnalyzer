// <copyright file="User.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Dto;

/// <summary>
/// Represents a user.
/// </summary>
public class User
{
  public required string UserId { get; set; }

  public required string Username { get; set; }

  public required string Email { get; set; }

  public required string FirstName { get; set; }

  public required string LastName { get; set; }

  public required DateTime DateOfBirth { get; set; }

  public DateTime? CreatedAt { get; set; }

  public required string Gender { get; set; }

  public required string PhoneNumber { get; set; }

  public required Address Address { get; set; }

  public required Preferences Preferences { get; set; }

  public required DateTime LastLogin { get; set; }

  public required string AccountStatus { get; set; }

  public required string SubscriptionPlan { get; set; }

  public required string PaymentMethod { get; set; }

  public required DateTime LastPaymentDate { get; set; }

  public required int TotalOrders { get; set; }

  public required List<string> FavoriteCategories { get; set; } = new();

  public required List<string> Wishlist { get; set; } = new();

  public required List<string> RecentSearches { get; set; } = new();

  public required int CartItems { get; set; }

  public required int LoyaltyPoints { get; set; }

  public required string ReferralCode { get; set; }

  public required SocialMedia SocialMedia { get; set; }

  public required DeviceInfo DeviceInfo { get; set; }

  public required List<ActivityLog> ActivityLog { get; set; } = new();
}
