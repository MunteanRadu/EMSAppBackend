using EMSApp.Application.Interfaces;
using EMSApp.Application;
using EMSApp.Domain;
using System.Text.Json;

namespace EMSApp.Infrastructure.Services;

public class ScheduleGenerationService : IScheduleGenerationService
{
    private readonly IScheduleRepository _scheduleRepo;
    private readonly IChatBotService _chatBotService;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly ILeaveRequestRepository _leaveRepo;
    private readonly IShiftRuleRepository _shiftRuleRepo;

    public ScheduleGenerationService(
        IScheduleRepository scheduleRepo,
        IChatBotService chatBotService,
        IDepartmentRepository departmentRepo,
        ILeaveRequestRepository leaveRepo,
        IShiftRuleRepository shiftRuleRepo)
    {
        _scheduleRepo = scheduleRepo;
        _chatBotService = chatBotService;
        _departmentRepo = departmentRepo;
        _leaveRepo = leaveRepo;
        _shiftRuleRepo = shiftRuleRepo;
    }

    public async Task<string> GetScheduleSuggestionJsonAsync(string departmentId, DateOnly weekStart, CancellationToken ct)
    {
        var department = await _departmentRepo.GetByIdAsync(departmentId, ct);
        var employeeIds = department.Employees;

        var leaveRequests = await _leaveRepo.GetApprovedLeavesForWeekAsync(employeeIds, weekStart, ct);

        var schedules = await _scheduleRepo.GetByDepartmentAsync(departmentId, ct);
        var templatesList = schedules.Select(s => new {
            day = s.Day.ToString(),
            shift = s.ShiftType.ToString(),
            startTime = s.StartTime.ToString(@"hh\:mm\:ss"),
            endTime = s.EndTime.ToString(@"hh\:mm\:ss")
        }).ToList();
        var templatesJSON = JsonSerializer.Serialize(templatesList, new JsonSerializerOptions
        {
            WriteIndented = true
        });

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

        var prompt = $@"You are tasked with generating employee shifts for the specified department for the workweek from {weekStart:yyyy-MM-dd} to {weekStart.AddDays(6):yyyy-MM-dd}.

Department’s weekly templates:
{templatesJSON}

Employees in department:
{employeesJSON}

Active approved leaves for this week:
{leavesJSON}

Shift rules to strictly adhere to:
- Shift1: 08:00:00 to 15:59:59, minimum {rule.MinPerShift1} employees required.
- Shift2: 16:00:00 to 23:59:59, minimum {rule.MinPerShift2} employees required.
- NightShift: 00:00:00 to 07:59:59, minimum {rule.MinPerNightShift} employees required.
- Employees cannot exceed {rule.MaxConsecutiveNightShifts} consecutive NightShift assignments.
- Employees MUST have at least {rule.MinRestHoursBetweenShifts} rest hours between shifts.

IMPORTANT CONSTRAINTS:
- ONLY schedule shifts for weekdays (Monday to Friday). DO NOT schedule shifts for weekends (Saturday or Sunday).
- ENSURE EACH employee is assigned only ONE shift per day.
- DO NOT assign shifts to employees on approved leave during their leave dates.

Your response must be exclusively a JSON array structured as follows:
[
  {{
    ""userId"": ""exampleUserId"",
    ""date"": ""2025-06-09"",
    ""shift"": ""Shift1"",
    ""startTime"": ""08:00:00"",
    ""endTime"": ""15:59:59""
  }},

  {{
    ""userId"": ""exampleUserId"",
    ""date"": ""2025-06-09"",
    ""shift"": ""Shift2"",
    ""startTime"": ""16:00:00"",
    ""endTime"": ""23:59:59""
  }}
]

Do not include explanations, notes, or additional text; provide only the JSON.";


        var responseJson = await _chatBotService.GetChatResponseAsync(prompt, ct);
        var cleanedJson = responseJson.Trim();
        if (cleanedJson.StartsWith("```json"))
        {
            cleanedJson = cleanedJson.Substring("```json".Length);
        }
        else if (cleanedJson.StartsWith("```"))
        {
            cleanedJson = cleanedJson.Substring("```".Length);
        }

        if (cleanedJson.EndsWith("```"))
        {
            cleanedJson = cleanedJson.Substring(0, cleanedJson.LastIndexOf("```"));
        }

        Console.WriteLine(cleanedJson);

        return cleanedJson;
    }
}

