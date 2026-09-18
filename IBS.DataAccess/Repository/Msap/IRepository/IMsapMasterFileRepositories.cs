using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MasterFile;

namespace IBS.DataAccess.Repository.Msap.IRepository;

public interface IMsapChartOfAccountRepository : IRepository<ChartOfAccount>;
public interface IMsapSupplierRepository : IRepository<Supplier>
{
    Task<bool> IsSupplierExistAsync(string supplierName, string category, CancellationToken cancellationToken = default);
    Task<bool> IsSupplierExistAsync(string supplierName, string category, string company, CancellationToken cancellationToken = default);
    Task<bool> IsTinNoExistAsync(string tin, string branch, string category, CancellationToken cancellationToken = default);
    Task<bool> IsTinNoExistAsync(string tin, string branch, string category, string company, CancellationToken cancellationToken = default);
    Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(Supplier model, CancellationToken cancellationToken = default);
}
public interface IMsapTermsRepository : IRepository<Terms>
{
    Task UpdateAsync(Terms model, CancellationToken cancellationToken = default);
    Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetTermsListAsyncByCode(CancellationToken cancellationToken = default);
}
public interface IMsapBankAccountRepository : IRepository<BankAccount>
{
    Task<bool> IsBankAccountNoExist(string accountNo, CancellationToken cancellationToken = default);
}
public interface IMsapCustomerRepository : IRepository<Customer>
{
    Task<bool> IsTinNoExistAsync(string tin, CancellationToken cancellationToken = default);
    Task<bool> IsTinNoExistAsync(string tin, string customerType, CancellationToken cancellationToken = default);
    Task<bool> IsTinNoExistAsync(string tin, string customerType, string company, CancellationToken cancellationToken = default);
    Task<string> GenerateCodeAsync(string customerType, CancellationToken cancellationToken = default);
    Task UpdateAsync(Customer model, CancellationToken cancellationToken = default);
    Task<List<object>> SearchCustomersDtoAsync(string term, int limit, CancellationToken cancellationToken = default);
}
public interface IMsapEmployeeRepository : IRepository<Employee>;
