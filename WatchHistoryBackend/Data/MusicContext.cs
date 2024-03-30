using Microsoft.EntityFrameworkCore;
using WatchHistoryBackend.Models;

namespace WatchHistoryBackend.Data
{
    public class MusicContext : DbContext
    {
        public DbSet<Song> Songs { get; set; }
        public DbSet<ListenedSong> ListenedSongs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=WatchHistory;Integrated Security=True;Trust Server Certificate=true;Trusted_Connection=True;");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ListenedSong>()
                .HasOne(x => x.Song)
                .WithMany(x => x.Listens)
                .HasForeignKey(x => x.SongId);
        }
    }
}
