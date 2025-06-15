using RevenueRecognitionAPI.Models;

namespace RevenueRecognitionAPI.Services;

public interface IRevenueService
{
    Task<bool> DoesIndividualClientExist(string pesel);
    Task<bool> DoesCompanyClientExist(string krs);
    Task<bool> DoesClientExist(int clientId);
    Task<IndividualClient> AddIndividualClient(IndividualClient client);
    Task<CompanyClient> AddCompanyClient(CompanyClient client);
    Task<Client?> GetClientById(int clientId);
    Task<IndividualClient?> UpdateIndividualClient(int clientId, IndividualClient updatedClient);
    Task<CompanyClient?> UpdateCompanyClient(int clientId, CompanyClient updatedClient);
    Task<bool> SoftDeleteIndividualClient(int clientId);
    Task<IEnumerable<Client>> GetAllClients();
    
    Task<Software?> GetSoftwareById(int softwareId);
    Task<IEnumerable<Software>> GetAllSoftware();
    Task<bool> DoesSoftwareExist(int softwareId);
    
    Task<Contract> CreateContract(Contract contract);
    Task<Contract?> GetContractById(int contractId);
    Task<bool> HasActiveContractForSoftware(int clientId, int softwareId);
    Task<IEnumerable<Contract>> GetClientContracts(int clientId);
    Task<bool> ProcessContractPayment(int contractId, decimal amount);
    Task CancelExpiredContracts();

    Task<IEnumerable<Discount>> GetActiveDiscounts();
    Task<decimal> CalculateDiscountedPrice(decimal originalPrice, int? softwareId, bool isReturningClient);
    
    Task<decimal> CalculateCurrentRevenue(int? softwareId = null, string? currency = null);
    Task<decimal> CalculatePredictedRevenue(int? softwareId = null, string? currency = null);
    
    Task<Employee?> GetEmployeeByLogin(string login);
    Task<bool> ValidateEmployeeCredentials(string login, string password);
    
    Task<bool> IsReturningClient(int clientId);
    Task<decimal> ConvertCurrency(decimal amount, string targetCurrency);
}