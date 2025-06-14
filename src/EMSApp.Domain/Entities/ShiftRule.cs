using EMSApp.Domain.Exceptions;

namespace EMSApp.Domain;

public class ShiftRule
{
    public string Id { get; private set; }
    public string DepartmentId { get; private set; }
    public int MinPerShift1 { get; private set; }
    public int MinPerShift2 { get; private set; }
    public int MinPerNightShift { get; private set; }
    public int MaxConsecutiveNightShifts { get; private set; }
    public double MinRestHoursBetweenShifts { get; set; }

    public ShiftRule(string departmentId,
                     int minShift1,
                     int minShift2,
                     int minNightShift,
                     int maxConsecutiveNight,
                     double minRestHoursBetweenShifts)
    {
        if (string.IsNullOrWhiteSpace(departmentId))
            throw new DomainException("DepartmentId cannot be empty");
        if (minShift1 < 0 || minShift2 < 0 || minNightShift < 0)
            throw new DomainException("Min values cannot be negative");
        if (maxConsecutiveNight < 1)
            throw new DomainException("MaxConsecutiveNightShifts must be at least 1");

        Id = Guid.NewGuid().ToString();
        DepartmentId = departmentId;
        MinPerShift1 = minShift1;
        MinPerShift2 = minShift2;
        MinPerNightShift = minNightShift;
        MaxConsecutiveNightShifts = maxConsecutiveNight;
        MinRestHoursBetweenShifts = minRestHoursBetweenShifts;
    }

    public void Update(int minShift1, int minShift2, int minNightShift, int maxConsecutiveNight, double minRestHours)
    {
        if (minShift1 < 0 || minShift2 < 0 || minNightShift < 0)
            throw new DomainException("Min values cannot be negative");
        if (maxConsecutiveNight < 1)
            throw new DomainException("MaxConsecutiveNightShifts must be at least 1");

        MinPerShift1 = minShift1;
        MinPerShift2 = minShift2;
        MinPerNightShift = minNightShift;
        MaxConsecutiveNightShifts = maxConsecutiveNight;
        MinRestHoursBetweenShifts = minRestHours;
    }
}
