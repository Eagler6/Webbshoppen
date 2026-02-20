using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WindowDemo
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(
                    "Server=tcp:edinsdb.database.windows.net,1433;Initial Catalog=EdinsDB;Persist Security Info=False;User ID=edin;Password=PratesOliPoa479;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;")
                .Options;

            return new AppDbContext(options);
        }
    }
}
