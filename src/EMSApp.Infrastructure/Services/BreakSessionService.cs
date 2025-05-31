using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;

namespace EMSApp.Infrastructure;

public class BreakSessionService : IBreakSessionService
{
    private readonly IBreakSessionRepository _repo;
    private readonly IPunchRecordRepository _punchRepo;
    private readonly IMapper _mapper;
    public BreakSessionService(IBreakSessionRepository breakRepo, IPunchRecordRepository recordRepo, IMapper mapper)
    {
        _repo = breakRepo;
        _punchRepo = recordRepo;
        _mapper = mapper;
    }

    public async Task<BreakSessionDto> CreateAsync(string punchId, TimeOnly startTime, CancellationToken ct)
    {
        var punch = await _punchRepo.GetByIdAsync(punchId, ct)
                   ?? throw new DomainException("Punch not found");

        var bs = punch.StartBreakSession(startTime);
        
        await _repo.CreateAsync(bs, ct);
        await _punchRepo.UpdateAsync(punch, true, ct);
        return _mapper.Map<BreakSessionDto>(bs);
    }

    public async Task<BreakSessionDto?> EndAsync(string punchId, string breakSessionId, TimeOnly endTime, CancellationToken ct)
    {
        var punch = await _punchRepo.GetByIdAsync(punchId, ct)
                   ?? throw new DomainException("Punch not found");

        punch.EndBreakSession(breakSessionId, endTime);

        var bs = punch.BreakSessions.Single(x => x.Id == breakSessionId);

        await _punchRepo.UpdateAsync(punch, true, ct);
        await _repo.UpdateAsync(bs, true, ct);
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
