using EMSApp.Application;
using EMSApp.Domain.Entities;
using EMSApp.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using EMSApp.Application.Interfaces;
using AutoMapper;

namespace EMSApp.Api;

[ApiController]
[Route("api/shifts")]
public class ShiftAssignmentController : ControllerBase
{
    private readonly IScheduleGenerationService _scheduleGenerationService;
    private readonly IShiftAssignmentService _service;
    private readonly IUserRepository _userRepo;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly IMapper _mapper;

    public ShiftAssignmentController(
        IScheduleGenerationService scheduleGenerationService,
        IShiftAssignmentService service,
        IUserRepository userRepo,
        IDepartmentRepository departmentRepo,
        IMapper mapper)
    {
        _scheduleGenerationService = scheduleGenerationService;
        _service = service;
        _userRepo = userRepo;
        _departmentRepo = departmentRepo;
        _mapper = mapper;
    }

    [HttpPost("{departmentId}/generate")]
    [Authorize(Roles = "manager,admin")]
    public async Task<IActionResult> GenerateWeeklySchedule(
        [FromRoute] string departmentId,
        [FromQuery] DateTime weekStart,
        CancellationToken ct)
    {
        var startDate = DateOnly.FromDateTime(weekStart.Date);
        if (startDate.DayOfWeek != DayOfWeek.Monday)
            return BadRequest("WeekStart must be Monday");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();
            
        var dept = await _departmentRepo.GetByIdAsync(departmentId, ct);
        if (dept == null)
            return NotFound("Department not found");

        if (dept.ManagerId != userId && !User.IsInRole("Admin"))
            return Forbid("Only the department's Manager or an Admin can generate a schedule");

        await _service.GenerateWeeklyScheduleAsync(departmentId, startDate, ct);
        return Ok(new { message = $"Generated schedule for week starting on {startDate}"});
    }


    [HttpPost("{departmentId}/ai-generate")]
    [Authorize(Roles = "manager,admin")]
    public async Task<IActionResult> AI_GenerateWeeklySchedule(string departmentId, DateTime weekStart, CancellationToken ct)
    {
        var startDate = DateOnly.FromDateTime(weekStart.Date);
        if (startDate.DayOfWeek != DayOfWeek.Monday)
            return BadRequest("WeekStart must be Monday");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var dept = await _departmentRepo.GetByIdAsync(departmentId, ct);
        if (dept == null)
            return NotFound("Department not found");

        if (dept.ManagerId != userId && !User.IsInRole("Admin"))
            return Forbid("Only the department's Manager or an Admin can generate a schedule");

        var jsonResponse = await _scheduleGenerationService.GetScheduleSuggestionJsonAsync(departmentId, DateOnly.FromDateTime(weekStart), ct);

        List<ShiftFromAiDto>? shiftDtos;
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new DateOnlyJsonConverter());
            options.Converters.Add(new TimeOnlyJsonConverter());
            options.Converters.Add(new JsonStringEnumConverter());

            shiftDtos = JsonSerializer.Deserialize<List<ShiftFromAiDto>>(jsonResponse, options);
        }
        catch (JsonException ex)
        {
            return BadRequest($"AI did not return valid JSON. {ex.Message}");
        }

        if (shiftDtos == null || !shiftDtos.Any())
        {
            return Ok("AI returned no shifts to schedule.");
        }

        await _service.SaveGeneratedShiftsAsync(departmentId, DateOnly.FromDateTime(weekStart), shiftDtos, ct);

        return Ok("Schedule generated successfully.");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShiftFromAiDto>>> List(
        CancellationToken ct)
    {
        var list = await _service.GetAll(ct);
        return Ok(_mapper.Map<IEnumerable<ShiftFromAiDto>>(list));
    }

    [HttpGet("user/{userId}")]
    [Authorize]
    public async Task<IActionResult> GetUserSchedule(
        [FromRoute] string userId,
        [FromQuery] DateTime weekStart,
        CancellationToken ct)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null)
            return Unauthorized();

        if (!User.IsInRole("Admin") && currentUserId != userId)
        {
            var user = await _userRepo.GetByIdAsync(currentUserId, ct);
            var targetUser = await _userRepo.GetByIdAsync(userId, ct);
            if (user == null || targetUser == null)
                return NotFound();

            if (user.Role != UserRole.Manager || user.DepartmentId != targetUser.DepartmentId)
                return Forbid();
        }

        var startDate = DateOnly.FromDateTime(weekStart.Date);
        if (startDate.DayOfWeek != DayOfWeek.Monday)
            return BadRequest("WeekStart must be Monday");

        var result = await _service.GetUserScheduleAsync(userId, startDate, ct);
        return Ok(result);
    }
}
