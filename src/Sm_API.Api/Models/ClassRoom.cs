using System.ComponentModel.DataAnnotations;

namespace Sm_API.Api.Models;

public class ClassRoom
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 12)]
    public int GradeLevel { get; set; }

    [Required, MaxLength(20)]
    public string RoomNumber { get; set; } = string.Empty;

    public int TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
