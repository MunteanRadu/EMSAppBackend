using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using MongoDB.Bson;
using System.Threading;
using ZstdSharp.Unsafe;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EMSApp.Infrastructure;

public class PunchRecordService : IPunchRecordService
{
    private readonly IShiftAssignmentRepository _shiftAssignmentRepo;
    private readonly IPunchRecordRepository _repo;
    private readonly ILeaveRequestService _leaveService;
    private readonly IPolicyService _policyService;
    private readonly IBreakSessionRepository _breakSessionRepository;
    private readonly IMapper _mapper;
    public PunchRecordService(
        IShiftAssignmentRepository shiftAssignmentRepo,
        IPunchRecordRepository repo, 
        ILeaveRequestService leaveService, 
        IPolicyService policyService, 
        IBreakSessionRepository breakSessionRepository, 
        IMapper mapper)
    {
        _shiftAssignmentRepo = shiftAssignmentRepo;
        _repo = repo;
        _leaveService = leaveService;
        _policyService = policyService;
        _breakSessionRepository = breakSessionRepository;
        _mapper = mapper;
    }

    public async Task<PunchRecord> CreateAsync(string userId, DateOnly date, TimeOnly time, CancellationToken ct)
    {
        var leaves = await _leaveService.ListByUserAsync(userId, ct);
        if (leaves.Any(l =>
            l.Status == LeaveStatus.Approved &&
            l.StartDate <= date &&
            l.EndDate >= date))
        {
            throw new DomainException("Cannot punch in while you have an active approved leave");
        }

        var policy = await _policyService.GetByYearAsync(date.Year, ct);
        if (policy == null)
            throw new DomainException($"No company policy defined for year {date.Year}");

        var assignment = await _shiftAssignmentRepo.GetForUserOnDateAsync(userId, date, ct);
        TimeOnly windowStart, windowEnd;
        if (assignment != null)
        {
            windowStart = assignment.StartTime;
            windowEnd = assignment.EndTime;
        }
        else
        {
            windowStart = policy.WorkDayStart;
            windowEnd = policy.WorkDayStart;
        }

        var earliestAllowed = windowStart.Add(-policy.PunchInTolerance);

        if (windowStart == TimeOnly.Parse("00:00:00"))
        {
            earliestAllowed = windowStart;
        }

        if (time < earliestAllowed)
        {
            throw new DomainException(
                $"Punch-in at {time} is befored allowed window {earliestAllowed}-{assignment.StartTime} !!!!!!!!!! start {windowStart} end {windowEnd} year {date.Year}");
        }

        var punchRecord = new PunchRecord(userId, date, time);
        await _repo.CreateAsync(punchRecord, ct);
        return punchRecord;
    }

    public async Task<PunchRecordDto> PunchOutAsync(string punchId, TimeOnly timeOut, CancellationToken ct)
    {
        var punchRecord = await _repo.GetByIdAsync(punchId);
        if (punchRecord is null)
            throw new KeyNotFoundException($"Punch record with ID {punchId} not found");

        var policy = await _policyService.GetByYearAsync(punchRecord.Date.Year, ct);
        if (policy == null)
            throw new DomainException($"No company policy defined for year {punchRecord.Date.Year}.");

        var assignment = await _shiftAssignmentRepo.GetForUserOnDateAsync(punchRecord.UserId, punchRecord.Date, ct);
        TimeOnly windowStart, windowEnd;
        if (assignment != null)
        {
            windowStart = assignment.StartTime;
            windowEnd = assignment.EndTime;
        }
        else
        {
            windowStart = policy.WorkDayEnd;
            windowEnd = policy.WorkDayEnd;
        }

        var latestAllowed = windowEnd.Add(policy.PunchOutTolerance);
        if (windowEnd == TimeOnly.Parse("23:59:59"))
        {
            latestAllowed = windowEnd;
        }

        if (timeOut > latestAllowed)
        {
            throw new DomainException(
                $"Punch-out at {timeOut} is after allowed window {windowEnd}-{latestAllowed}");
        }

        var breaks = await _breakSessionRepository.ListByPunchRecordAsync(punchId, ct);
        var totalBreaks = breaks.Aggregate(TimeSpan.Zero, (acc, b) => acc + (b.Duration ?? TimeSpan.Zero));

        if (totalBreaks > policy.MaxTotalBreakPerDay)
        {
            Console.WriteLine($"[WARNING] Total breaks for punch {punchId} exceeded MaxTotalBreakPerDay ({totalBreaks} > {policy.MaxTotalBreakPerDay})");
            punchRecord.MarkAsNonCompliant();
        }

        punchRecord.PunchOut(timeOut);

        await _repo.UpdateAsync(punchRecord, true, ct);

        return _mapper.Map<PunchRecordDto>(punchRecord);
    }


    public async Task<PunchRecordDto?> GetByIdAsync(string id, CancellationToken ct)
    {
        var pr = await _repo.GetByIdAsync(id, ct);
        if (pr is null) return null;

        return _mapper.Map<PunchRecordDto?>(pr);
    }

    public async Task<PunchRecord?> GetByIdForUpdateAsync(string id, CancellationToken ct)
    {
        var pr = await _repo.GetByIdAsync(id, ct);
        if (pr is null) return null;

        return pr;
    }

    public Task<IReadOnlyList<PunchRecord>> ListByUserAsync(string userId, CancellationToken ct)
    {
        return _repo.ListByUserAsync(userId, ct);
    }

    public async Task UpdateAsync(PunchRecord record, CancellationToken ct)
    {
        var policy = await _policyService.GetByYearAsync(record.Date.Year, ct);
        if (policy == null)
            throw new DomainException($"No company policy defined for year {record.Date.Year}");

        var assignment = await _shiftAssignmentRepo.GetForUserOnDateAsync(record.UserId, record.Date, ct);

        TimeOnly windowStart = assignment?.StartTime ?? policy.WorkDayStart;
        TimeOnly windowEnd = assignment?.EndTime ?? policy.WorkDayStart;

        var earliest = windowStart.Add(-policy.PunchInTolerance);
        if (record.TimeIn < earliest)
            throw new DomainException(
                $"Punch-in {record.TimeIn} is befored allowed window {earliest}-{assignment.StartTime}");

        var latest = windowEnd.Add(policy.PunchOutTolerance);
        if (record.TimeOut > latest)
        {
            throw new DomainException(
                $"Punch-out at {record.TimeOut} is after allowed window {assignment.EndTime}-{latest}");
        }


        if (record.TimeOut.HasValue)
        {
            if (!policy.IsValidPunchOut(record.TimeOut.Value))
            {
                var punchOutWindowStart = policy.WorkDayEnd.Add(-policy.PunchOutTolerance);
                var punchOutWindowEnd = policy.WorkDayEnd.Add(policy.PunchOutTolerance);

                throw new DomainException($"Punch-out time {record.TimeOut.Value} does not respect company policy (allowed window: {punchOutWindowStart} - {punchOutWindowEnd}).");
            }

            var breaks = await _breakSessionRepository.ListByPunchRecordAsync(record.Id, ct);

            foreach (var breakSession in breaks)
            {
                if (breakSession.Duration.HasValue && breakSession.Duration.Value > policy.MaxSingleBreak)
                {
                    throw new DomainException($"Break session exceeds maximum allowed single break duration ({policy.MaxSingleBreak}).");
                }
            }

            var totalBreaks = breaks.Aggregate(TimeSpan.Zero, (acc, b) => acc + (b.Duration ?? TimeSpan.Zero));

            if (totalBreaks > policy.MaxTotalBreakPerDay)
            {
                throw new DomainException($"Total breaks for the day exceed allowed maximum ({policy.MaxTotalBreakPerDay}).");
            }
        }

        await _repo.UpdateAsync(record, false, ct);
    }


    public async Task DeleteAsync(string id, CancellationToken ct)
    {
        var breaks = await _breakSessionRepository.ListByPunchRecordAsync(id, ct);
        foreach (var b in breaks)
        {
            await _breakSessionRepository.DeleteAsync(b.Id, ct);
        }

        await _repo.DeleteAsync(id, ct);
    }

    public Task<IReadOnlyList<PunchRecord>> GetAllAsync(CancellationToken ct)
    {
        return _repo.GetAllAsync(ct);
    }

    public async Task<IReadOnlyList<DaySummaryDto>> GetMonthSummaryAsync(
    string userId, int year, int month, CancellationToken ct)
    {
        var records = await _repo.ListByUserAndMonthAsync(userId, year, month, ct);

        var daysWithPunches = records
            .Select(r => r.Date)
            .ToHashSet();

        var daysInMonth = DateTime.DaysInMonth(year, month);

        var summaries = new List<DaySummaryDto>(daysInMonth);
        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            summaries.Add(new DaySummaryDto(
                Date: date,
                HasPunches: daysWithPunches.Contains(date)
            ));
        }

        return summaries;
    }

    public async Task<IReadOnlyList<PunchRecordDto>> GetByDateAsync(string userId, DateOnly date, CancellationToken ct)
    {
        var records = await _repo.FilterByAsync(
            pr => pr.UserId == userId && pr.Date == date, ct);

        return records
            .Select(pr => _mapper.Map<PunchRecordDto>(pr)).ToList();
    }
}
