
using EntityLayer.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Concrete
{
    public class Context : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("server=DESKTOP-T2GF1F3\\SQLEXPRESS;database=TahminOyunuDB; " +
                "integrated security=true;TrustServerCertificate=true");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Admin konfigürasyonu
            modelBuilder.Entity<Admin>()
                .HasIndex(a => a.Username)
                .IsUnique();

            // Category konfigürasyonu
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            // Media konfigürasyonu
            modelBuilder.Entity<Media>()
                .HasOne(m => m.Category)
                .WithMany(c => c.Medias)
                .HasForeignKey(m => m.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Kategori silinirse bağlı medyalar silinmez

            // MediaImage konfigürasyonu
            modelBuilder.Entity<MediaImage>()
                .HasOne(mi => mi.Media)
                .WithMany(m => m.MediaImages)
                .HasForeignKey(mi => mi.MediaId)
                .OnDelete(DeleteBehavior.Cascade); // Media silinirse görselleri de silinir

            // Her film için her sıra numarası benzersiz olmalı
            modelBuilder.Entity<MediaImage>()
                .HasIndex(mi => new { mi.MediaId, mi.OrderNo })
                .IsUnique();
        }

        public DbSet<Admin> Admins { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Media> Medias { get; set; }
        public DbSet<MediaImage> MediaImages { get; set; }
    }
}
