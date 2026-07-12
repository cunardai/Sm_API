using System.Net;
using System.Net.Http.Json;
using Sm_API.Api.Dtos;
using Xunit;

namespace Sm_API.Tests;

public class SchoolWorkflowTests : IClassFixture<SmApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SchoolWorkflowTests(SmApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullWorkflow_TeacherClassRoomStudentEnrollment_Succeeds()
    {
        var teacherDto = new TeacherWriteDto
        {
            FirstName = "Grace",
            LastName = "Hopper",
            Email = $"grace.{Guid.NewGuid()}@school.test",
            Subject = "Computer Science",
            HireDate = new DateOnly(2020, 1, 15)
        };
        var teacherResponse = await _client.PostAsJsonAsync("/api/teachers", teacherDto);
        Assert.Equal(HttpStatusCode.Created, teacherResponse.StatusCode);
        var teacher = await teacherResponse.Content.ReadFromJsonAsync<TeacherReadDto>();

        var classRoomDto = new ClassRoomWriteDto
        {
            Name = "Intro to Programming",
            GradeLevel = 9,
            RoomNumber = "B12",
            TeacherId = teacher!.Id
        };
        var classRoomResponse = await _client.PostAsJsonAsync("/api/classrooms", classRoomDto);
        Assert.Equal(HttpStatusCode.Created, classRoomResponse.StatusCode);
        var classRoom = await classRoomResponse.Content.ReadFromJsonAsync<ClassRoomReadDto>();

        var studentDto = new StudentWriteDto
        {
            FirstName = "Alan",
            LastName = "Turing",
            Email = $"alan.{Guid.NewGuid()}@school.test",
            DateOfBirth = new DateOnly(2011, 6, 23),
            EnrollmentDate = new DateOnly(2024, 9, 1)
        };
        var studentResponse = await _client.PostAsJsonAsync("/api/students", studentDto);
        var student = await studentResponse.Content.ReadFromJsonAsync<StudentReadDto>();

        var enrollmentDto = new EnrollmentWriteDto
        {
            StudentId = student!.Id,
            ClassRoomId = classRoom!.Id,
            EnrollmentDate = new DateOnly(2024, 9, 1)
        };
        var enrollmentResponse = await _client.PostAsJsonAsync("/api/enrollments", enrollmentDto);
        Assert.Equal(HttpStatusCode.Created, enrollmentResponse.StatusCode);

        var duplicateEnrollment = await _client.PostAsJsonAsync("/api/enrollments", enrollmentDto);
        Assert.Equal(HttpStatusCode.Conflict, duplicateEnrollment.StatusCode);
    }

    [Fact]
    public async Task CreateClassRoom_UnknownTeacher_ReturnsBadRequest()
    {
        var classRoomDto = new ClassRoomWriteDto
        {
            Name = "Ghost Class",
            GradeLevel = 5,
            RoomNumber = "X1",
            TeacherId = 999999
        };

        var response = await _client.PostAsJsonAsync("/api/classrooms", classRoomDto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTeacher_AssignedToClass_ReturnsConflict()
    {
        var teacherDto = new TeacherWriteDto
        {
            FirstName = "Marie",
            LastName = "Curie",
            Email = $"marie.{Guid.NewGuid()}@school.test",
            Subject = "Chemistry",
            HireDate = new DateOnly(2019, 8, 20)
        };
        var teacherResponse = await _client.PostAsJsonAsync("/api/teachers", teacherDto);
        var teacher = await teacherResponse.Content.ReadFromJsonAsync<TeacherReadDto>();

        var classRoomDto = new ClassRoomWriteDto
        {
            Name = "Chemistry 101",
            GradeLevel = 10,
            RoomNumber = "C3",
            TeacherId = teacher!.Id
        };
        await _client.PostAsJsonAsync("/api/classrooms", classRoomDto);

        var deleteResponse = await _client.DeleteAsync($"/api/teachers/{teacher.Id}");
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }
}
