using Aniyuu.DbContext;
using Aniyuu.Exceptions;
using Aniyuu.Helpers;
using Aniyuu.Interfaces.UserInterfaces;
using Aniyuu.Models.UserModels;
using Aniyuu.Utils;
using MongoDB.Driver;
using NLog;

namespace Aniyuu.Services.UserServices;

public class UserService(IMongoDbContext mongoDbContext) : IUserService
{
    private readonly IMongoCollection<UserModel> _userCollection = mongoDbContext
        .GetCollection<UserModel>(AppSettingConfig.Configuration["MongoDBSettings:UserCollection"]!);
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public async Task<UserModel> Get(string userId, CancellationToken cancellationToken)
    {
        var user = await _userCollection.Find(x => x.Id == userId)
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null)
        {
            Logger.Error($"[UserService.Get] User not found. UserId: {userId}");
            throw new AppException("User not found",404);
        }
        return user;
    }

    public async Task<string> GetEmail(string username, CancellationToken cancellationToken)
    {
        var filter = Builders<UserModel>.Filter.Eq(x => x.Username, username);
        var email = await _userCollection.Find(filter)
            .Project(x => x.Email).FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrEmpty(email))
        {
            Logger.Error($"[UserService.GetEmail] User not found. Username: {username}");
            throw new AppException("User not found", 404);
        }
        return email;
    }

    public async Task<UserModel> Update(UserModel updatedUserModel, CancellationToken cancellationToken)
    {
        var existedUserModel = await Get(updatedUserModel.Id, cancellationToken);
        updatedUserModel.UpdatedAt = DateTime.UtcNow;
        updatedUserModel =  UpdateCheckHelper.ReplaceNullToOldValues(existedUserModel,updatedUserModel);
        try
        {
            var result = await _userCollection.ReplaceOneAsync(u => u.Id == updatedUserModel.Id, updatedUserModel,
                cancellationToken: cancellationToken);
            if (result.ModifiedCount == 0)
            {
                Logger.Error($"[UserService.Update] Update failed. (modifiedCount: {result.ModifiedCount})");
                throw new AppException("Update failed", 500);
            }
        }
        catch (Exception e)
        {
            Logger.Error($"[UserService.Update] Error updating user. Problem is: {e.Message}");
            throw new AppException("Update failed", 500);
        }
        return updatedUserModel;
    }

    public async Task<bool> Delete(string userId, CancellationToken cancellationToken)
    {
        var user = await Get(userId, cancellationToken);
        user.IsDeleted = true;
        var deletedUser = await Update(user, cancellationToken);
        if (user.IsDeleted == deletedUser.IsDeleted)
        {
            Logger.Error($"[UserService.Delete] User ({userId}){user.Username} is not deleted");
            throw new AppException("User is not deleted", 500);
        }
        return true;
    }

    public async Task<bool> ChangePassword(string userId, string oldPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await Get(userId, cancellationToken);

        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.HashedPassword))
        {
            Logger.Error($"[UserService.ChangePassword] Incorrect current password for UserId: {userId}");
            throw new AppException("password is wrong", 401);
        }

        user.HashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword, 11);

        await Update(user, cancellationToken);
        return true;
    }
}