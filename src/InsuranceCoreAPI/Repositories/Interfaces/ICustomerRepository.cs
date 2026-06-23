using InsuranceCoreAPI.Domain;

namespace InsuranceCoreAPI.Repositories.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id);
    Task<Customer> AddAsync(Customer customer);
}
