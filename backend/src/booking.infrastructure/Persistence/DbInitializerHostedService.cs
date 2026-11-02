using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace booking.infrastructure.Persistence;

public class DbInitializerHostedService : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly IHostEnvironment _env;

    public DbInitializerHostedService(IServiceProvider sp, IHostEnvironment env)
    {
        _sp = sp;
        _env = env;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var infraRoot = Path.Combine(_env.ContentRootPath, "..", "booking.infrastructure");
        await DatabaseBootstrapper.InitAsync(db, infraRoot, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}