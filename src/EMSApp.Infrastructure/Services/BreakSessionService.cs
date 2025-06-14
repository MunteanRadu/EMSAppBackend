using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;

namespace EMSApp.Infrastructure;

public class BreakSessionService : IBreakSessionService
{
    private readonly IBreakSessionRepository _repo;
    private readonly IPunchRecordRepository _punchRepo;
    private readonly IPolicyService _policyService;
    private readonly IMapper _mapper;

    public BreakSessionService(
        IBreakSessionRepository breakRepo,
        IPunchRecordRepository recordRepo,
        IPolicyService policyService,
        IMapper mapper)
    {
        _repo = breakRepo;
        _punchRepo = recordRepo;
        _policyService = policyService;
        _mapper = mapper;
    }

    public async Task<BreakSessionDto> CreateAsync(string punchId, TimeOnly startTime, CancellationToken ct)
    {
        var punch = await _punchRepo.GetByIdAsync(punchId, ct)
                   ?? throw new DomainException("Punch not found.");

        var policy = await _policyService.GetByYearAsync(punch.Date.Year, ct);
        if (policy == null)
            throw new DomainException($"No company policy defined for year {punch.Date.Year}.");

        var breaks = await _repo.ListByPunchRecordAsync(punchId, ct);

        if (breaks.Any(b => !b.EndTime.HasValue))
            throw new DomainException("There is already an open break.");

        var totalBreaksSoFar = breaks
            .Aggregate(TimeSpan.Zero, (acc, b) => acc + (b.Duration ?? TimeSpan.Zero));

        if (totalBreaksSoFar >= policy.MaxTotalBreakPerDay)
        {
            throw new DomainException($"Cannot start new break — total breaks for today already reached the maximum allowed ({policy.MaxTotalBreakPerDay}).");
        }

        var bs = new BreakSession(punchId, startTime);

        await _repo.CreateAsync(bs, ct);
        return _mapper.Map<BreakSessionDto>(bs);
    }

    public async Task<BreakSessionDto?> EndAsync(string punchId, string breakSessionId, TimeOnly endTime, CancellationToken ct)
    {
        var punch = await _punchRepo.GetByIdAsync(punchId, ct)
                   ?? throw new DomainException("Punch not found.");

        var policy = await _policyService.GetByYearAsync(punch.Date.Year, ct);
        if (policy == null)
            throw new DomainException($"No company policy defined for year {punch.Date.Year}.");

        var bs = await _repo.GetByIdAsync(breakSessionId, ct)
                 ?? throw new DomainException("Break session not found.");

        if (bs.PunchRecordId != punchId)
            throw new DomainException("Break session does not belong to this punch.");

        bs.End(endTime);

        if (bs.Duration.HasValue && bs.Duration.Value > policy.MaxSingleBreak)
        {
            Console.WriteLine($"[WARNING] Break {bs.Id} exceeded MaxSingleBreak ({bs.Duration} > {policy.MaxSingleBreak})");
            bs.MarkAsNonCompliant();
        }

        var breaks = await _repo.ListByPunchRecordAsync(punchId, ct);

        var totalBreaks = breaks
            .Where(b => b.Id == bs.Id ? true : b.EndTime.HasValue)
            .Aggregate(TimeSpan.Zero, (acc, b) => acc + (b.Duration ?? TimeSpan.Zero));

        if (totalBreaks > policy.MaxTotalBreakPerDay)
        {
            Console.WriteLine($"[WARNING] Total breaks for punch {punchId} exceeded MaxTotalBreakPerDay ({totalBreaks} > {policy.MaxTotalBreakPerDay})");
            punch.MarkAsNonCompliant();
            await _punchRepo.DeleteAsync(punchId);
        }

        await _repo.UpdateAsync(bs, false, ct);
        return _mapper.Map<BreakSessionDto>(bs);
    }


    public async Task<BreakSessionDto?> GetByIdAsync(string id, CancellationToken ct)
    {
        var bs = await _repo.GetByIdAsync(id, ct);
        return bs is null ? null : _mapper.Map<BreakSessionDto>(bs);
    }

    public async Task<IReadOnlyList<BreakSessionDto>> ListByPunchRecordAsync(string punchId, CancellationToken ct)
    {
        var list = await _repo.ListByPunchRecordAsync(punchId, ct);
        return list.Select(bs => _mapper.Map<BreakSessionDto>(bs)).ToList();
    }

    public Task UpdateAsync(BreakSession newBreakSession, CancellationToken ct)
    {
        return _repo.UpdateAsync(newBreakSession, false, ct);
    }

    public Task DeleteAsync(string id, CancellationToken ct)
    {
        return _repo.DeleteAsync(id, ct);
    }

    public Task<IReadOnlyList<BreakSession>> GetAllAsync(CancellationToken ct)
    {
        return _repo.GetAllAsync(ct);
    }
}
