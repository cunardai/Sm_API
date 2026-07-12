using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sm_API.Api.Data;
using Sm_API.Api.Dtos;
using Sm_API.Api.Models;

namespace Sm_API.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrollmentsController : ControllerBase
{
    private readonly SmApiDbContext _db;

    public EnrollmentsController(SmApiDbContext db)
    {
        _db = db;
    }

    private static EnrollmentReadDto ToDto(Enrollment e) =>
        new(e.Id, e.StudentId, e.ClassRoomId, e.EnrollmentDate);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EnrollmentReadDto>>> GetAll()
    {
        var enrollments = await _db.Enrollments.AsNoTracking().ToListAsync();
        return Ok(enrollments.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EnrollmentReadDto>> GetById(int id)
    {
        var enrollment = await _db.Enrollments.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (enrollment is null) return NotFound();
        return Ok(ToDto(enrollment));
    }

    [HttpPost]
    public async Task<ActionResult<EnrollmentReadDto>> Create(EnrollmentWriteDto dto)
    {
        var studentExists = await _db.Students.AnyAsync(s => s.Id == dto.StudentId);
        if (!studentExists)
        {
            return BadRequest(new ProblemDetails { Title = $"Student {dto.StudentId} does not exist." });
        }

        var classRoomExists = await _db.ClassRooms.AnyAsync(c => c.Id == dto.ClassRoomId);
        if (!classRoomExists)
        {
            return BadRequest(new ProblemDetails { Title = $"ClassRoom {dto.ClassRoomId} does not exist." });
        }

        var enrollment = new Enrollment
        {
            StudentId = dto.StudentId,
            ClassRoomId = dto.ClassRoomId,
            EnrollmentDate = dto.EnrollmentDate
        };

        _db.Enrollments.Add(enrollment);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new ProblemDetails { Title = "This student is already enrolled in this class." });
        }

        return CreatedAtAction(nameof(GetById), new { id = enrollment.Id }, ToDto(enrollment));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, EnrollmentWriteDto dto)
    {
        var enrollment = await _db.Enrollments.FindAsync(id);
        if (enrollment is null) return NotFound();

        enrollment.StudentId = dto.StudentId;
        enrollment.ClassRoomId = dto.ClassRoomId;
        enrollment.EnrollmentDate = dto.EnrollmentDate;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new ProblemDetails { Title = "This student is already enrolled in this class." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var enrollment = await _db.Enrollments.FindAsync(id);
        if (enrollment is null) return NotFound();

        _db.Enrollments.Remove(enrollment);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
