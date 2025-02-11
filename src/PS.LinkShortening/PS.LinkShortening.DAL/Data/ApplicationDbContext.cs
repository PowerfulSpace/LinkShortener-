﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PS.LinkShortening.Domain.Entities;

namespace PS.LinkShortening.DAL.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }


        public DbSet<Link> Links { get; set; } = null!;
        public DbSet<AuthTokenItem> AuthTokenItems { get; set; } = null!;

    }

}
