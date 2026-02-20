using Microsoft.EntityFrameworkCore;
using System;

namespace WindowDemo
{
    internal static partial class ShopService
    {
        private static Func<AppDbContext>? _dbFactory;

        private const int DbCommandTimeoutSeconds = 5;

        public static void SetDbFactory(Func<AppDbContext> factory)
        {
            _dbFactory = factory;
        }

        private static bool TryCreateDb(out AppDbContext db)
        {
            db = null!;
            try
            {
                if (_dbFactory == null)
                    return false;

                db = _dbFactory();
                try { db.Database.SetCommandTimeout(DbCommandTimeoutSeconds); } catch { }
                return true;
            }
            catch
            {
                try { db?.Dispose(); } catch { }
                return false;
            }
        }

        private static bool TryDbRead<T>(Func<AppDbContext, T> read, out T value)
        {
            value = default!;
            try
            {
                if (!TryCreateDb(out var db)) return false;
                using (db)
                {
                    value = read(db);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool TryDbWrite(Action<AppDbContext> write)
        {
            try
            {
                if (!TryCreateDb(out var db)) return false;
                using (db)
                {
                    write(db);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
