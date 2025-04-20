using Aniyuu.DbContext;
using Aniyuu.Exceptions;
using Aniyuu.Interfaces;
using Aniyuu.Interfaces.UserInterfaces;
using Aniyuu.Models.UserModels;
using Aniyuu.Utils;
using MongoDB.Driver;
using NLog;

namespace Aniyuu.Services.UserServices;

public class ActivationService(IMongoDbContext mongoDbContext,
    IEmailService emailService) : IActivationService
{
    private readonly IMongoCollection<UserModel> _userCollection = mongoDbContext
        .GetCollection<UserModel>(AppSettingConfig.Configuration["MongoDBSettings:UserCollection"]!);
    private readonly IMongoCollection<ActivationCodeModel> _codeCollection = mongoDbContext
        .GetCollection<ActivationCodeModel>(AppSettingConfig.Configuration["MongoDBSettings:CodeCollection"]!);
    
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public async Task<bool> ActivateUser(int code, CancellationToken cancellationToken)
    {
        var codeModel = await _codeCollection
            .Find(x=>x.ActivationCode == code)
            .FirstOrDefaultAsync(cancellationToken);
        if (codeModel == null || codeModel.ExpireAt < DateTime.UtcNow)
            throw new AppException("code not found or expired", 404);
        
        var filter = Builders<UserModel>.Filter.Eq(u => u.Id, codeModel.UserId);
        var update = Builders<UserModel>.Update.Set(u => u.IsActive, true);

        var result = await _userCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> ResendActivationCode(string email, CancellationToken cancellationToken)
    {
        var user = await _userCollection.Find(x => x.Email == email).FirstOrDefaultAsync(cancellationToken);
        if (user == null && user.IsActive == true)
        {
            Logger.Error($"[ActivationService.ResendActivationCode] User with email {email} not found or already activated");
            throw new AppException("user is not found", 404);
        }
        var filter = Builders<ActivationCodeModel>.Filter.And(
            Builders<ActivationCodeModel>.Filter.Eq(x => x.UserId, user.Id),
            Builders<ActivationCodeModel>.Filter.Eq(x => x.IsExpired, false)
            );
        var allCodeModels = await _codeCollection.Find(filter)
            .ToListAsync(cancellationToken);
        var update = Builders<ActivationCodeModel>.Update.Set(x => x.UsedAt, DateTime.UtcNow)
            .Set(x => x.IsExpired, true);
        try
        {
            await _codeCollection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            Logger.Error($"[ActivationService.ResendActivationCode] Error updating code", e);
            throw new AppException("Error updating code", 500);
        }
        var code = await GenerateActivationCode(email, cancellationToken);
        emailService.ResendConfirmationEmail(email, user.Username, code, cancellationToken);
        return true;
    }

    public async Task<int> GenerateActivationCode(string email, CancellationToken cancellationToken)
    {
        try
        {
            var userId = await _userCollection.Find(x => x.Email == email).Project(x => x.Id).FirstOrDefaultAsync(cancellationToken);
            
            var rnd = new Random();
            var activateCode = rnd.Next(100000, 1000000);

            var code = new ActivationCodeModel
            {
                ActivationCode = activateCode,
                ExpireAt = DateTime.UtcNow.AddHours(1),
                UserId = userId,
                IsExpired = false
            };
            await _codeCollection.InsertOneAsync(code, cancellationToken);
            return Convert.ToInt32(code.ActivationCode);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new AppException("Server error occurred on generating activation code");
        }
    }
}