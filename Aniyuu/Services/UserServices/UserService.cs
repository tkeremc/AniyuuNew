using Aniyuu.DbContext;
using Aniyuu.Exceptions;
using Aniyuu.Helpers;
using Aniyuu.Interfaces.UserInterfaces;
using Aniyuu.Models.UserModels;
using Aniyuu.Utils;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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

        if (file.Length > 2 * 1024 * 1024 && file.Length < 5 * 1024)
        {
            Logger.Error($"[UserService.UpdateAvatar] File cannot be larger than 2MB. UserId: {currentUserService.GetUserId()}");
            throw new AppException("File cannot be larger than 2MB");
        }

        if (!file.ContentType.StartsWith("image/"))
        {
            Logger.Error($"[UserService.UpdateAvatar] File type not supported. UserId: {currentUserService.GetUserId()}");
            throw new AppException("File type not supported");
        }
        
        var currentUser = await Get(cancellationToken);
        
        var allowedExtensions = new[] { ".jpg", ".png", ".jpeg" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (extension == ".gif" && !currentUser.Roles!.Contains("Premium"))
        {
            Logger.Error($"[UserService.UpdateAvatar] User is not premium user. gif pfp is forbidden. UserId: {currentUserService.GetUserId()}");
            throw new AppException("Gif pfp is forbidden");
        }
        
        if (!allowedExtensions.Contains(extension) && extension != ".gif")
        {
            Logger.Error($"[UserService.UpdateAvatar] File extension is invalid. UserId: {currentUserService.GetUserId()}");
            throw new AppException("File extension is invalid");
        }

        var container = blobServiceClient.GetBlobContainerClient(AppSettingConfig.Configuration["AzureBlob:ProfilePhotoContainer"]);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        if (currentUser.ProfilePhoto != "not set")
        {
            var oldProfilePhoto = Path.GetFileName(new Uri(currentUser.ProfilePhoto).AbsolutePath);
            await container.GetBlobClient(oldProfilePhoto).DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
        
        var usernameExtension = Guid.NewGuid().ToString("N")[..8]; //Guid.NewGuid().ToString("N").Substring(0,8);
        
        var newPfp = $"{currentUser.Id}{usernameExtension}{extension}";
        var blobClient = container.GetBlobClient(newPfp);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = file.ContentType,
                    ContentDisposition = "inline"
                }
            },
            cancellationToken
            );

        var photoUrl = blobClient.Uri.ToString();

        currentUser.ProfilePhoto = photoUrl;
        await Update(currentUser, cancellationToken, currentUserService.GetUserId());
        
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