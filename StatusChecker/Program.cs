using Npgsql;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var connString = "Host=10.21.61.51;Port=5432;Database=convoydb;Username=postgres;Password=GarantDockerPass";
        
        try
        {
            using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            Console.WriteLine("✓ Connected to database");

            using var cmd = new NpgsqlCommand(@"
                SELECT 
                    (SELECT COUNT(*) FROM locations) as loc_count,
                    (SELECT COUNT(*) FROM locations WHERE distance_from_previous IS NOT NULL) as loc_with_dist_count,
                    (SELECT COUNT(*) FROM daily_distance_reports) as report_count
                ", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                Console.WriteLine($"Total Locations: {reader.GetInt64(0)}");
                Console.WriteLine($"Locations with Distance: {reader.GetInt64(1)}");
                Console.WriteLine($"Daily Distance Reports: {reader.GetInt64(2)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Error: {ex.Message}");
        }
    }
}
