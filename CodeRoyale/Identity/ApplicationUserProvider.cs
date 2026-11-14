using CodeRoyale.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CodeRoyale.Identity;

/// <summary>
/// Scoped service that caches and provides the logged in user.
/// </summary>
public sealed class ApplicationUserProvider : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILogger<ApplicationUserProvider> _logger;
    private ApplicationUser? _user;
    private bool _hasUser;
    private int _hasEvent;

    public ApplicationUserProvider(ApplicationDbContext dbContext, AuthenticationStateProvider authStateProvider, ILogger<ApplicationUserProvider> logger)
    {
        _dbContext = dbContext;
        _authStateProvider = authStateProvider;
        _logger = logger;
    }

    private void OnAuthStateChanged(Task<AuthenticationState> newState)
    {
        Task.Run(async () =>
        {
            try
            {
                AuthenticationState state = await newState;
                await UpdateSavedState(state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error thrown in OnAuthStateChanged.");
            }
        });
    }

    public ValueTask<ApplicationUser?> GetLoggedInUserAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        if (_hasUser)
        {
            return new ValueTask<ApplicationUser?>(_user);
        }

        return new ValueTask<ApplicationUser?>(Core(token));

        async Task<ApplicationUser?> Core(CancellationToken token)
        {
            if (Interlocked.Exchange(ref _hasEvent, 1) != 1)
            {
                _authStateProvider.AuthenticationStateChanged += OnAuthStateChanged;
            }

            return await UpdateSavedState(await _authStateProvider.GetAuthenticationStateAsync(), token);
        }
    }

    private async Task<ApplicationUser?> UpdateSavedState(AuthenticationState state, CancellationToken token = default)
    {
        ClaimsPrincipal? userPrincipal = state?.User;
        string? id = userPrincipal?.FindFirst(u => u.Type == ClaimTypes.NameIdentifier)?.Value;
        ApplicationUser? user = null;
        if (!string.IsNullOrEmpty(id))
        {
            user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, token);
        }

        _user = user;
        _hasUser = true;
        return user;
    }

    public void Dispose()
    {
        if (_hasEvent != 0)
        {
            _authStateProvider.AuthenticationStateChanged -= OnAuthStateChanged;
            _hasEvent = 0;
        }
    }
}
