using Aniyuu.DbContext;
using Aniyuu.Exceptions;
using Aniyuu.Interfaces.AdminServices;
using Aniyuu.Interfaces.UserInterfaces;
using Aniyuu.Models.UserModels;
using Aniyuu.Utils;
using MongoDB.Driver;
using NLog;

namespace Aniyuu.Services.AdminServices;

public class AdminUserService(IMongoDbContext mongoDbContext,
    ICurrentUserService currentUserService) : IAdminUserService
{
    private readonly IMongoCollection<UserModel> _userCollection = mongoDbContext
        .GetCollection<UserModel>(AppSettingConfig.Configuration["MongoDBSettings:UserCollection"]!);
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    
    public async Task<List<UserModel>> GetAllUsers(CancellationToken cancellationToken)
    {
        var users = await _userCollection
            .Find(x => x.IsActive == true)
            .ToListAsync(cancellationToken: cancellationToken);
        if (users.Count == 0 || users == null)
        {
            Logger.Error("[AdminUserService.GetAllUsers] No users found");
            throw new AppException("No users found",404);
        }

        return users;
    }

    public async Task<UserModel> GetUser(string username, CancellationToken cancellationToken)
    {
        if (username == null) throw new AppException("Username is cannot be empty", 409);
        
        
        var user = await _userCollection
            .Find(x => x.IsActive == true && x.Username == username)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
        if (user == null)
        {
            Logger.Error("[AdminUserService.GetUser] No user found");
            throw new AppException("No user found", 404);
        }
        return user;
    }

    public async Task<bool> SetUserAsAdmin(string username, CancellationToken cancellationToken)
    {
        var updatedUser = await UpdateUser(username, "admin", cancellationToken);
        return updatedUser.Roles.Contains("admin");
    }

    public async Task<bool> DeleteUser(string username, CancellationToken cancellationToken)
    {
        var updatedUser = await UpdateUser(username, "delete", cancellationToken);
        return updatedUser is { IsActive: false, IsDeleted: true };
    }

    private async Task<UserModel> UpdateUser(string username, string updateType, CancellationToken cancellationToken)
    {
        var user = await GetUser(username, cancellationToken);
        
        var filter = Builders<UserModel>.Filter.And(
            Builders<UserModel>.Filter.Eq(x => x.Id, user.Id),
            Builders<UserModel>.Filter.Eq(x => x.IsActive, true));
        var update = updateType switch
        {
            "admin" => Builders<UserModel>.Update
                .Push(x => x.Roles, "admin")
                .Set(x => x.UpdatedBy, currentUserService.GetUsername())
                .Set(x => x.UpdatedAt, DateTime.UtcNow),
            "delete" => Builders<UserModel>.Update
                .Set(x => x.IsActive, false)
                .Set(x => x.IsDeleted, true)
                .Set(x => x.UpdatedBy, currentUserService.GetUsername())
                .Set(x => x.UpdatedAt, DateTime.UtcNow),
            _ => throw new AppException("Invalid update type", 404)
        };
        try
        {
            var result = await _userCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            if (!result.IsAcknowledged || result.ModifiedCount == 0)
            {
                throw new AppException("user not updated", 502);
            }
        }
        catch (Exception e)
        {
            Logger.Error($"[AdminUserService.UpdateUser] Error updating user: {e.Message}");
            throw new AppException("An error occured while updating user", 500);
        }
        return await GetUser(username, cancellationToken);
    }
}