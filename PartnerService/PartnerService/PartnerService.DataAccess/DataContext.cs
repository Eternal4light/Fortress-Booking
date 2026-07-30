using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PartnerService.Core.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection.Emit;
using System.Text;


namespace PartnerService.DataAccess.Repositories
{
    public class DataContext
        : DbContext
    {

        public DbSet<Partner> Partners { get; set; }


        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Partner>(entity =>
            {
                entity.Property(x => x.Name).HasMaxLength(250).IsRequired();

            });



            base.OnModelCreating(modelBuilder);
        }
    }
}