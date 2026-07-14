using System.ComponentModel.DataAnnotations;

namespace Sm_API.Api.Dtos;

public record ClassRoomReadDto(int Id, string Name, int GradeLevel, string RoomNumber, int TeacherId);

public class ClassRoomWriteDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 12)]
    public int GradeLevel { get; set; }

    [Required, MaxLength(20)]
    public string RoomNumber { get; set; } = string.Empty;

    [Required]
    public int TeacherId { get; set; }
}
