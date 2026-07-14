using System.ComponentModel.DataAnnotations;

namespace Sm_API.Api.Models;

public class Student
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public DateOnly EnrollmentDate { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
