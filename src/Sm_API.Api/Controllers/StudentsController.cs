using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sm_API.Api.Data;
using Sm_API.Api.Dtos;
using Sm_API.Api.Models;

namespace Sm_API.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly SmApiDbContext _db;

    public StudentsController(SmApiDbContext db)
    {
        _db = db;
    }

    private static StudentReadDto ToDto(Student s) =>
        new(s.Id, s.FirstName, s.LastName, s.Email, s.DateOfBirth, s.EnrollmentDate);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudentReadDto>>> GetAll()
    {
        var students = await _db.Students.AsNoTracking().ToListAsync();
        return Ok(students.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StudentReadDto>> GetById(int id)
    {
        var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (student is null) return NotFound();
        return Ok(ToDto(student));
    }

    [HttpPost]
    public async Task<ActionResult<StudentReadDto>> Create(StudentWriteDto dto)
    {
        var student = new Student
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            DateOfBirth = dto.DateOfBirth,
            EnrollmentDate = dto.EnrollmentDate
        };

        _db.Students.Add(student);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new ProblemDetails { Title = "A student with this email already exists." });
        }

        return CreatedAtAction(nameof(GetById), new { id = student.Id }, ToDto(student));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, StudentWriteDto dto)
    {
        var student = await _db.Students.FindAsync(id);
        if (student is null) return NotFound();

        student.FirstName = dto.FirstName;
        student.LastName = dto.LastName;
        student.Email = dto.Email;
        student.DateOfBirth = dto.DateOfBirth;
        student.EnrollmentDate = dto.EnrollmentDate;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new ProblemDetails { Title = "A student with this email already exists." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var student = await _db.Students.FindAsync(id);
        if (student is null) return NotFound();

        _db.Students.Remove(student);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
