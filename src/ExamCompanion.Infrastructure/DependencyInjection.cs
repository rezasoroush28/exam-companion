using ChallengePrototype.Data;
using ChallengePrototype.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExamCompanion.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextFactory<ChallengeDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("ChallengeProgress"), sqlite =>
                sqlite.MigrationsAssembly(typeof(ChallengeDbContext).Assembly.FullName)));
        services.AddSingleton<IQuestionService, QuestionService>();
        services.AddSingleton<ILevelDesignService, LevelDesignService>();
        return services;
    }

    public static async Task MigrateInfrastructureAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ChallengeDbContext>().Database.MigrateAsync();
    }
}
