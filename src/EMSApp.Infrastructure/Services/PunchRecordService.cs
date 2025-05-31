using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using MongoDB.Bson;
using ZstdSharp.Unsafe;

namespace EMSApp.Infrastructure;

public class PunchRecordService : IPunchRecordService
{
    private readonly IPunchRecordRepository _repo;
    private readonly ILeaveRequestService _leaveService;
    private readonly IMapper _mapper;
    public PunchRecordService(IPunchRecordRepository repo, ILeaveRequestService leaveService, IMapper mapper)
    {
        _repo = repo;
        _leaveService = leaveService;
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
            throw new DomainException("Cannot punch in while you have an active approved leave.");
        }
        var punchRecord = new PunchRecord(userId, date, time);
        await _repo.CreateAsync(punchRecord, ct);
        return punchRecord;
    }

    public async Task<PunchRecordDto> PunchOutAsync(string punchId, TimeOnly timeOut, CancellationToken ct)
    {
        var punchRecord = await _repo.GetByIdAsync(punchId);
        if (punchRecord is null) return null;

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

    public Task<IReadOnlyList<PunchRecord>> ListByUserAsync(string userId, CancellationToken ct)
    {
        return _repo.ListByUserAsync(userId, ct);
    }

    public Task UpdateAsync(PunchRecord record, CancellationToken ct)
    {
        return _repo.UpdateAsync(record, false, ct);
    }

    public Task DeleteAsync(string id, CancellationToken ct)
    {
        return _repo.DeleteAsync(id, ct);
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
