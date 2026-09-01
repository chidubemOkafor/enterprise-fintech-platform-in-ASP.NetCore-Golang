using Microsoft.EntityFrameworkCore;
using wallet.Data;

namespace wallet.Services;

public interface IAccountNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken ct = default);
}

public class AccountNumberGenerator : IAccountNumberGenerator
{
    private const string InstitutionCode = "058"; // your assigned bank/PSP code

    private readonly ApplicationDbContext _context;

    public AccountNumberGenerator(ApplicationDbContext context) => _context = context;

    public async Task<string> GenerateAsync(CancellationToken ct = default)
    {
        var serial = await _context.Database
            .SqlQueryRaw<long>("SELECT nextval('wallet_account_number_seq') AS \"Value\"")
            .SingleAsync(ct);

        var body = serial.ToString("D9");
        return body + CheckDigit(InstitutionCode + body);
    }

    // CBN NUBAN check digit
    private static char CheckDigit(string twelveDigits)
    {
        ReadOnlySpan<int> weights = [3, 7, 3, 3, 7, 3, 3, 7, 3, 3, 7, 3];
        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += (twelveDigits[i] - '0') * weights[i];

        var cd = 10 - (sum % 10);
        return (char)('0' + (cd == 10 ? 0 : cd));
    }
}
