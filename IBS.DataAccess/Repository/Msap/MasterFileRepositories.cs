using IBS.DataAccess.Data;
using IBS.DataAccess.Repository;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Msap;

public sealed class MsapChartOfAccountRepository(ApplicationDbContext db) : Repository<ChartOfAccount>(db), IMsapChartOfAccountRepository;
public sealed class MsapSupplierRepository : Repository<Supplier>, IMsapSupplierRepository
{
    private readonly ApplicationDbContext _context;

    public MsapSupplierRepository(ApplicationDbContext context) : base(context) => _context = context;

    public Task<bool> IsSupplierExistAsync(string supplierName, string category, CancellationToken cancellationToken = default) => dbSet.AnyAsync(x => x.SupplierName == supplierName && x.Category == category, cancellationToken);
    public Task<bool> IsSupplierExistAsync(string supplierName, string category, string company, CancellationToken cancellationToken = default) => dbSet.AnyAsync(x => x.SupplierName == supplierName && x.Category == category && x.Company == company, cancellationToken);
    public Task<bool> IsTinNoExistAsync(string tin, string branch, string category, CancellationToken cancellationToken = default) => dbSet.AnyAsync(x => x.SupplierTin == tin && x.Branch == branch && x.Category == category, cancellationToken);
    public Task<bool> IsTinNoExistAsync(string tin, string branch, string category, string company, CancellationToken cancellationToken = default) => dbSet.AnyAsync(x => x.SupplierTin == tin && x.Branch == branch && x.Category == category && x.Company == company, cancellationToken);
    public async Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default) => $"SUP{await dbSet.CountAsync(cancellationToken) + 1:0000}";
    public async Task UpdateAsync(Supplier model, CancellationToken cancellationToken = default) { dbSet.Update(model); await _context.SaveChangesAsync(cancellationToken); }
}
public sealed class MsapEmployeeRepository(ApplicationDbContext db) : Repository<Employee>(db), IMsapEmployeeRepository;

public sealed class MsapTermsRepository : Repository<Terms>, IMsapTermsRepository
{
    private readonly ApplicationDbContext _context;

    public MsapTermsRepository(ApplicationDbContext context) : base(context) => _context = context;

    public async Task UpdateAsync(Terms model, CancellationToken cancellationToken = default)
    {
        dbSet.Update(model);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<List<SelectListItem>> GetTermsListAsyncByCode(CancellationToken cancellationToken = default) =>
        dbSet.OrderBy(x => x.TermsCode)
            .Select(x => new SelectListItem { Value = x.TermsCode, Text = x.TermsCode })
            .ToListAsync(cancellationToken);
}

public sealed class MsapBankAccountRepository(ApplicationDbContext context) : Repository<BankAccount>(context), IMsapBankAccountRepository
{
    public Task<bool> IsBankAccountNoExist(string accountNo, CancellationToken cancellationToken = default) =>
        dbSet.AnyAsync(x => x.AccountNo == accountNo, cancellationToken);
}

public sealed class MsapCustomerRepository : Repository<Customer>, IMsapCustomerRepository
{
    private readonly ApplicationDbContext _context;

    public MsapCustomerRepository(ApplicationDbContext context) : base(context) => _context = context;

    public Task<bool> IsTinNoExistAsync(string tin, CancellationToken cancellationToken = default) =>
        dbSet.AnyAsync(x => x.CustomerTin == tin, cancellationToken);

    public Task<bool> IsTinNoExistAsync(string tin, string customerType, CancellationToken cancellationToken = default) =>
        dbSet.AnyAsync(x => x.CustomerTin == tin && x.CustomerType == customerType, cancellationToken);

    public Task<bool> IsTinNoExistAsync(string tin, string customerType, string company, CancellationToken cancellationToken = default) =>
        dbSet.AnyAsync(x => x.CustomerTin == tin && x.CustomerType == customerType && x.Company == company, cancellationToken);

    public async Task<string> GenerateCodeAsync(string customerType, CancellationToken cancellationToken = default)
    {
        var count = await dbSet.CountAsync(cancellationToken);
        return $"{customerType[..Math.Min(3, customerType.Length)].ToUpperInvariant()}{count + 1:0000}";
    }

    public async Task UpdateAsync(Customer model, CancellationToken cancellationToken = default)
    {
        dbSet.Update(model);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<object>> SearchCustomersDtoAsync(string term, int limit, CancellationToken cancellationToken = default)
    {
        var query = dbSet.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var value = term.ToLower();
            query = query.Where(x => x.CustomerName.ToLower().Contains(value) || x.CustomerCode.ToLower().Contains(value));
        }

        return await query.OrderBy(x => x.CustomerName).Take(limit)
            .Select(x => (object)new
            {
                value = x.CustomerId,
                name = x.CustomerName,
                vatType = x.VatType,
                isUndoc = x.Type,
                address = x.CustomerAddress,
                tinNo = x.CustomerTin,
                terms = x.CustomerTerms,
                businessStyle = x.BusinessStyle ?? "-",
                withholdingTax = x.WithHoldingTax,
                withholdingVat = x.WithHoldingVat,
                hasPrincipal = _context.Set<IBS.Models.MSAP.MasterFile.Principal>().Any(p => p.CustomerId == x.CustomerId)
            }).ToListAsync(cancellationToken);
    }
}
