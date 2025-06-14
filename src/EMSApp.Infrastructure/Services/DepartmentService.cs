using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Entities;

namespace EMSApp.Infrastructure;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _repo;
    private readonly IUserRepository _userRepository;
    public DepartmentService(IDepartmentRepository repo, IUserRepository userRepository)
    {
        _repo = repo;
        _userRepository = userRepository;
    }

    public async Task<Department> CreateAsync(string name, CancellationToken ct)
    {
        var department = new Department(name);
        await _repo.CreateAsync(department, ct);
        return department;
    }

    public Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken ct)
    {
        return _repo.GetAllAsync(ct);
    }

    public Task<Department?> GetByIdAsync(string id, CancellationToken ct)
    {
        return _repo.GetByIdAsync(id, ct);
    }

    public Task UpdateAsync(Department department, CancellationToken ct)
    {
        return _repo.UpdateAsync(department, false, ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct)
    {
        var deptEmployees = await _userRepository.ListByDepartmentAsync(id, ct);
        foreach (var user in deptEmployees)
        {
            user.UpdateDepartment("");
            await _userRepository.UpdateAsync(user, true, ct);
        }

        await _repo.DeleteAsync(id, ct);
    }

    public async Task AddEmployeeAsync(string id, string userId, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException($"User {userId} not found");
        user.UpdateDepartment(id);
        await _userRepository.UpdateAsync(user, true, ct);

        var department = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Department {id} not found");
        department.AddEmployee(userId);
        
        await _repo.UpdateAsync(department, false, ct);
    }

    public async Task RemoveEmployeeAsync(string id, string userId, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException($"User {userId} not found");
        user.UpdateDepartment("");
        await _userRepository.UpdateAsync(user, true, ct);

        var department = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Department {id} not found");
        department.RemoveEmployee(userId);
        await _repo.UpdateAsync(department, false, ct);
    }
}
