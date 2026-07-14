using System.ComponentModel.DataAnnotations;

namespace Sm_API.Api.Dtos;

public record TeacherReadDto(int Id, string FirstName, string LastName, string Email, string Subject, DateOnly HireDate);

public class TeacherWriteDto
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Subject { get; set; } = string.Empty;

    public DateOnly HireDate { get; set; }
}
