using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using ScrumPoker.Data.Database;
using ScrumPoker.Data.Models;

namespace ScrumPoker.Data.Services;

public class SessionService(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor) : ISessionService
{
    public async Task<Session> CreateSessionAsync(string creatorId, string sessionName)
    {
        try
        {
            var newSession = new Session
            {
                Id = ObjectId.GenerateNewId(),
                DisplayName = sessionName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Participants = [],
                CreatorId = GetOrCreateUserId(),
            };
            dbContext.Sessions.Add(newSession);
            await dbContext.SaveChangesAsync();
            return newSession;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private string GetOrCreateUserId()
    {
        var context = httpContextAccessor.HttpContext ??
            throw new InvalidOperationException("HttpContext is not available.");
        if (context.Request.Cookies.TryGetValue("UserId", out var userId))
            return userId;

        userId = Guid.NewGuid().ToString();
        //context.Response.Cookies.Append("UserId", userId, new CookieOptions
        //{
        //    HttpOnly = true,
        //    SameSite = SameSiteMode.Lax,
        //    Expires = DateTime.Now.AddHours(1),
        //    Secure = true
        //});

        return userId ;
    }
}
