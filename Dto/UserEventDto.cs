using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Dto
{
    public class UserEventDto
    {
        public required string EventId { get; set; }
        public required DateTime Timestamp { get; set; }
        public required string Source { get; set; }
        public required string Message { get; set; }
        public required User User { get; set; }
    }

    public class User
    {
        public required string UserId { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required DateTime DateOfBirth { get; set; }
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

    public class Address
    {
        public required string Street { get; set; }
        public required string City { get; set; }
        public required string State { get; set; }
        public required string ZipCode { get; set; }
        public required string Country { get; set; }
    }

    public class Preferences
    {
        public required string Theme { get; set; }
        public required string Language { get; set; }
        public required bool Notifications { get; set; }
        public required bool Newsletter { get; set; }
        public required string Timezone { get; set; }
    }

    public class SocialMedia
    {
        public required string Facebook { get; set; }
        public required string Twitter { get; set; }
        public required string Instagram { get; set; }
    }

    public class DeviceInfo
    {
        public required string DeviceType { get; set; }
        public required string Os { get; set; }
        public required string Browser { get; set; }
        public required string IpAddress { get; set; }
    }

    public class ActivityLog
    {
        public required string Action { get; set; }
        public required DateTime Timestamp { get; set; }
        public string? ProductId { get; set; }
    }
} 