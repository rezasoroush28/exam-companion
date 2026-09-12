using Microsoft.Extensions.DependencyInjection;

namespace ExamCompanion.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ChallengePrototype.Services.ChallengeEngine>();
        services.AddSingleton(new ChallengePrototype.Services.HealthProgressionOptions());
        services.AddSingleton<ChallengePrototype.Services.ChallengeHealthEngine>();
        services.AddSingleton<ChallengePrototype.Services.IChallengeHealthPatternCalculator,
            ChallengePrototype.Services.ChallengeHealthPatternCalculator>();
        services.AddScoped<ChallengePrototype.Services.ITopicSuggestionService,
            ChallengePrototype.Services.TopicSuggestionService>();
        services.AddScoped<ChallengePrototype.Services.IStudyClaimService,
            ChallengePrototype.Services.StudyClaimService>();
        services.AddScoped<ChallengePrototype.Services.IRecoveryMiniGameService,
            ChallengePrototype.Services.RecoveryMiniGameService>();
        return services;
    }
}
