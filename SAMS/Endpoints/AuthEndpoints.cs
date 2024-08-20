using Microsoft.AspNetCore.Identity;
using SAMS.Models.Auth;
using SAMS.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using SAMS.Data;

namespace SAMS.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/register", async (UserManager<ApplicationUser> userManager, AMSContext context, RegisterModel model, ILogger<Program> logger) =>
            {
                try
                {
                    if (model.Password != model.ConfirmPassword)
                        return Results.BadRequest("Password and confirmation password do not match.");

                    string userType;
                    if (model.Email.EndsWith("@ucsi.ruh.ac.lk"))
                        userType = "Student";
                    else if (model.Email.EndsWith("@dcs.ruh.ac.lk"))
                        userType = "Lecturer";
                    else
                        return Results.BadRequest("Invalid email domain. Must be either @ucsi.ruh.ac.lk for students or @dcs.ruh.ac.lk for lecturers.");

                    var userExists = await userManager.FindByEmailAsync(model.Email);
                    if (userExists != null)
                        return Results.BadRequest("User with this email already exists!");

                    ApplicationUser user = new()
                    {
                        Email = model.Email,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        UserName = model.Email,
                        Name = model.Name,
                        UserType = userType
                    };
                    using var transaction = await context.Database.BeginTransactionAsync();

                    try { 

                    var result = await userManager.CreateAsync(user, model.Password);
                    if (!result.Succeeded)
                        return Results.BadRequest(result.Errors);

                    await userManager.AddToRoleAsync(user, userType);

                        if (userType == "Student"){
                            var student = new Student
                        {
                            UserID = user.Id,
                            CurrentSemester = 1 // Or any default value
                            };
                         context.Students.Add(student);
                        }
                        else if (userType == "Lecturer")
                                               {
                         var lecturer = new Lecturer
                                                   {
                         UserID = user.Id
                                                       };
                         context.Lecturers.Add(lecturer);
                                               }

                        await context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        logger.LogInformation($"User {user.Email} registered successfully as {userType}");
                    return Results.Ok($"User created successfully as {userType}!");
                }
                    catch (Exception){
                        await transaction.RollbackAsync();
                        throw;
                     }
                    }
                    catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred during user registration");
                    return Results.Problem("An error occurred during registration. Please try again later.", statusCode: 500);
                }
            });

            app.MapPost("/login", async (UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, LoginModel model, ILogger<Program> logger) =>
            {
                try
                {
                    var user = await userManager.FindByEmailAsync(model.Email);
                    if (user == null)
                    {
                        logger.LogWarning($"Login failed: User not found for email {model.Email}");
                        return Results.BadRequest(new { message = "Invalid email or password" });
                    }

                    var result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
                    if (result.Succeeded)
                    {
                        logger.LogInformation($"Login successful for user: {user.Email}");
                        var token = GenerateJwtToken(user, configuration , model.RememberMe);
                        return Results.Ok(new { Token = token, Message = "Login successful", UserType = user.UserType });
                    }
                    else if (result.IsLockedOut)
                    {
                        logger.LogWarning($"Account locked out for user {model.Email}");
                        return Results.BadRequest(new { message = "Account is locked out. Please try again later or contact support." });
                    }
                    else
                    {
                        logger.LogWarning($"Login failed: Invalid password for user {model.Email}");
                        return Results.BadRequest(new { message = "Invalid email or password" });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred during user login");
                    return Results.Problem("An error occurred during login. Please try again later.", statusCode: 500);
                }
            }).RequireCors("AllowReactApp");
        }

        private static string GenerateJwtToken(ApplicationUser user, IConfiguration configuration , bool rememberMe)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.UserType)
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiration = rememberMe ? DateTime.Now.AddDays(30) : DateTime.Now.AddHours(3);

            var token = new JwtSecurityToken(
                issuer: configuration["JWT:ValidIssuer"],
                audience: configuration["JWT:ValidAudience"],
                claims: claims,
                expires: expiration,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
    }
