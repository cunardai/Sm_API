using System.Net;
using System.Net.Http.Json;
using Sm_API.Api.Dtos;
using Xunit;

namespace Sm_API.Tests;

public class StudentsControllerTests : IClassFixture<SmApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public StudentsControllerTests(SmApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static StudentWriteDto NewStudent(string email) => new()
    {
        FirstName = "Ada",
        LastName = "Lovelace",
        Email = email,
        DateOfBirth = new DateOnly(2012, 5, 1),
        EnrollmentDate = new DateOnly(2024, 9, 1)
    };

    [Fact]
    public async Task Create_Then_Get_ReturnsStudent()
    {
        var dto = NewStudent($"ada.{Guid.NewGuid()}@school.test");

        var createResponse = await _client.PostAsJsonAsync("/api/students", dto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<StudentReadDto>();
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/students/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<StudentReadDto>();
        Assert.Equal(dto.Email, fetched!.Email);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/students/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateEmail_ReturnsConflict()
    {
        var email = $"dup.{Guid.NewGuid()}@school.test";
        var dto = NewStudent(email);

        var first = await _client.PostAsJsonAsync("/api/students", dto);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/students", dto);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidEmail_ReturnsBadRequest()
    {
        var dto = NewStudent("not-an-email");

        var response = await _client.PostAsJsonAsync("/api/students", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ExistingStudent_PersistsChanges()
    {
        var dto = NewStudent($"update.{Guid.NewGuid()}@school.test");
        var createResponse = await _client.PostAsJsonAsync("/api/students", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<StudentReadDto>();

        dto.LastName = "Byron";
        var updateResponse = await _client.PutAsJsonAsync($"/api/students/{created!.Id}", dto);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/students/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<StudentReadDto>();
        Assert.Equal("Byron", fetched!.LastName);
    }

    [Fact]
    public async Task Delete_ExistingStudent_RemovesIt()
    {
        var dto = NewStudent($"delete.{Guid.NewGuid()}@school.test");
        var createResponse = await _client.PostAsJsonAsync("/api/students", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<StudentReadDto>();

        var deleteResponse = await _client.DeleteAsync($"/api/students/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/students/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
