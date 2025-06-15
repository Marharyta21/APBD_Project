using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RevenueRecognitionAPI.Data;
using RevenueRecognitionAPI.Models;
using RevenueRecognitionAPI.Services;

namespace RevenueRecognitionAPI.Tests
{
    [TestClass]
    public class RevenueServiceTests
    {
        private DatabaseContext GetTestContext()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new DatabaseContext(options);
        }

        private RevenueService GetTestService(DatabaseContext context)
        {
            var httpClient = new HttpClient();
            return new RevenueService(context, httpClient);
        }

        #region Discount Tests

        [TestMethod]
        public async Task CalculateDiscountedPrice_ChoosesHighestDiscount()
        {
            using var context = GetTestContext();
            var service = GetTestService(context);
            
            context.Discounts.AddRange(
                new Discount { Id = 1, Name = "Small", Percentage = 10M, 
                              StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1) },
                new Discount { Id = 2, Name = "Big", Percentage = 20M, 
                              StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1) }
            );
            await context.SaveChangesAsync();
            
            var result = await service.CalculateDiscountedPrice(1000M, null, false);
            
            Assert.AreEqual(800M, result);
        }

        [TestMethod]
        public async Task CalculateDiscountedPrice_ReturningClientGetsExtraDiscount()
        {
            using var context = GetTestContext();
            var service = GetTestService(context);

            context.Discounts.Add(
                new Discount { Id = 1, Name = "Base", Percentage = 10M, 
                              StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1) }
            );
            await context.SaveChangesAsync();
            
            var result = await service.CalculateDiscountedPrice(1000M, null, true);
            
            Assert.AreEqual(850M, result);
        }

        #endregion

        #region Client Tests

        [TestMethod]
        public async Task IsReturningClient_WithSignedContract_ReturnsTrue()
        {
            using var context = GetTestContext();
            var service = GetTestService(context);

            var client = new IndividualClient 
            { 
                Id = 1, FirstName = "John", LastName = "Doe", PESEL = "12345678901",
                Address = "Test St", Email = "test@test.com", PhoneNumber = "123456789"
            };
            var contract = new Contract 
            { 
                Id = 1, ClientId = 1, SoftwareId = 1, IsSigned = true, Price = 1000M,
                StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30),
                SoftwareVersion = "1.0"
            };

            context.Clients.Add(client);
            context.Contracts.Add(contract);
            await context.SaveChangesAsync();
            
            var result = await service.IsReturningClient(1);
            
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task SoftDeleteClient_OverwritesData()
        {
            using var context = GetTestContext();
            var service = GetTestService(context);

            var client = new IndividualClient 
            { 
                Id = 1, FirstName = "John", LastName = "Doe", PESEL = "12345678901",
                Address = "Test St", Email = "test@test.com", PhoneNumber = "123456789"
            };
            context.IndividualClients.Add(client);
            await context.SaveChangesAsync();
            
            await service.SoftDeleteIndividualClient(1);
            
            var deletedClient = await context.IndividualClients.FindAsync(1);
            Assert.IsTrue(deletedClient.IsDeleted);
            Assert.AreEqual("DELETED", deletedClient.FirstName);
        }

        #endregion

        #region Contract Tests

        [TestMethod]
        public async Task ProcessContractPayment_FullPayment_SignsContract()
        {
            using var context = GetTestContext();
            var service = GetTestService(context);

            var client = new IndividualClient 
            { 
                Id = 1, FirstName = "John", LastName = "Doe", PESEL = "12345678901",
                Address = "Test St", Email = "test@test.com", PhoneNumber = "123456789"
            };
            var software = new Software 
            { 
                Id = 1, Name = "Test Software", Description = "Test", CurrentVersion = "1.0",
                Category = "Test", UpfrontPrice = 1000M
            };
            var contract = new Contract 
            { 
                Id = 1, ClientId = 1, SoftwareId = 1, Price = 1000M, IsSigned = false,
                StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30),
                SoftwareVersion = "1.0"
            };

            context.Clients.Add(client);
            context.Software.Add(software);
            context.Contracts.Add(contract);
            await context.SaveChangesAsync();
            
            var result = await service.ProcessContractPayment(1, 1000M);
            
            Assert.IsTrue(result);
            var updatedContract = await context.Contracts.FindAsync(1);
            Assert.IsTrue(updatedContract.IsSigned);
        }

        [TestMethod]
        public async Task ProcessContractPayment_ExpiredContract_Fails()
        {
            using var context = GetTestContext();
            var service = GetTestService(context);

            var client = new IndividualClient 
            { 
                Id = 1, FirstName = "John", LastName = "Doe", PESEL = "12345678901",
                Address = "Test St", Email = "test@test.com", PhoneNumber = "123456789"
            };
            var software = new Software 
            { 
                Id = 1, Name = "Test Software", Description = "Test", CurrentVersion = "1.0",
                Category = "Test", UpfrontPrice = 1000M
            };
            var expiredContract = new Contract 
            { 
                Id = 1, ClientId = 1, SoftwareId = 1, Price = 1000M, IsSigned = false,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-1),
                SoftwareVersion = "1.0"
            };

            context.Clients.Add(client);
            context.Software.Add(software);
            context.Contracts.Add(expiredContract);
            await context.SaveChangesAsync();
            
            var result = await service.ProcessContractPayment(1, 1000M);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task CancelExpiredContracts_RefundsPayments()
        {
            using var context = GetTestContext();
            var service = GetTestService(context);

            var client = new IndividualClient 
            { 
                Id = 1, FirstName = "John", LastName = "Doe", PESEL = "12345678901",
                Address = "Test St", Email = "test@test.com", PhoneNumber = "123456789"
            };
            var software = new Software 
            { 
                Id = 1, Name = "Test Software", Description = "Test", CurrentVersion = "1.0",
                Category = "Test", UpfrontPrice = 1000M
            };
            var expiredContract = new Contract 
            { 
                Id = 1, ClientId = 1, SoftwareId = 1, Price = 1000M, IsSigned = false,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-1),
                SoftwareVersion = "1.0"
            };
            var payment = new Payment 
            { 
                Id = 1, ContractId = 1, Amount = 500M, PaymentDate = DateTime.UtcNow.AddDays(-5)
            };

            context.Clients.Add(client);
            context.Software.Add(software);
            context.Contracts.Add(expiredContract);
            context.Payments.Add(payment);
            await context.SaveChangesAsync();

            await service.CancelExpiredContracts();

            var cancelledContract = await context.Contracts.FindAsync(1);
            var refundedPayment = await context.Payments.FindAsync(1);
            
            Assert.IsTrue(cancelledContract.IsCancelled);
            Assert.IsTrue(refundedPayment.IsRefunded);
        }

        #endregion

        #region Revenue Tests

        [TestMethod]
        public async Task CalculateCurrentRevenue_OnlyCountsSignedContracts()
        {
            using var context = GetTestContext();
            var service = GetTestService(context);

            var client = new IndividualClient 
            { 
                Id = 1, FirstName = "John", LastName = "Doe", PESEL = "12345678901",
                Address = "Test St", Email = "test@test.com", PhoneNumber = "123456789"
            };
            var software = new Software 
            { 
                Id = 1, Name = "Test Software", Description = "Test", CurrentVersion = "1.0",
                Category = "Test", UpfrontPrice = 1000M
            };
            
            var signedContract = new Contract 
            { 
                Id = 1, ClientId = 1, SoftwareId = 1, Price = 1000M, IsSigned = true,
                StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30),
                SoftwareVersion = "1.0"
            };
            var signedPayment = new Payment 
            { 
                Id = 1, ContractId = 1, Amount = 1000M
            };
            
            var unsignedContract = new Contract 
            { 
                Id = 2, ClientId = 1, SoftwareId = 1, Price = 1000M, IsSigned = false,
                StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30),
                SoftwareVersion = "1.0"
            };
            var unsignedPayment = new Payment 
            { 
                Id = 2, ContractId = 2, Amount = 500M
            };

            context.Clients.Add(client);
            context.Software.Add(software);
            context.Contracts.AddRange(signedContract, unsignedContract);
            context.Payments.AddRange(signedPayment, unsignedPayment);
            await context.SaveChangesAsync();
            
            var result = await service.CalculateCurrentRevenue();
            
            Assert.AreEqual(1000M, result);
        }

        [TestMethod]
        public async Task CalculatePredictedRevenue_IncludesUnsignedContracts()
        {
            using var context = GetTestContext();
            var service = GetTestService(context);

            var client = new IndividualClient 
            { 
                Id = 1, FirstName = "John", LastName = "Doe", PESEL = "12345678901",
                Address = "Test St", Email = "test@test.com", PhoneNumber = "123456789"
            };
            var software = new Software 
            { 
                Id = 1, Name = "Test Software", Description = "Test", CurrentVersion = "1.0",
                Category = "Test", UpfrontPrice = 1000M
            };

            var signedContract = new Contract 
            { 
                Id = 1, ClientId = 1, SoftwareId = 1, Price = 1000M, IsSigned = true,
                StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30),
                SoftwareVersion = "1.0"
            };
            var currentPayment = new Payment { Id = 1, ContractId = 1, Amount = 1000M };

            var unsignedContract = new Contract 
            { 
                Id = 2, ClientId = 1, SoftwareId = 1, Price = 2000M, IsSigned = false,
                StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30),
                SoftwareVersion = "1.0"
            };

            context.Clients.Add(client);
            context.Software.Add(software);
            context.Contracts.AddRange(signedContract, unsignedContract);
            context.Payments.Add(currentPayment);
            await context.SaveChangesAsync();

            var result = await service.CalculatePredictedRevenue();

            Assert.AreEqual(3000M, result); // 1000 (current) + 2000 (unsigned)
        }

        #endregion

        #region Model Property Tests

        [TestMethod]
        public void Contract_TotalPaid_CalculatesCorrectly()
        {
            var contract = new Contract();
            contract.Payments = new List<Payment>
            {
                new Payment { Amount = 500M, IsRefunded = false },
                new Payment { Amount = 300M, IsRefunded = false },
                new Payment { Amount = 100M, IsRefunded = true }
            };

            Assert.AreEqual(800M, contract.TotalPaid);
        }

        [TestMethod]
        public void Contract_IsFullyPaid_WhenTotalEqualsPrice()
        {
            var contract = new Contract { Price = 1000M };
            contract.Payments = new List<Payment>
            {
                new Payment { Amount = 1000M, IsRefunded = false }
            };

            Assert.IsTrue(contract.IsFullyPaid);
        }

        [TestMethod]
        public void Contract_IsPaymentWindowOpen_WhenNotExpiredAndNotCancelled()
        {
            var contract = new Contract
            {
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(5),
                IsCancelled = false
            };

            Assert.IsTrue(contract.IsPaymentWindowOpen);
        }

        [TestMethod]
        public void Discount_IsActiveAt_WhenWithinDateRange()
        {
            var discount = new Discount
            {
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(5)
            };
            
            Assert.IsTrue(discount.IsActiveAt(DateTime.UtcNow));
        }

        #endregion
    }
}