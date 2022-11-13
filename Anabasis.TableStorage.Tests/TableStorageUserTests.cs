using Azure.Data.Tables;
using Microsoft.AspNetCore.Identity;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.TableStorage.Tests
{
    //[Ignore("integration")]
    [TestFixture]
    public class TableStorageUserTests
    {
        private TableStorageUserStore<TestUser, TableStorageUserLoginInfo, TableStorageRole, TableStorageClaim> _tableStorageUserStore;
        private readonly Guid _userId;

        class TestUser: TableStorageUser
        {
            public string SomeValue { get; set; } 
        }

        public TableStorageUserTests()
        {
            _userId = Guid.NewGuid();

            var tableStorageRepositoryOptions = new TableStorageRepositoryOptions()
            {
                StorageUri = new Uri("https://anabasisteststorage.table.core.windows.net/"),
                TableSharedKeyCredential = new TableSharedKeyCredential("", "")
            };

            _tableStorageUserStore = new TableStorageUserStore<TestUser, TableStorageUserLoginInfo, TableStorageRole, TableStorageClaim>(tableStorageRepositoryOptions);

        }

        [Test, Order(0)]
        public async Task ShouldCreateOneUser()
        {

            var testUser = new TestUser()
            {
                PartitionKey = _userId.ToString(),
                RowKey = _userId.ToString(),
                SomeValue = $"{Guid.NewGuid()}",
                Id = _userId
            };

            var createUserActionResult = await _tableStorageUserStore.CreateAsync(testUser);

            Assert.AreEqual(true, createUserActionResult.Succeeded);

            await _tableStorageUserStore.AddLoginAsync(testUser, new UserLoginInfo("custom", _userId.ToString(), "Custom Login"));

            var createdUser = await _tableStorageUserStore.FindByLoginAsync("custom", _userId.ToString());

            Assert.AreEqual(testUser.Id, createdUser.Id);

        }

        [Test, Order(1)]
        public async Task ShouldAddRolesAndClaimsToOneUser()
        {
            var testUser = new TestUser()
            {
                PartitionKey = _userId.ToString(),
                RowKey = _userId.ToString(),
                Id = _userId
            };

            await _tableStorageUserStore.AddToRoleAsync(testUser, "admin");

            var userRoles = await _tableStorageUserStore.GetRolesAsync(testUser);

            Assert.True(userRoles.Count == 1);

            await _tableStorageUserStore.RemoveFromRoleAsync(testUser, "admin");

            userRoles = await _tableStorageUserStore.GetRolesAsync(testUser);

            Assert.True(userRoles.Count == 0);

            var claimId = Guid.NewGuid().ToString();

            var testClaim = new Claim("special", "very special");

            await _tableStorageUserStore.AddClaimsAsync(testUser, new[] { testClaim });

            var userClaims = await _tableStorageUserStore.GetClaimsAsync(testUser);

            Assert.True(userClaims.Count == 1);

            await _tableStorageUserStore.RemoveClaimsAsync(testUser, new[] { testClaim });

            userClaims = await _tableStorageUserStore.GetClaimsAsync(testUser);

            Assert.True(userClaims.Count == 0);

        }

        [Test, Order(2)]
        public async Task ShouldDeleteOneUser()
        {
            var testUser = new TestUser()
            {
                PartitionKey = _userId.ToString(),
                RowKey = _userId.ToString(),
                Id = _userId
            };

            await _tableStorageUserStore.DeleteAsync(testUser);

            await _tableStorageUserStore.AddLoginAsync(testUser, new UserLoginInfo("custom", _userId.ToString(), "Custom Login"));

            var deletedUser = await _tableStorageUserStore.FindByLoginAsync("custom", _userId.ToString());

            Assert.IsNull(deletedUser);
        }
    }
}
