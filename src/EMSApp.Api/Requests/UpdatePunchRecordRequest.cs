using EMSApp.Domain;

namespace EMSApp.Api;

public record class UpdatePunchRecordRequest
{
    public TimeOnly TimeOut { get; init; }
}
