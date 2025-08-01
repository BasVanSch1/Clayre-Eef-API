
using CEApi.Data;
using Microsoft.EntityFrameworkCore;

namespace CEApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

            builder.Services.AddDbContext<MsSqlDatabaseContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("PricesLocal"));
            });

            builder.Services.AddControllers().AddNewtonsoftJson();
            builder.Services.AddOpenApi();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
                {
                    policy.WithOrigins(
                        "http://localhost:5173",
                        "https://prijzencheck.clayre-eef.com")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            //app.UseHttpsRedirection(); // the api is for internal use only at the moment, so I won't bother with enforcing HTTPS
            app.UseCors(MyAllowSpecificOrigins);
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
