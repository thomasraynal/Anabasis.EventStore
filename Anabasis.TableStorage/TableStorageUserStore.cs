using Anabasis.Common.Contracts;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Anabasis.Common;
using Anabasis.Common.Utilities;

namespace Anabasis.TableStorage
{
    public static class TableStorageExtensions
    {
        public static bool IsEntityNotFound(this Exception exception)
        {
            var actualException = exception.GetActualException();

            return actualException.Message.Contains("The specified resource does not exist");
        }
    }

    public class TableStorageUserRepositoryOptions : TableStorageRepositoryOptions
    {
    }

    public class TableStorageUserStore<TUser, TUserLoginInfo, TRole, TClaim> : IAnabasisUserStore<TUser>
        where TUser : TableStorageUser, new()
        where TRole : TableStorageRole, new()
        where TUserLoginInfo : TableStorageUserLoginInfo, new()
        where TClaim : TableStorageClaim, new()
    {
        private readonly TableStorageRepository _usersTableStorageRepository;
        private readonly TableStorageRepository _userClaimsTableStorageRepository;
        private readonly TableStorageRepository _userRolesTableStorageRepository;
        private readonly TableStorageRepository _userLoginsTableStorageRepository;

        public const string UsersTable = "anabasisusers";
        public const string UserClaimsTable = "anabasiuserclaims";
        public const string UserRolesTable = "anabasiuserroles";
        public const string UserLoginsTable = "anabasiuserlogins";

        public TableStorageUserStore(TableStorageUserRepositoryOptions tableStorageUserRepositoryOptions)
        {
            var storageUri = tableStorageUserRepositoryOptions.StorageUri;
            var tableSharedKeyCredential = tableStorageUserRepositoryOptions.TableSharedKeyCredential;

            _usersTableStorageRepository = new TableStorageRepository(storageUri, tableSharedKeyCredential, UsersTable);
            _userClaimsTableStorageRepository = new TableStorageRepository(storageUri, tableSharedKeyCredential, UserClaimsTable);
            _userRolesTableStorageRepository = new TableStorageRepository(storageUri, tableSharedKeyCredential, UserRolesTable);
            _userLoginsTableStorageRepository = new TableStorageRepository(storageUri, tableSharedKeyCredential, UserLoginsTable);
        }

        public async Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var claims = await _userClaimsTableStorageRepository.GetMany<TClaim>(user.PartitionKey, cancellationToken).ToListAsync(cancellationToken);

            return claims.Select(claim => new Claim(claim.ClaimType, claim.ClaimValue)).ToArray();
        }

        public Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var userClaims = claims.Select(claim => new TClaim()
            {
                ClaimType = claim.ValueType,
                ClaimValue = claim.Value,
                PartitionKey = user.PartitionKey,
                RowKey = $"{Guid.NewGuid()}"
            });

           return _userClaimsTableStorageRepository.CreateOrUpdateMany(userClaims.ToArray(), cancellationToken: cancellationToken);
        }

        public async Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default)
        {
            try
            {

                cancellationToken.ThrowIfCancellationRequested();

                var userClaim = await _userClaimsTableStorageRepository.GetOne<TClaim>((userClaim) =>
                        userClaim.UserId == user.PartitionKey &&
                        userClaim.ClaimType == claim.Type &&
                        userClaim.ClaimValue == claim.Value, cancellationToken);

                var newUserClaim = new TClaim()
                {
                    ClaimType = claim.ValueType,
                    ClaimValue = claim.Value,
                    PartitionKey = user.PartitionKey,
                    RowKey = $"{Guid.NewGuid()}"
                };

                await _userClaimsTableStorageRepository.CreateOrUpdateOne(newUserClaim, cancellationToken: cancellationToken);

            }
            catch (Exception ex)
            {
                if (ex.IsEntityNotFound())
                {
                    return;
                }

                throw;
            }
        }

        public async Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var claimsToDelete = claims.ToArray();

            var allUserClaims = await _userClaimsTableStorageRepository.GetMany<TClaim>((userClaim) => userClaim.PartitionKey == user.PartitionKey, cancellationToken).ToListAsync(cancellationToken);

            var allClaimsToDelete = allUserClaims.Where(claim => claimsToDelete.Any(claimToDelete => claim.ClaimValue == claimToDelete.Value)).ToArray();

            await _userClaimsTableStorageRepository.DeleteMany(allClaimsToDelete, cancellationToken: cancellationToken);
        }

        public async Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var allClaims = await _userClaimsTableStorageRepository.GetMany<TClaim>((userClaim) => claim.Type == userClaim.ClaimType && claim.Value == userClaim.ClaimValue, cancellationToken).ToListAsync(cancellationToken);

            var getUserTasks = allClaims.Select(claim => claim.UserId)
                                        .Distinct()
                                        .Select(userId => _userClaimsTableStorageRepository.GetOne<TUser>(userId, userId))
                                        .ToArray();

            await getUserTasks.ExecuteAndWaitForCompletion(10);

            return getUserTasks.Select(task => task.Result).ToList();

        }

        public Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var userLoginInfo = new TUserLoginInfo
            {
                PartitionKey = user.PartitionKey,
                RowKey = user.RowKey,
                LoginProvider = login.LoginProvider,
                ProviderDisplayName = login.ProviderDisplayName,
                ProviderKey = login.ProviderKey
            };

            return _userLoginsTableStorageRepository.CreateOrUpdateOne(userLoginInfo, cancellationToken: cancellationToken);
        }

        public Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _userLoginsTableStorageRepository.DeleteOne<TUserLoginInfo>(user.PartitionKey, loginProvider, cancellationToken);
        }

        public async Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var userLoginInfos = await _userLoginsTableStorageRepository.GetMany<TUserLoginInfo>((userLoginInfo) => userLoginInfo.PartitionKey == user.PartitionKey, cancellationToken).ToListAsync(cancellationToken);
            
            return userLoginInfos.Cast<UserLoginInfo>().ToList();
        }

        public async Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var loginInfo = await _userLoginsTableStorageRepository.GetOne<TUserLoginInfo>((userLoginInfo) => userLoginInfo.ProviderKey == providerKey && userLoginInfo.LoginProvider == loginProvider, cancellationToken);

                if (null == loginInfo) return null;
                return await _usersTableStorageRepository.GetOne<TUser>(loginInfo.PartitionKey, loginInfo.PartitionKey, cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex.IsEntityNotFound())
                {
                    return null;
                }

                throw;
            }
        }


        public Task AddToRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();


            var role = new TRole()
            {
                PartitionKey = user.PartitionKey,
                RowKey= roleName,
                Name = roleName,
                Id = $"{Guid.NewGuid()}"
            };

            return _userRolesTableStorageRepository.CreateOrUpdateOne(role, cancellationToken: cancellationToken);
        }

        public Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _userRolesTableStorageRepository.DeleteOne<TRole>(user.PartitionKey, roleName, cancellationToken);
        }

        public async Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var allUserRoles = await _userRolesTableStorageRepository.GetMany<TRole>((role) => role.PartitionKey == user.PartitionKey, cancellationToken: cancellationToken).ToListAsync(cancellationToken);

            return allUserRoles.Select(userRole => userRole.Name).ToList();
        }

        public Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _userRolesTableStorageRepository.DoesEntityExist<TRole>(user.PartitionKey, roleName, cancellationToken: cancellationToken);
        }

        public async Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var allRelevantRoles = await _userRolesTableStorageRepository.GetMany<TRole>((role) => role.RowKey == roleName, cancellationToken: cancellationToken).ToListAsync(cancellationToken);

            var getRelevantUserTasks = allRelevantRoles.Select(relevantRole => _usersTableStorageRepository.GetOne<TUser>(relevantRole.PartitionKey, relevantRole.PartitionKey)).ToArray();

            await getRelevantUserTasks.ExecuteAndWaitForCompletion(10);

            return getRelevantUserTasks.Select(getUserTask => getUserTask.Result).Distinct().ToList();
        }

        public Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            user.PasswordHash = passwordHash;

            return Task.CompletedTask;
        }

        public Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
        }

        public Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            user.SecurityStamp = stamp;

            return Task.CompletedTask;
        }

        public Task<string> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(user.SecurityStamp);
        }

        public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult($"{user.Id}");
        }

        public Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(user.UserName);
        }

        public Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            user.UserName = userName;

            return Task.CompletedTask;
        }

        public Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(user.UserName);
        }

        public Task SetNormalizedUserNameAsync(TUser user, string normalizedName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            user.NormalizedUserName = normalizedName;

            return Task.CompletedTask;
        }

        public async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _usersTableStorageRepository.CreateOrUpdateOne(user, cancellationToken: cancellationToken);

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _usersTableStorageRepository.CreateOrUpdateOne(user, cancellationToken: cancellationToken);

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _usersTableStorageRepository.DeleteOne(user, cancellationToken: cancellationToken);

            return IdentityResult.Success;
        }

        public Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
        {

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return _usersTableStorageRepository.GetOne<TUser>(userId, userId, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex.IsEntityNotFound())
                {
                    return null;
                }

                throw;
            }
        }

        public async Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await _usersTableStorageRepository.GetMany<TUser>((user) => user.NormalizedUserName == normalizedUserName, cancellationToken: cancellationToken)
                                                     .FirstOrDefaultAsync(cancellationToken: cancellationToken);
        }

        public void Dispose()
        {
        }

        public Task SetEmailAsync(TUser user, string email, CancellationToken cancellationToken)
        {
            user.Email = email;

            return Task.CompletedTask; 
        }

        public Task<string> GetEmailAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.EmailConfirmed);
        }

        public Task SetEmailConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;

            return Task.CompletedTask;
        }

        public async Task<TUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await _usersTableStorageRepository.GetMany<TUser>((user) => user.NormalizedEmail == normalizedEmail, cancellationToken: cancellationToken)
                                                     .FirstOrDefaultAsync(cancellationToken: cancellationToken);
        }

        public Task<string> GetNormalizedEmailAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalizedEmail);
        }

        public Task SetNormalizedEmailAsync(TUser user, string normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;

            return Task.CompletedTask;
        }
    }
}
