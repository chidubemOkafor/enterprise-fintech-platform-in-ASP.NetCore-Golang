using account.Data;
using Contracts.Grpc;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace account.Services;

// AccountService.AccountServiceBase is generated from Contracts/Protos/AccountService.proto
// at build time. It exists only after a build — before that the editor will flag it as missing.
public class AccountGrpcService : AccountService.AccountServiceBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountGrpcService> _logger;

    public AccountGrpcService(ApplicationDbContext context, ILogger<AccountGrpcService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public override async Task<GetUserResponse> GetUser(
        GetUserRequest request, ServerCallContext context)
    {
        var user = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == request.UserId, context.CancellationToken);

        if (user is null)
        {
            _logger.LogWarning("gRPC GetUser: no account for UserId {UserId}", request.UserId);
            throw new RpcException(new Status(
                StatusCode.NotFound, $"No account found for user {request.UserId}"));
        }

        // proto3 string fields reject null — the generated setters throw
        // ArgumentNullException rather than storing it. Empty string is how
        // proto3 represents "absent", so coalesce every nullable string.
        return new GetUserResponse
        {
            UserId = user.UserId,
            Email = user.Email ?? "",
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            PhoneNumber = user.PhoneNumber ?? ""
        };
    }
}
