var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- START CORS CONFIGURATION ---
// Define a CORS policy name (you can use any name, "AllowSpecificOrigin" is common)
const string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          // IMPORTANT: Replace 'https://your-vercel-frontend-url.vercel.app' with your actual Vercel frontend URL
                          // After deploying your frontend to Vercel, copy its URL and put it here.
                          // For testing, you might use .AllowAnyOrigin() but it's not recommended for production.
                          policy.WithOrigins("https://your-vercel-frontend-url.vercel.app")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});
// --- END CORS CONFIGURATION ---

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection(); // Often removed for HTTP-only hosting on Render free tier, or handled by Render's proxy

// --- START CORS USAGE ---
app.UseCors(MyAllowSpecificOrigins); // Use the CORS policy here, BEFORE UseAuthorization and MapControllers
// --- END CORS USAGE ---

app.UseAuthorization();

app.MapControllers();

app.Run();