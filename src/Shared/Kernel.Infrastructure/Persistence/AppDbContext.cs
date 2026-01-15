﻿using Microsoft.EntityFrameworkCore;
using Product.Template.Core.Identity.Domain.Entities;

namespace Product.Template.Kernel.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    // Identity Tables
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations from all assemblies
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
