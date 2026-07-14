using System.ComponentModel.DataAnnotations;

namespace Sm_API.Api.Dtos;

public record EnrollmentReadDto(int Id, int StudentId, int ClassRoomId, DateOnly EnrollmentDate);

public class EnrollmentWriteDto
{
    [Required]
    public int StudentId { get; set; }

    [Required]
    public int ClassRoomId { get; set; }

    public DateOnly EnrollmentDate { get; set; }
}
