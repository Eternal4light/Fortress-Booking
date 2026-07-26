using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using PartnerService.Core.Domain;


namespace PartnerService.DataAccess
{
    public class DataContext
        : DbContext
    {

        public DbSet<Partner> Partners { get; set; }

        public DataContext()
        {

        }

        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }
    }
}
