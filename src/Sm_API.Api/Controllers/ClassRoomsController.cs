using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sm_API.Api.Data;
using Sm_API.Api.Dtos;
using Sm_API.Api.Models;

namespace Sm_API.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClassRoomsController : ControllerBase
{
    private readonly SmApiDbContext _db;

    public ClassRoomsController(SmApiDbContext db)
    {
        _db = db;
    }

    private static ClassRoomReadDto ToDto(ClassRoom c) =>
        new(c.Id, c.Name, c.GradeLevel, c.RoomNumber, c.TeacherId);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClassRoomReadDto>>> GetAll()
    {
        var classRooms = await _db.ClassRooms.AsNoTracking().ToListAsync();
        return Ok(classRooms.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClassRoomReadDto>> GetById(int id)
    {
        var classRoom = await _db.ClassRooms.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (classRoom is null) return NotFound();
        return Ok(ToDto(classRoom));
    }

    [HttpPost]
    public async Task<ActionResult<ClassRoomReadDto>> Create(ClassRoomWriteDto dto)
    {
        var teacherExists = await _db.Teachers.AnyAsync(t => t.Id == dto.TeacherId);
        if (!teacherExists)
        {
            return BadRequest(new ProblemDetails { Title = $"Teacher {dto.TeacherId} does not exist." });
        }

        var classRoom = new ClassRoom
        {
            Name = dto.Name,
            GradeLevel = dto.GradeLevel,
            RoomNumber = dto.RoomNumber,
            TeacherId = dto.TeacherId
        };

        _db.ClassRooms.Add(classRoom);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = classRoom.Id }, ToDto(classRoom));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ClassRoomWriteDto dto)
    {
        var classRoom = await _db.ClassRooms.FindAsync(id);
        if (classRoom is null) return NotFound();

        var teacherExists = await _db.Teachers.AnyAsync(t => t.Id == dto.TeacherId);
        if (!teacherExists)
        {
            return BadRequest(new ProblemDetails { Title = $"Teacher {dto.TeacherId} does not exist." });
        }

        classRoom.Name = dto.Name;
        classRoom.GradeLevel = dto.GradeLevel;
        classRoom.RoomNumber = dto.RoomNumber;
        classRoom.TeacherId = dto.TeacherId;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var classRoom = await _db.ClassRooms.FindAsync(id);
        if (classRoom is null) return NotFound();

        _db.ClassRooms.Remove(classRoom);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
