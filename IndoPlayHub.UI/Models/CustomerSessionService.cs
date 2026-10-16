using IndoPlayHub.UI.Data;
using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI.Services
{
    public class CustomerSessionService
    {
        private const string CustomerIdKey = "CustomerID";
        private const string CustomerRememberKey = "CustomerRememberMe";

        private readonly AppDbContext _dbContext;

        public CustomerSessionService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // =========================================================
        // SAVE CUSTOMER SESSION
        // =========================================================

        public async Task SaveSessionAsync(
            int customerId,
            bool rememberMe)
        {
            if (!rememberMe)
            {
                await ClearSessionAsync();
                return;
            }

            await SecureStorage.SetAsync(
                CustomerIdKey,
                customerId.ToString());

            Preferences.Set(
                CustomerRememberKey,
                true);
        }

        // =========================================================
        // RESTORE CUSTOMER SESSION
        // =========================================================

        public async Task<Customer?> RestoreSessionAsync()
        {
            bool rememberMe =
                Preferences.Get(
                    CustomerRememberKey,
                    false);

            if (!rememberMe)
                return null;

            string? customerIdText =
                await SecureStorage.GetAsync(
                    CustomerIdKey);

            if (!int.TryParse(
                    customerIdText,
                    out int customerId))
            {
                await ClearSessionAsync();
                return null;
            }

            Customer? customer =
                await _dbContext.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        c => c.CustomerID == customerId);

            if (customer == null ||
                !customer.IsActive)
            {
                await ClearSessionAsync();
                return null;
            }

            return customer;
        }

        // =========================================================
        // CLEAR SESSION
        // =========================================================

        public async Task ClearSessionAsync()
        {
            SecureStorage.Remove(
                CustomerIdKey);

            Preferences.Remove(
                CustomerRememberKey);

            await Task.CompletedTask;
        }

        // =========================================================
        // GET SAVED CUSTOMER ID
        // =========================================================

        public async Task<int?> GetCustomerIdAsync()
        {
            string? customerIdText =
                await SecureStorage.GetAsync(
                    CustomerIdKey);

            if (int.TryParse(
                    customerIdText,
                    out int customerId))
            {
                return customerId;
            }

            return null;
        }
    }
}