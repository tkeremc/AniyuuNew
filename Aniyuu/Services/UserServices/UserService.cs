using Aniyuu.DbContext;
using Aniyuu.Exceptions;
using Aniyuu.Helpers;
using Aniyuu.Interfaces.UserInterfaces;
using Aniyuu.Models.UserModels;
using Aniyuu.Utils;
using Azure.Storage.Blobs;
using MongoDB.Driver;
using NLog;

namespace Aniyuu.Services.UserServices;

public class UserService(IMongoDbContext mongoDbContext,
    ICurrentUserService currentUserService,
    BlobServiceClient blobServiceClient) : IUserService
{
    private readonly IMongoCollection<UserModel> _userCollection = mongoDbContext
        .GetCollection<UserModel>(AppSettingConfig.Configuration["MongoDBSettings:UserCollection"]!);
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public async Task<UserModel> Get(CancellationToken cancellationToken)
    {
        var user = await _userCollection.Find(x => x.Id == currentUserService.GetUserId())
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null || user.IsDeleted == true)
        {
            Logger.Error($"[UserService.Get] User not found. UserId: {currentUserService.GetUserId()}");
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

    public async Task<bool> CheckUsername(string username, CancellationToken cancellationToken)
    {
        return await _userCollection.Find(x => x.Username == username).AnyAsync(cancellationToken);
    }

    public async Task<UserModel> Update(UserModel updatedUserModel, CancellationToken cancellationToken,
        string updatedBy = "system")
    {
        var existedUserModel = await Get(cancellationToken);
        updatedUserModel.UpdatedAt = DateTime.UtcNow;
        updatedUserModel.UpdatedBy = updatedBy;
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

    public async Task<string> UpdateAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        
        if (file == null || file.Length == 0)
        {
            Logger.Error($"[UserService.UpdateAvatar] File is null or empty. UserId: {currentUserService.GetUserId()}");
            throw new AppException("Avatar file is empty");
        }
        
        var currentUser = await Get(cancellationToken);
        var allowedExtensions = new[] { ".jpg", ".png", ".jpeg" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var container = blobServiceClient.GetBlobContainerClient("profilephotos");
        await container.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob, cancellationToken: cancellationToken);
        
        if (extension == ".gif" && !currentUser.Roles!.Contains("Premium"))
        {
            Logger.Error($"[UserService.UpdateAvatar] User is not premium user. gif pfp is forbidden. UserId: {currentUserService.GetUserId()}");
            throw new AppException("Gif pfp is forbidden");
        }
        
        if (!allowedExtensions.Contains(extension))
        {
            Logger.Error($"[UserService.UpdateAvatar] File extension is invalid. UserId: {currentUserService.GetUserId()}");
            throw new AppException("File extension is invalid");
        }

        foreach (var ext in allowedExtensions.Concat([".gif"]))
            await container.GetBlobClient($"{currentUser.Id}{ext}").DeleteIfExistsAsync(cancellationToken: cancellationToken);
        
        var newPfp = $"{currentUser.Id}{extension}";
        var blobClient = container.GetBlobClient(newPfp);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: cancellationToken);

        var photoUrl = blobClient.Uri.ToString();
        return photoUrl;
    }

    public async Task<bool> Delete(CancellationToken cancellationToken)
    {
        var user = await Get(cancellationToken);
        user.IsDeleted = true;
        var deletedUser = await Update(user, cancellationToken);
        if (user.IsDeleted != deletedUser.IsDeleted)
        {
            Logger.Error($"[UserService.Delete] User ({user.Id}){user.Username} is not deleted");
            throw new AppException("User is not deleted", 500);
        }
        return true;
    }

    public async Task<bool> ChangePassword(string oldPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await Get(cancellationToken);

        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.HashedPassword))
        {
            Logger.Error($"[UserService.ChangePassword] Incorrect current password for UserId: {user.Id}");
            throw new AppException("password is wrong", 401);
        }

        user.HashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword, 11);

        await Update(user, cancellationToken);
        return true;
    }
}