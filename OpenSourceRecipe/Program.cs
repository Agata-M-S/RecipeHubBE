using System.Reflection;
using System.Text;
using FluentMigrator.Runner;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using OpenSourceRecipes.Services;
using OpenSourceRecipes.Seeds;
using OpenSourceRecipe.Controllers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo{Title = "Open Source Recipes", Version = "v1"});

    // add JWT Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
       Description = "JWT Authorization header using the Bearer scheme.",
       Name = "Authorization",
       In = ParameterLocation.Header,
       Type = SecuritySchemeType.ApiKey,
       Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// get environment variable for connection string

string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

string connectionStringName = "";

if(builder.Environment.IsDevelopment())
{
    connectionStringName = "DefaultConnection";
}
else if(builder.Environment.IsProduction())
{
    connectionStringName = "ProductionConnection";
}
else if(env == "Testing")
{
    connectionStringName = "TestConnection";
}

var connectionString = builder.Configuration.GetConnectionString(connectionStringName);

/*if (env == "Testing")
{
    using var connection = new NpgsqlConnection(connectionString);
    connection.Open();

    var deleteScript = @"
    DO
    $do$
    DECLARE
        r RECORD;
    BEGIN
        FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = current_schema() AND tablename <> 'VersionInfo') LOOP
            EXECUTE 'DELETE FROM ' || quote_ident(r.tablename);
        END LOOP;
    END
    $do$;";

    using var cmd = new NpgsqlCommand(deleteScript, connection);
    cmd.ExecuteNonQuery();

    connection.Close();
}*/

// add common FluentMigrator services
builder.Services
    .AddFluentMigratorCore()
    .ConfigureRunner(rb => rb

    // Add PostgreSQL support to FluentMigrator
    .AddPostgres()

    // Set the connection string
    .WithGlobalConnectionString(connectionString)

    // Define the assembly containing the migrations
    // Assembly is defined in the project file (.csproj)
    .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations())

    // Enable logging to console in the FluentMigrator way
    .AddLogging(lb => lb.AddFluentMigratorConsole());

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Validate the JWT Issuer (iss) claim
        ValidateIssuer = true,
        // Validate the JWT Audience (aud) claim
        ValidateAudience = true,
        // Validate the token expiry
        ValidateLifetime = true,
        // Validate the JWT Issuer (iss) claim
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"], // this comes from appsettings.json
        ValidAudience = builder.Configuration["Jwt:Audience"], // this comes from appsettings.json

        // The signing key must match the one used to generate the token
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? string.Empty))
    };
});

// Repositories
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<RecipeRepository>();
builder.Services.AddScoped<IngredientRepository>();
builder.Services.AddScoped<CuisineRepository>();
builder.Services.AddScoped<CommentRepository>();
builder.Services.AddScoped<RecipeRatingsRepository>();
builder.Services.AddScoped<CommentVotesRepository>();

// Controllers
builder.Services.AddControllers();

var app = builder.Build();

if (env == "Development" || env == "Production")
{
    app.UseSwagger();
    app.UseSwaggerUI();

}

if (env == "Testing" || env == "Development")
{

    using (var scope = app.Services.CreateScope())
    {
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        await Task.Run(() =>
        {
            runner.MigrateDown(0);
        });

        await Task.Run(() =>
        {
            runner.MigrateUp();
        });
    }

    var userseed = new SeedUserData(builder.Configuration);
    var ingredientSeed = new SeedFoodData(builder.Configuration);
    var cuisineSeed = new SeedCuisineData(builder.Configuration);
    var recipeSeed = new SeedRecipeData(builder.Configuration);
    var commentSeed = new SeedCommentsData(builder.Configuration);
    var ratingSeed = new SeedRecipeRatingData(builder.Configuration);
    var commentVoteSeed = new SeedCommentVotesData(builder.Configuration);

    await userseed.InsertIntoUser(connectionStringName);
    await ingredientSeed.InsertIntoFood(connectionStringName);
    await cuisineSeed.InsertIntoCuisine(connectionStringName);
    await recipeSeed.InsertIntoRecipe(connectionStringName);
    await commentSeed.InsertIntoComments(connectionStringName);
    await ratingSeed.InsertIntoRatings(connectionStringName);
    await commentVoteSeed.InsertIntoCommentVotes(connectionStringName);
}

if (env == "Production")
{
    using (var scope = app.Services.CreateScope())
    {
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
    }
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
