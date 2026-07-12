using System.ComponentModel.DataAnnotations;

namespace Sm_API.Api.Models;

public class Teacher
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Subject { get; set; } = string.Empty;

    public DateOnly HireDate { get; set; }

    public ICollection<ClassRoom> ClassRooms { get; set; } = new List<ClassRoom>();
}
