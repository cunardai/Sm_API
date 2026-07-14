using System.ComponentModel.DataAnnotations;

namespace Sm_API.Api.Dtos;

public record StudentReadDto(int Id, string FirstName, string LastName, string Email, DateOnly DateOfBirth, DateOnly EnrollmentDate);

public class StudentWriteDto
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public DateOnly EnrollmentDate { get; set; }
}
