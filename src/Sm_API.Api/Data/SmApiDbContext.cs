using Microsoft.EntityFrameworkCore;
using Sm_API.Api.Models;

namespace Sm_API.Api.Data;

public class SmApiDbContext : DbContext
{
    public SmApiDbContext(DbContextOptions<SmApiDbContext> options) : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<ClassRoom> ClassRooms => Set<ClassRoom>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>()
            .HasIndex(s => s.Email)
            .IsUnique();

        modelBuilder.Entity<Teacher>()
            .HasIndex(t => t.Email)
            .IsUnique();

        modelBuilder.Entity<ClassRoom>()
            .HasOne(c => c.Teacher)
            .WithMany(t => t.ClassRooms)
            .HasForeignKey(c => c.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.ClassRoom)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.ClassRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Enrollment>()
            .HasIndex(e => new { e.StudentId, e.ClassRoomId })
            .IsUnique();
    }
}
