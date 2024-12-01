using Alpha_API.Controllers;
using Alpha_API.Services;
using Alpha_API.Utils;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Firebase.Database;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

namespace WebAPI
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			var firebaseBaseUrl = builder.Configuration.GetSection("Firebase:BaseUrl").Value;
			FirebaseApp.Create(new AppOptions()
			{
				Credential = GoogleCredential.FromFile("D:\\Downloads\\Fall24\\SEP490\\SEP490_GSM_API\\Secret\\sgm-management-c98cd-firebase-adminsdk-kc3zt-383493a9bd.json")
			});

			builder.Services.AddControllers();
			builder.Services.AddEndpointsApiExplorer();

			var firebaseClient = new FirebaseClient(firebaseBaseUrl);
			builder.Services.AddSingleton(firebaseClient);
			builder.Services.AddSingleton<EmailService>();
			builder.Services.AddSingleton(FirebaseAuth.DefaultInstance);
			builder.Services.AddSingleton<RoleService>();
			builder.Services.AddScoped<RegisterService>();
			builder.Services.AddScoped<TrainerService>();
			builder.Services.AddScoped<SalaryService>();
			builder.Services.AddScoped<StaffService>();
			builder.Services.AddScoped<ShiftService>();
			builder.Services.AddScoped<GymMembershipCheckService>();
			builder.Services.AddScoped<QrCodeService>();
			builder.Services.AddSingleton<PaymentMethodService>();
			builder.Services.AddSingleton<TimeSlotService>();
			builder.Services.AddScoped<IScheduleService, ScheduleService>();
			builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
			builder.Services.AddScoped<FirebaseClientProvider>(provider =>
				new FirebaseClientProvider(
					provider.GetRequiredService<IHttpContextAccessor>(),
					firebaseBaseUrl
				));
			builder.Services.AddSwaggerGen(options =>
			{
				options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
				{
					Description = "Standard Authorization header using the Bearer scheme. Example: \"bearer {token}\"",
					In = ParameterLocation.Header,
					Name = "Authorization",
					Type = SecuritySchemeType.ApiKey,
					Scheme = "Bearer"
				});
				options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

			var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();


			builder.Services.AddCors(opts =>
			{
				opts.AddPolicy("CORSPolicy", builder => builder.AllowAnyHeader().AllowAnyMethod().AllowCredentials().SetIsOriginAllowed((host) => true));
			});


			builder.Services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(options =>
			{
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					ValidIssuer = builder.Configuration["Jwt:Issuer"],
					ValidAudience = builder.Configuration["Jwt:Audience"],
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
				};
			});



			//builder.Services.AddAuthorization(options =>
			//{
			//	options.AddPolicy("AdminOnly", policy => policy.RequireClaim(ClaimTypes.Role, "admin"));
			//	options.AddPolicy("StaffOnly", policy => policy.RequireClaim(ClaimTypes.Role, "staff"));
			//	options.AddPolicy("PTOnly", policy => policy.RequireClaim(ClaimTypes.Role, "pt"));
			//	options.AddPolicy("CustomerOnly", policy => policy.RequireClaim(ClaimTypes.Role, "customer"));
			//});


			builder.WebHost.UseUrls("http://0.0.0.0:5000");
			builder.Services.AddHttpClient<AuthController>();

			// Add session services
			builder.Services.AddDistributedMemoryCache(); // You can also use other types of caches
			builder.Services.AddSession(options =>
			{
				options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
				options.Cookie.HttpOnly = true; // Make the session cookie accessible only to the server
				options.Cookie.IsEssential = true; // Ensure session cookie is always available
			});

			builder.Services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "GymAPI_OData", Version = "v1" });
			});
			builder.Services.AddHttpContextAccessor();

			var app = builder.Build();

			using (var scope = app.Services.CreateScope())
			{
				var timeSlotService = scope.ServiceProvider.GetRequiredService<TimeSlotService>();
				await timeSlotService.LoadTimeSlotsAsync();
			}

			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseHttpsRedirection();
			app.UseSession();
			app.UseRouting();
			app.UseCors("CORSPolicy");
			app.UseAuthentication();
			app.UseAuthorization();
			app.MapControllers();
			app.UseODataBatching();



			app.Run();
		}
	}
}