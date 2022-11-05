using Azure;
using Azure.Data.Tables;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.TableStorage.Tests
{
    [Ignore("integration")]
    [TestFixture]
    public class TableStorageTests
    {
        class TestEntity : ITableEntity
        {
            public TestEntity()
            {
            }

            public TestEntity(string partitionKey, string rowKey)
            {
                PartitionKey = partitionKey;
                RowKey = rowKey;
            }

            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public DateTimeOffset? Timestamp { get; set; }
            public ETag ETag { get; set; } = ETag.All;

            public string Data { get; set; }
        }

        private readonly TableStorageRepository _tableStorageRepository;
        private readonly BlobStorageRepository _blobStorageRepository;
        private const int EntitiesCount = 200;
        private const string BlobContainerName = "data";
        private static readonly string Partition = $"{Guid.NewGuid()}";
        private TestEntity[] _createdEntities = Array.Empty<TestEntity>();

        public TableStorageTests()
        {
            _tableStorageRepository = new TableStorageRepository(new Uri(""),
                new TableSharedKeyCredential("", ""), "");

            _blobStorageRepository = new BlobStorageRepository("");
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            await _tableStorageRepository.DeleteMany(_createdEntities);

            await _blobStorageRepository.DeleteFile("folder/data.txt",BlobContainerName);
        }

        [Test,Order(0)]
        public async Task ShouldCreateSomeRecords()
        {

            _createdEntities = Enumerable.Range(0, EntitiesCount).Select(index => new TestEntity(Partition, $"{index}")).ToArray();

            await _tableStorageRepository.CreateOrUpdateMany(_createdEntities);
        }

        [Test, Order(1)]
        public async Task GetAllRecords()
        {
            var entities = await _tableStorageRepository.GetAll<TestEntity>().ToArrayAsync();

            Assert.AreEqual(EntitiesCount, entities.Length);
        }

        [Test, Order(2)]
        public async Task GetSomeRecords()
        {
            var entities = await _tableStorageRepository.GetMany<TestEntity>((entity) => entity.RowKey == "0" || entity.RowKey == "1").ToArrayAsync();

            Assert.AreEqual(2, entities.Length);
        }

        [Test, Order(3)]
        public async Task GetOneRecord()
        {
            var entity = await _tableStorageRepository.GetOne<TestEntity>(Partition, "0");

            Assert.NotNull(entity);
        }

        [Test, Order(4)]
        public async Task ShouldUpdateSomeRecords()
        {
            var entities = await _tableStorageRepository.GetMany<TestEntity>((entity) => entity.RowKey == "0" || entity.RowKey == "1").ToArrayAsync();

            Assert.AreEqual(2, entities.Length);

            foreach(var entity in entities)
            {
                entity.Data = $"{Guid.NewGuid()}";
            }

            await _tableStorageRepository.CreateOrUpdateMany(entities);

            entities = await _tableStorageRepository.GetMany<TestEntity>((entity) => entity.RowKey == "0" || entity.RowKey == "1").ToArrayAsync();

            foreach (var entity in entities)
            {
                Assert.NotNull(entity.Data);
            }
        }

        [Test, Order(5)]    
        public async Task ShouldUpdateOneRecords()
        {
            var entity = await _tableStorageRepository.GetOne<TestEntity>(Partition, "10");

            Assert.NotNull(entity);

            entity.Data = $"{Guid.NewGuid()}";

             await _tableStorageRepository.CreateOrUpdateOne(entity);

            entity = await _tableStorageRepository.GetOne<TestEntity>(Partition, "10");

            Assert.NotNull(entity.Data);
        }

        [Test, Order(6)]
        public async Task ShouldDeleteSomeRecords()
        {
            var entities = await _tableStorageRepository.GetMany<TestEntity>((entity) => entity.RowKey == "0" || entity.RowKey == "1").ToArrayAsync();
            
            Assert.AreEqual(2, entities.Length);

            await _tableStorageRepository.DeleteMany(entities.ToArray());

           var deletedEntities = await _tableStorageRepository.GetMany<TestEntity>((entity) => entity.RowKey == "0" || entity.RowKey == "1").ToArrayAsync();

            Assert.AreEqual(0, deletedEntities.Length);

            await _tableStorageRepository.CreateOrUpdateMany(entities);

        }

        [Test, Order(7)]
        public async Task ShouldCreateABlob()
        {
            await _blobStorageRepository.UploadFile("./data.txt", "folder/data.txt", "data");
        }

        [Test, Order(8)]
        public async Task ShouldReadAndDowloadABlob()
        {
            var fileContent = await _blobStorageRepository.ReadFile("folder/data.txt", "data");

            Assert.AreEqual(File.ReadAllText("./data.txt"), fileContent);

            await _blobStorageRepository.DownloadFile("./data2.txt", "folder/data.txt", "data");

            Assert.IsTrue(File.Exists("./data2.txt"));

            Assert.AreEqual(File.ReadAllText("./data.txt"), File.ReadAllText("./data2.txt"));

        }
    }
}