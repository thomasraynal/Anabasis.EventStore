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

namespace Anabasis.TableStorage
{
    public class TableStorageUserStore<TUser, TRole, TClaim> : IAnabasisUserStore<TUser>
        where TUser : TableStorageUser, new()
        where TRole : TableStorageRole, new()
        where TClaim : TableStorageClaim, new()
    {
        private readonly IPasswordHasher<TUser> _passwordHasher;
        private readonly TableStorageRepository _usersTableStorageRepository;
        private readonly TableStorageRepository _userClaimsTableStorageRepository;
        private readonly TableStorageRepository _userRolesTableStorageRepository;

        public const string UsersTable = "anabasisusers";
        public const string UserClaimsTable = "userclaims";
        public const string UserRolesTable = "userroles";
        
        public TableStorageUserStore(TableStorageRepositoryOptions tableStorageRepositoryOptions, IPasswordHasher<TUser> passwordHasher)
        {
            var storageUri = tableStorageRepositoryOptions.StorageUri;
            var tableSharedKeyCredential = tableStorageRepositoryOptions.TableSharedKeyCredential;

            _passwordHasher = passwordHasher;
            _usersTableStorageRepository = new TableStorageRepository(storageUri, tableSharedKeyCredential, UsersTable);
            _userClaimsTableStorageRepository = new TableStorageRepository(storageUri, tableSharedKeyCredential, UserClaimsTable);
            _userRolesTableStorageRepository = new TableStorageRepository(storageUri, tableSharedKeyCredential, UserRolesTable);
        }

        public Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task AddToRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken)
        {
            var allUserRoles = await _userRolesTableStorageRepository.GetMany<TRole>((role) => role.PartitionKey == user.PartitionKey, cancellationToken: cancellationToken).ToListAsync(cancellationToken);

            return allUserRoles.Select(userRole => userRole.Name).ToList();
        }

        public async Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            return await _userRolesTableStorageRepository.DoesEntityExist<TRole>(user.PartitionKey, roleName, cancellationToken: cancellationToken);
        }

        public async Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            var allRelevantRoles = await _userRolesTableStorageRepository.GetMany<TRole>((role) => role.RowKey == roleName, cancellationToken: cancellationToken).ToListAsync(cancellationToken);

            var getRelevantUserTasks = allRelevantRoles.Select(relevantRole => Task.Run(async () =>
           {
              return await _usersTableStorageRepository.GetOne<TUser>(relevantRole.PartitionKey, relevantRole.PartitionKey);

           }));

            await getRelevantUserTasks.ExecuteAndWaitForCompletion(10);

            return getRelevantUserTasks.Select(getUserTask => getUserTask.Result).Distinct().ToList();
        }

        public Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;

            return Task.CompletedTask;
        }

        public Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
        }

        public Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Guid> GetUserIdAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Id);
        }

        public Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.UserName);
        }

        public Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;

            return Task.CompletedTask;
        }

        public Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.UserName);
        }

        public Task SetNormalizedUserNameAsync(TUser user, string normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;

            return Task.CompletedTask;
        }

        public async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken)
        {
            await _usersTableStorageRepository.CreateOrUpdateOne(user, cancellationToken: cancellationToken);

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken)
        {
            await _usersTableStorageRepository.CreateOrUpdateOne(user, cancellationToken: cancellationToken);

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken)
        {
            await _usersTableStorageRepository.DeleteOne(user, cancellationToken: cancellationToken);

            return IdentityResult.Success;
        }

        public async Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            return await _usersTableStorageRepository.GetOne<TUser>(userId, userId, cancellationToken: cancellationToken);
        }

        public async Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            return await _usersTableStorageRepository.GetMany<TUser>((user) => user.NormalizedUserName == normalizedUserName, cancellationToken: cancellationToken)
                                                     .FirstOrDefaultAsync(cancellationToken: cancellationToken);
        }

        public void Dispose()
        {
        }
    }
}
