using Microsoft.EntityFrameworkCore;
using RevenueRecognitionAPI.Data;
using RevenueRecognitionAPI.Models;
using System.Text.Json;

namespace RevenueRecognitionAPI.Services;

public class RevenueService : IRevenueService
{
    private readonly DatabaseContext _context;
    private readonly HttpClient _httpClient;

    public RevenueService(DatabaseContext context, HttpClient httpClient)
    {
        _context = context;
        _httpClient = httpClient;
    }
    
    // CLIENT MANAGEMENT
    
    public async Task<bool> DoesIndividualClientExist(string pesel)
    {
        return await _context.IndividualClients.AnyAsync(c => c.PESEL == pesel && !c.IsDeleted);
    }

    public async Task<bool> DoesCompanyClientExist(string krs)
    {
        return await _context.CompanyClients.AnyAsync(c => c.KRS == krs && !c.IsDeleted);
    }

    public async Task<bool> DoesClientExist(int clientId)
    {
        return await _context.Clients.AnyAsync(c => c.Id == clientId && !c.IsDeleted);
    }

    public async Task<IndividualClient> AddIndividualClient(IndividualClient client)
    {
        _context.IndividualClients.Add(client);
        await _context.SaveChangesAsync();
        return client;
    }

    public async Task<CompanyClient> AddCompanyClient(CompanyClient client)
    {
        _context.CompanyClients.Add(client);
        await _context.SaveChangesAsync();
        return client;
    }

    public async Task<Client?> GetClientById(int clientId)
    {
        return await _context.Clients
            .Include(c => c.Contracts)
            .FirstOrDefaultAsync(c => c.Id == clientId && !c.IsDeleted);
    }

    public async Task<IndividualClient?> UpdateIndividualClient(int clientId, IndividualClient updatedClient)
    {
        var existingClient = await _context.IndividualClients
            .FirstOrDefaultAsync(c => c.Id == clientId && !c.IsDeleted);

        if (existingClient == null) return null;
        
        existingClient.FirstName = updatedClient.FirstName;
        existingClient.LastName = updatedClient.LastName;
        existingClient.Address = updatedClient.Address;
        existingClient.Email = updatedClient.Email;
        existingClient.PhoneNumber = updatedClient.PhoneNumber;

        await _context.SaveChangesAsync();
        return existingClient;
    }

    public async Task<CompanyClient?> UpdateCompanyClient(int clientId, CompanyClient updatedClient)
    {
        var existingClient = await _context.CompanyClients
            .FirstOrDefaultAsync(c => c.Id == clientId && !c.IsDeleted);

        if (existingClient == null) return null;
        
        existingClient.CompanyName = updatedClient.CompanyName;
        existingClient.Address = updatedClient.Address;
        existingClient.Email = updatedClient.Email;
        existingClient.PhoneNumber = updatedClient.PhoneNumber;

        await _context.SaveChangesAsync();
        return existingClient;
    }

    public async Task<bool> SoftDeleteIndividualClient(int clientId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var client = await _context.IndividualClients
                .FirstOrDefaultAsync(c => c.Id == clientId && !c.IsDeleted);

            if (client == null) 
            {
                await transaction.RollbackAsync();
                return false;
            }
            
            client.IsDeleted = true;
            client.FirstName = "DELETED";
            client.LastName = "DELETED";
            client.Email = "deleted@deleted.com";
            client.PhoneNumber = "000000000";
            client.Address = "DELETED";
            client.PESEL = "00000000000";

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<IEnumerable<Client>> GetAllClients()
    {
        return await _context.Clients.Where(c => !c.IsDeleted).ToListAsync();
    }
    
    // SOFTWARE MANAGEMENT

    public async Task<Software?> GetSoftwareById(int softwareId)
    {
        return await _context.Software.FirstOrDefaultAsync(s => s.Id == softwareId);
    }

    public async Task<IEnumerable<Software>> GetAllSoftware()
    {
        return await _context.Software.ToListAsync();
    }

    public async Task<bool> DoesSoftwareExist(int softwareId)
    {
        return await _context.Software.AnyAsync(s => s.Id == softwareId);
    }
    
    // CONTRACT MANAGEMENT
    
    public async Task<Contract> CreateContract(Contract contract)
    {
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();
        return contract;
    }

    public async Task<Contract?> GetContractById(int contractId)
    {
        return await _context.Contracts
            .Include(c => c.Client)
            .Include(c => c.Software)
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.Id == contractId);
    }

    public async Task<bool> HasActiveContractForSoftware(int clientId, int softwareId)
    {
        return await _context.Contracts.AnyAsync(c => 
            c.ClientId == clientId && 
            c.SoftwareId == softwareId && 
            c.IsSigned && 
            !c.IsCancelled);
    }

    public async Task<IEnumerable<Contract>> GetClientContracts(int clientId)
    {
        return await _context.Contracts
            .Include(c => c.Software)
            .Include(c => c.Payments)
            .Where(c => c.ClientId == clientId)
            .ToListAsync();
    }

    // PAYMENT PROCESSING

    public async Task<bool> ProcessContractPayment(int contractId, decimal amount)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var contract = await GetContractById(contractId);
            if (contract == null || !contract.IsPaymentWindowOpen || contract.IsFullyPaid)
            {
                await transaction.RollbackAsync();
                return false;
            }

            var payment = new Payment
            {
                ContractId = contractId,
                Amount = amount,
                PaymentDate = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
        
            if (contract.TotalPaid + amount >= contract.Price)
            {
                contract.IsSigned = true;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }
    
    public async Task<int> CancelExpiredContracts()
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var expiredContracts = await _context.Contracts
                .Where(c => !c.IsSigned && !c.IsCancelled && DateTime.UtcNow > c.EndDate)
                .ToListAsync();

            foreach (var contract in expiredContracts)
            {
                contract.IsCancelled = true;
            
                var payments = await _context.Payments
                    .Where(p => p.ContractId == contract.Id && !p.IsRefunded)
                    .ToListAsync();

                foreach (var payment in payments)
                {
                    payment.IsRefunded = true;
                    payment.RefundDate = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        
            return expiredContracts.Count;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    // DISCOUNT SYSTEM
    
    public async Task<IEnumerable<Discount>> GetActiveDiscounts()
    {
        var now = DateTime.UtcNow;
        return await _context.Discounts
            .Where(d => d.StartDate <= now && d.EndDate >= now)
            .ToListAsync();
    }

    public async Task<decimal> CalculateDiscountedPrice(decimal originalPrice, int? softwareId, bool isReturningClient)
    {
        var discounts = await GetActiveDiscounts();
        
        var applicableDiscounts = discounts
            .Where(d => d.SoftwareId == null || d.SoftwareId == softwareId)
            .OrderByDescending(d => d.Percentage);

        var highestDiscount = applicableDiscounts.FirstOrDefault();
        var discountPercentage = highestDiscount?.Percentage ?? 0;
        
        if (isReturningClient)
        {
            discountPercentage += 5.0M;
        }
        
        discountPercentage = Math.Min(discountPercentage, 100.0M);

        return originalPrice * (1 - discountPercentage / 100);
    }
    
    // REVENUE CALCULATION
    
    public async Task<decimal> CalculateCurrentRevenue(int? softwareId = null, string? currency = null)
    {
        var contractRevenue = await _context.Payments
            .Include(p => p.Contract)
            .Where(p => !p.IsRefunded && 
                        p.Contract.IsSigned &&
                        (softwareId == null || p.Contract.SoftwareId == softwareId))
            .SumAsync(p => p.Amount);
        
        if (!string.IsNullOrEmpty(currency) && currency.ToUpper() != "PLN")
        {
            contractRevenue = await ConvertCurrency(contractRevenue, currency);
        }

        return contractRevenue;
    }

    public async Task<decimal> CalculatePredictedRevenue(int? softwareId = null, string? currency = null)
    {
        var currentRevenue = await CalculateCurrentRevenue(softwareId, currency);
        
        var unsignedContracts = await _context.Contracts
            .Where(c => !c.IsSigned && !c.IsCancelled &&
                        (softwareId == null || c.SoftwareId == softwareId))
            .SumAsync(c => c.Price);
        
        if (!string.IsNullOrEmpty(currency) && currency.ToUpper() != "PLN")
        {
            unsignedContracts = await ConvertCurrency(unsignedContracts, currency);
        }

        var totalPredicted = currentRevenue + unsignedContracts;

        return totalPredicted;
    }
    
    // AUTHENTICATION
    
    public async Task<Employee?> GetEmployeeByLogin(string login)
    {
        return await _context.Employees.FirstOrDefaultAsync(e => e.Login == login);
    }

    public async Task<bool> ValidateEmployeeCredentials(string login, string password)
    {
        var employee = await GetEmployeeByLogin(login);
        if (employee == null) return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, employee.PasswordHash);
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    // HELPER METHODS
    
    public async Task<bool> IsReturningClient(int clientId)
    {
        var hasContracts = await _context.Contracts
            .AnyAsync(c => c.ClientId == clientId && c.IsSigned);

        return hasContracts;
    }

    public async Task<decimal> ConvertCurrency(decimal amount, string targetCurrency)
    {
        var currency = targetCurrency.ToUpper();
        
        if (currency == "PLN")
        {
            return amount;
        }

        var response = await _httpClient.GetStringAsync("https://api.exchangerate-api.com/v4/latest/PLN");
        
        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;
        
        if (root.TryGetProperty("rates", out var ratesElement))
        {
            if (ratesElement.TryGetProperty(currency, out var rateElement))
            {
                var rate = rateElement.GetDecimal();
                return amount * rate;
            }
        }
        
        return amount;
    }
}