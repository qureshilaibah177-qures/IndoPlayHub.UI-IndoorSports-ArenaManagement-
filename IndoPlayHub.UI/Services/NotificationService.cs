
using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI.Services
{
    public class NotificationService
    {
        private readonly AppDbContext _dbContext;

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public NotificationService(AppDbContext dbContext)
        {
            _dbContext = dbContext
                ?? throw new ArgumentNullException(nameof(dbContext));
        }


        // =========================================================
        // CREATE NOTIFICATION
        // =========================================================

        public async Task<bool> CreateNotificationAsync(
            int customerID,
            string? notificationType,
            string? title,
            string? message)
        {
            try
            {
                // -------------------------------------------------
                // VALIDATE CUSTOMER
                // -------------------------------------------------

                if (customerID <= 0)
                    return false;


                // -------------------------------------------------
                // NORMALIZE VALUES
                // -------------------------------------------------

                string safeNotificationType =
                    string.IsNullOrWhiteSpace(notificationType)
                        ? "General"
                        : notificationType.Trim();

                string safeTitle =
                    string.IsNullOrWhiteSpace(title)
                        ? "Notification"
                        : title.Trim();

                string safeMessage =
                    message?.Trim() ?? string.Empty;


                // -------------------------------------------------
                // RESPECT DATABASE FIELD LIMITS
                // -------------------------------------------------

                if (safeNotificationType.Length > 50)
                    safeNotificationType =
                        safeNotificationType[..50];

                if (safeTitle.Length > 150)
                    safeTitle =
                        safeTitle[..150];

                if (safeMessage.Length > 500)
                    safeMessage =
                        safeMessage[..500];


                // -------------------------------------------------
                // CREATE NOTIFICATION
                // -------------------------------------------------

                Notification notification =
                    new Notification
                    {
                        CustomerID = customerID,
                        NotificationType = safeNotificationType,
                        Title = safeTitle,
                        Message = safeMessage,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };


                // -------------------------------------------------
                // SAVE
                // -------------------------------------------------

                await _dbContext.Notifications.AddAsync(
                    notification);

                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"NotificationService.CreateNotificationAsync failed: {ex}");

                return false;
            }
        }


        // =========================================================
        // BOOKING CONFIRMATION
        // =========================================================

        public async Task<bool>
            CreateBookingConfirmationNotificationAsync(
                Customer? customer,
                Booking? booking,
                Arena? arena,
                CourtOrMenuItem? court,
                Category? category)
        {
            try
            {
                if (customer == null ||
                    booking == null ||
                    arena == null ||
                    court == null)
                {
                    return false;
                }


                if (customer.CustomerID <= 0)
                    return false;


                string sport =
                    string.IsNullOrWhiteSpace(
                        category?.CategoryName)
                        ? "Sport"
                        : category.CategoryName.Trim();

                string arenaName =
                    string.IsNullOrWhiteSpace(
                        arena.ArenaName)
                        ? "Arena"
                        : arena.ArenaName.Trim();

                string courtName =
                    string.IsNullOrWhiteSpace(
                        court.ItemName)
                        ? "Court"
                        : court.ItemName.Trim();


                string bookingDate =
                    booking.BookingDate.ToString(
                        "dddd, dd MMMM yyyy");

                string startTime =
                    FormatTime(booking.StartTime);

                string endTime =
                    FormatTime(booking.EndTime);


                string message =
                    $"Your {sport} booking is confirmed.\n\n" +
                    $"Arena: {arenaName}\n" +
                    $"Court: {courtName}\n" +
                    $"Date: {bookingDate}\n" +
                    $"Time: {startTime} - {endTime}\n" +
                    $"Booking ID: #{booking.BookingID}\n" +
                    $"Total: Rs. {booking.TotalAmount:N0}";


                return await CreateNotificationAsync(
                    customer.CustomerID,
                    "Booking Confirmation",
                    "Booking Confirmed",
                    message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Booking confirmation notification failed: {ex}");

                return false;
            }
        }


        // =========================================================
        // BOOKING CANCELLATION
        // =========================================================

        public async Task<bool>
            CreateBookingCancellationNotificationAsync(
                Customer? customer,
                Booking? booking,
                Arena? arena,
                CourtOrMenuItem? court,
                Category? category)
        {
            try
            {
                if (customer == null ||
                    booking == null ||
                    arena == null ||
                    court == null)
                {
                    return false;
                }


                if (customer.CustomerID <= 0)
                    return false;


                string sport =
                    string.IsNullOrWhiteSpace(
                        category?.CategoryName)
                        ? "Sport"
                        : category.CategoryName.Trim();

                string arenaName =
                    string.IsNullOrWhiteSpace(
                        arena.ArenaName)
                        ? "Arena"
                        : arena.ArenaName.Trim();

                string courtName =
                    string.IsNullOrWhiteSpace(
                        court.ItemName)
                        ? "Court"
                        : court.ItemName.Trim();


                string bookingDate =
                    booking.BookingDate.ToString(
                        "dddd, dd MMMM yyyy");

                string startTime =
                    FormatTime(booking.StartTime);

                string endTime =
                    FormatTime(booking.EndTime);


                string refundMessage =
                    string.Equals(
                        booking.RefundStatus,
                        "Pending",
                        StringComparison.OrdinalIgnoreCase)
                        ? "Your refund is currently pending."
                        : "No refund is required.";


                string reason =
                    string.IsNullOrWhiteSpace(
                        booking.CancellationReason)
                        ? "No reason provided."
                        : booking.CancellationReason.Trim();


                string message =
                    $"Your {sport} booking has been cancelled.\n\n" +
                    $"Arena: {arenaName}\n" +
                    $"Court: {courtName}\n" +
                    $"Date: {bookingDate}\n" +
                    $"Time: {startTime} - {endTime}\n" +
                    $"Booking ID: #{booking.BookingID}\n\n" +
                    $"Cancellation reason: {reason}\n" +
                    $"{refundMessage}";


                return await CreateNotificationAsync(
                    customer.CustomerID,
                    "Cancellation",
                    "Booking Cancelled",
                    message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Booking cancellation notification failed: {ex}");

                return false;
            }
        }


        // =========================================================
        // BOOKING REMINDER
        // =========================================================

        public async Task<bool>
            CreateBookingReminderNotificationAsync(
                Customer? customer,
                Booking? booking,
                Arena? arena,
                CourtOrMenuItem? court,
                Category? category)
        {
            try
            {
                if (customer == null ||
                    booking == null ||
                    arena == null ||
                    court == null)
                {
                    return false;
                }


                if (customer.CustomerID <= 0)
                    return false;


                // -------------------------------------------------
                // NEVER REMIND CANCELLED BOOKINGS
                // -------------------------------------------------

                if (string.Equals(
                        booking.BookingStatus,
                        "Cancelled",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }


                // -------------------------------------------------
                // PREVENT DUPLICATE REMINDER
                // -------------------------------------------------

                bool alreadyCreated =
                    await ReminderAlreadyCreatedAsync(
                        customer.CustomerID,
                        booking.BookingID);

                if (alreadyCreated)
                    return false;


                string sport =
                    string.IsNullOrWhiteSpace(
                        category?.CategoryName)
                        ? "Sport"
                        : category.CategoryName.Trim();

                string arenaName =
                    string.IsNullOrWhiteSpace(
                        arena.ArenaName)
                        ? "Arena"
                        : arena.ArenaName.Trim();

                string courtName =
                    string.IsNullOrWhiteSpace(
                        court.ItemName)
                        ? "Court"
                        : court.ItemName.Trim();


                string bookingDate =
                    booking.BookingDate.ToString(
                        "dddd, dd MMMM yyyy");

                string startTime =
                    FormatTime(booking.StartTime);

                string endTime =
                    FormatTime(booking.EndTime);


                string message =
                    $"Your {sport} game starts in about 2 hours.\n\n" +
                    $"Arena: {arenaName}\n" +
                    $"Court: {courtName}\n" +
                    $"Date: {bookingDate}\n" +
                    $"Time: {startTime} - {endTime}\n" +
                    $"Booking ID: #{booking.BookingID}";


                return await CreateNotificationAsync(
                    customer.CustomerID,
                    "Reminder",
                    "Game Reminder",
                    message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Booking reminder notification failed: {ex}");

                return false;
            }
        }


        // =========================================================
        // CHECK WHETHER REMINDER ALREADY EXISTS
        // =========================================================

        public async Task<bool> ReminderAlreadyCreatedAsync(
            int customerID,
            int bookingID)
        {
            try
            {
                if (customerID <= 0 ||
                    bookingID <= 0)
                {
                    return false;
                }


                string bookingReference =
                    $"Booking ID: #{bookingID}";


                return await _dbContext.Notifications
                    .AsNoTracking()
                    .AnyAsync(
                        n =>
                            n.CustomerID == customerID &&
                            n.NotificationType == "Reminder" &&
                            n.Message.Contains(
                                bookingReference));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ReminderAlreadyCreatedAsync failed: {ex}");

                return false;
            }
        }


        // =========================================================
        // GET CUSTOMER NOTIFICATIONS
        // =========================================================

        public async Task<List<Notification>>
            GetCustomerNotificationsAsync(
                int customerID)
        {
            try
            {
                if (customerID <= 0)
                    return new List<Notification>();


                return await _dbContext.Notifications
                    .AsNoTracking()
                    .Where(
                        n =>
                            n.CustomerID == customerID)
                    .OrderByDescending(
                        n => n.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"GetCustomerNotificationsAsync failed: {ex}");

                return new List<Notification>();
            }
        }


        // =========================================================
        // GET UNREAD NOTIFICATIONS
        // =========================================================

        public async Task<List<Notification>>
            GetUnreadNotificationsAsync(
                int customerID)
        {
            try
            {
                if (customerID <= 0)
                    return new List<Notification>();


                return await _dbContext.Notifications
                    .AsNoTracking()
                    .Where(
                        n =>
                            n.CustomerID == customerID &&
                            !n.IsRead)
                    .OrderByDescending(
                        n => n.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"GetUnreadNotificationsAsync failed: {ex}");

                return new List<Notification>();
            }
        }


        // =========================================================
        // GET UNREAD COUNT
        // =========================================================

        public async Task<int> GetUnreadCountAsync(
            int customerID)
        {
            try
            {
                if (customerID <= 0)
                    return 0;


                return await _dbContext.Notifications
                    .AsNoTracking()
                    .CountAsync(
                        n =>
                            n.CustomerID == customerID &&
                            !n.IsRead);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"GetUnreadCountAsync failed: {ex}");

                return 0;
            }
        }


        // =========================================================
        // MARK ONE AS READ
        // =========================================================

        public async Task<bool> MarkAsReadAsync(
            int customerID,
            int notificationID)
        {
            try
            {
                if (customerID <= 0 ||
                    notificationID <= 0)
                {
                    return false;
                }


                Notification? notification =
                    await _dbContext.Notifications
                        .FirstOrDefaultAsync(
                            n =>
                                n.NotificationID ==
                                    notificationID &&
                                n.CustomerID ==
                                    customerID);


                if (notification == null)
                    return false;


                if (notification.IsRead)
                    return true;


                notification.IsRead = true;

                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"MarkAsReadAsync failed: {ex}");

                return false;
            }
        }


        // =========================================================
        // MARK ALL AS READ
        // =========================================================

        public async Task<int> MarkAllAsReadAsync(
            int customerID)
        {
            try
            {
                if (customerID <= 0)
                    return 0;


                List<Notification> notifications =
                    await _dbContext.Notifications
                        .Where(
                            n =>
                                n.CustomerID == customerID &&
                                !n.IsRead)
                        .ToListAsync();


                if (notifications.Count == 0)
                    return 0;


                foreach (Notification notification
                    in notifications)
                {
                    notification.IsRead = true;
                }


                await _dbContext.SaveChangesAsync();

                return notifications.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"MarkAllAsReadAsync failed: {ex}");

                return 0;
            }
        }


        // =========================================================
        // FORMAT TIME
        // =========================================================

        private static string FormatTime(
            TimeSpan time)
        {
            try
            {
                return DateTime.Today
                    .Add(time)
                    .ToString("h:mm tt");
            }
            catch
            {
                return time.ToString();
            }
        }
    }
}

