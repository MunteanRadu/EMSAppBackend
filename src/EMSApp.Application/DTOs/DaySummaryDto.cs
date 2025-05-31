namespace EMSApp.Application;

public sealed record DaySummaryDto(
    DateOnly Date,
    bool HasPunches
);
