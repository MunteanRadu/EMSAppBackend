namespace EMSApp.Api;

public sealed record UpdatePunchRecordRequest(
     DateOnly Date,
     TimeOnly TimeIn,
     TimeOnly TimeOut
);
