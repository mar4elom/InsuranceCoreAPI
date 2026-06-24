using InsuranceCoreAPI.Domain;
using InsuranceCoreAPI.DTOs.Customers;

namespace InsuranceCoreAPI.Services.Interfaces;
    public interface ICustomerService
    {
        Task<Customer> CreateAsync(CreateCustomerRequest request);

        Task<Customer> GetByIdAsync(Guid id);
}
