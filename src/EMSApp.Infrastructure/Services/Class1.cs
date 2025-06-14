using EMSApp.Application.Interfaces;
using EMSApp.Application;
using EMSApp.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace EMSApp.Infrastructure.Services;

public class ScheduleGenerationService : IScheduleGenerationService
{
    private readonly IChatBotService _chatBotService;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly ILeaveRequestRepository _leaveRepo;
    private readonly IShiftRuleRepository _shiftRuleRepo;
    private readonly IShiftAssignmentRepository _shiftAssignmentRepo;

    public ScheduleGenerationService(
        IChatBotService chatBotService,
        IDepartmentRepository departmentRepo,
        ILeaveRequestRepository leaveRepo,
        IShiftRuleRepository shiftRuleRepo,
        IShiftAssignmentRepository shiftAssignmentRepo)
    {
        _chatBotService = chatBotService;
        _departmentRepo = departmentRepo;
        _leaveRepo = leaveRepo;
        _shiftRuleRepo = shiftRuleRepo;
        _shiftAssignmentRepo = shiftAssignmentRepo;
    }

    public async Task GenerateAndSaveWeeklyScheduleAsync(string departmentId, DateOnly weekStart, CancellationToken ct)
    {
        var department = await _departmentRepo.GetByIdAsync(departmentId, ct);
        var employeeIds = department.Employees;

        var leaveRequests = await _leaveRepo.GetApprovedLeavesForWeekAsync(employeeIds, weekStart, ct);

        var employeesList = employeeIds.Select(id => new
        {
            userId = id
        }).ToList();
        var employeesJSON = JsonSerializer.Serialize(employeesList);

        var leavesList = leaveRequests.Select(lr => new
        {
            userId = lr.UserId,
            startDate = lr.StartDate.ToString("yyyy-MM-dd"),
            endDate = lr.EndDate.ToString("yyyy-MM-dd")
        }).ToList();
        var leavesJSON = JsonSerializer.Serialize(leavesList);

        var rule = await _shiftRuleRepo.GetByDepartmentAsync(departmentId, ct);

        var prompt = $@"These are the employees in our department:
{employeesJSON}

Approved leaves for week {weekStart:yyyy-MM-dd}..{weekStart.AddDays(6):yyyy-MM-dd}:
{leavesJSON}

Shift rules:
- Shift1 (08:00:00..16:00:00) => minimum {rule.MinPerShift1} employees
- Shift2 (16:00:00..00:00:00) => minimum {rule.MinPerShift2} employees
- NightShift (00:00:00..8:00:00) => minimum {rule.MinPerNightShift} employees
Every working day (monday..friday) must be completed with these requirements.

Generate a JSON object with the following structure. Do not include any other text or explanation in your response, only the JSON.:
[
{{ 
""userId"": ""u1"", 
""date"": ""2025-06-09"", 
""shift"": ""Shift1"", 
""startTime"": ""08:00:00"", 
""endTime"": ""16:00:00""
}},
{{ 
""userId"": ""u3"", 
""date"": ""2025-06-09"", 
""shift"": ""Shift2"", 
""startTime"": ""16:00:00"", 
""endTime"": ""00:00:00""
}},
  ...
]
";

        var response = await _chatBotService.GetChatResponseAsync(prompt, ct); ;

        List<ShiftFromAiDto>? shiftDtos;
        try
        {
            shiftDtos = JsonSerializer.Deserialize<List<ShiftFromAiDto>>(response,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
        }
        catch (JsonException ex)
        {
            throw new ApplicationException("The AI chatbot did not return a valid JSON", ex);
        }

        if (shiftDtos == null || !shiftDtos.Any())
        {
            return;
        }

        await _shiftAssignmentRepo.DeleteByDepartmentAndWeekAsync(departmentId, weekStart, ct);

        var newAssignments = new List<ShiftAssignment>();
        foreach (var dto in shiftDtos)
        {
            var date = DateOnly.Parse(dto.Date);
            var startTime = TimeOnly.Parse(dto.StartTime);
            var endTime = TimeOnly.Parse(dto.EndTime);

            if (!Enum.TryParse<ShiftType>(dto.Shift, ignoreCase: true, out var shiftType))
            {
                continue;
            }

            var assignment = new ShiftAssignment(
                userId: dto.UserId,
                date: date,
                shift: shiftType,
                startTime: startTime,
                endTime: endTime,
                departmentId: departmentId,
                managerId: department.ManagerId
            );
            newAssignments.Add(assignment);
        }

        await _shiftAssignmentRepo.AddManyAsync(newAssignments, ct);
    }
}

