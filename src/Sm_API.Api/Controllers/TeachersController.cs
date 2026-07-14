using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sm_API.Api.Data;
using Sm_API.Api.Dtos;
using Sm_API.Api.Models;

namespace Sm_API.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeachersController : ControllerBase
{
    private readonly SmApiDbContext _db;

    public TeachersController(SmApiDbContext db)
    {
        _db = db;
    }

    private static TeacherReadDto ToDto(Teacher t) =>
        new(t.Id, t.FirstName, t.LastName, t.Email, t.Subject, t.HireDate);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TeacherReadDto>>> GetAll()
    {
        var teachers = await _db.Teachers.AsNoTracking().ToListAsync();
        return Ok(teachers.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TeacherReadDto>> GetById(int id)
    {
        var teacher = await _db.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (teacher is null) return NotFound();
        return Ok(ToDto(teacher));
    }

    [HttpPost]
    public async Task<ActionResult<TeacherReadDto>> Create(TeacherWriteDto dto)
    {
        var teacher = new Teacher
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Subject = dto.Subject,
            HireDate = dto.HireDate
        };

        _db.Teachers.Add(teacher);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new ProblemDetails { Title = "A teacher with this email already exists." });
        }

        return CreatedAtAction(nameof(GetById), new { id = teacher.Id }, ToDto(teacher));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TeacherWriteDto dto)
    {
        var teacher = await _db.Teachers.FindAsync(id);
        if (teacher is null) return NotFound();

        teacher.FirstName = dto.FirstName;
        teacher.LastName = dto.LastName;
        teacher.Email = dto.Email;
        teacher.Subject = dto.Subject;
        teacher.HireDate = dto.HireDate;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new ProblemDetails { Title = "A teacher with this email already exists." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var teacher = await _db.Teachers.FindAsync(id);
        if (teacher is null) return NotFound();

        var hasClasses = await _db.ClassRooms.AnyAsync(c => c.TeacherId == id);
        if (hasClasses)
        {
            return Conflict(new ProblemDetails { Title = "Cannot delete a teacher who is still assigned to a class." });
        }

        _db.Teachers.Remove(teacher);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
