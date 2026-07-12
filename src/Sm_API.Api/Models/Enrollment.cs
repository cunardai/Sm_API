namespace Sm_API.Api.Models;

public class Enrollment
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public int ClassRoomId { get; set; }
    public ClassRoom? ClassRoom { get; set; }

    public DateOnly EnrollmentDate { get; set; }
}
