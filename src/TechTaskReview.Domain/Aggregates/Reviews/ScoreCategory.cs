namespace TechTaskReview.Domain.Aggregates.Reviews;

public enum ScoreCategory
{
    // General (applicable to both)
    CodeQuality = 1,
    Architecture = 2,
    Maintainability = 3,
    Naming = 4,
    ErrorHandling = 5,
    Security = 6,
    Performance = 7,
    Testability = 8,

    // Frontend-specific
    ComponentDesign = 100,
    StateManagement = 101,
    HooksUsage = 102,
    RerenderOptimization = 103,
    Accessibility = 104,
    Responsiveness = 105,

    // Backend-specific
    ApiDesign = 200,
    LayeredArchitecture = 201,
    DependencyInjection = 202,
    AsyncCorrectness = 203,
    SecurityPractices = 204,
    DbInteractionPatterns = 205
}
