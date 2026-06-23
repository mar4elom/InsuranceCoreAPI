using InsuranceCoreAPI.Domain;
using InsuranceCoreAPI.DTOs.Customers;
using InsuranceCoreAPI.Errors;
using InsuranceCoreAPI.Repositories.Interfaces;
using InsuranceCoreAPI.Services.Interfaces;

namespace InsuranceCoreAPI.Services;

public sealed class CustomerService(ICustomerRepository repository) : ICustomerService
{
    public async Task<Customer> CreateAsync(CreateCustomerRequest request)
    {
        var customer = new Customer { FullName = request.FullName };
        return await repository.AddAsync(customer);
    }

    public async Task<Customer> GetByIdAsync(Guid id)
    {
        var customer = await repository.GetByIdAsync(id);
        if (customer is null)
            throw new NotFoundException($"Customer '{id}' was not found.");

        return customer;
    }
}