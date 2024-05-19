using Astra.Client;
using Astra.Common;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Server;
using Astra.Server.Authentication;

namespace Astra.Tests.Astra;

[TestFixture]
public class SequentialClientTestFixture
{
    private readonly ColumnSchemaSpecifications[] _columns =
    [
        new()
        {
            Name = "col1",
            DataType = DataType.DWordMask,
            Indexer = IndexerType.BTree,
        },
        new()
        {
            Name = "col2",
            DataType = DataType.StringMask,
            Indexer = IndexerType.None, 
        },
        new()
        {
            Name = "col3",
            DataType = DataType.StringMask,
            Indexer = IndexerType.Generic,
        },
        new()
        {
            Name = "col4",
            DataType = DataType.BytesMask,
            Indexer = IndexerType.None,
        },
        new()
        {
            Name = "col5",
            DataType = DataType.SingleMask,
            Indexer = IndexerType.BTree,
        },
    ];
    private AstraClientConnectionSettings _connectionSettings;
    
    private TcpServer _server = null!;
    private Task _serverTask = null!;
    private AstraClient _astraClient = null!;
    
    [OneTimeSetUp]
    public async Task SetUp()
    {
        const string password = "helloWorld";
        _connectionSettings = new()
        {
            Address = "127.0.0.1",
            Port = TcpServer.DefaultPort,
            Password = password
        };
        _server = new(new()
        {
            LogLevel = "debug",
            Schema = new()
            {
                Columns = _columns
            }
        }, AuthenticationHelper.SaltedSha256Authentication(password));
        _serverTask = _server.RunAsync();
        await Task.Delay(100);
        _astraClient = new AstraClient();
        await _astraClient.ConnectAsync(_connectionSettings);
    }
    
    [OneTimeTearDown]
    public Task TearDown()
    {
        _astraClient.Dispose();
        _server.Kill();
        return _serverTask;
    }

    private static int _num;

    
    private async Task SimpleValueTypeInsertionTestAsync()
    {
        var originalAmount = await _astraClient.CountAllAsync();
        var inserted = await _astraClient.BulkInsertSerializableCompatAsync(new SimpleSerializableStruct[]
        {
            new()
            {
                Value1 = ++_num,
                Value2 = "test1",
                Value3 = "𝄞",
                Value4 = new byte[] { 1, 2, 3, 4 }
            },
            new()
            {
                Value1 = ++_num,
                Value2 = "test1",
                Value3 = "𝄞",
                Value4 = new byte[] { 1, 2, 3, 4 }
            },
            new()
            {
                Value1 = _num,
                Value2 = "test1",
                Value3 = "𝄞",
                Value4 = new byte[] { 1, 2, 3, 4 }
            },
        });
        var newAmount = await _astraClient.CountAllAsync();
        Assert.Multiple(() =>
        {
            Assert.That(inserted, Is.EqualTo(2));
            Assert.That(newAmount - originalAmount, Is.EqualTo(2));
        });
    }

    [Test]
    public Task ValueTypeBulkInsertionOne()
    {
        return SimpleValueTypeInsertionTestAsync();
    }
    
    [Test]
    public Task ValueTypeBulkInsertionTwo()
    {
        return SimpleValueTypeInsertionTestAsync();
    }
    
    [Test]
    public Task ValueTypeBulkInsertionThree()
    {
        return SimpleValueTypeInsertionTestAsync();
    }

    [Test]
    public async Task ZDataStoreClearTest()
    {
        var originalAmount = await _astraClient.CountAllAsync();
        // Assert.That(originalAmount, Is.Not.Zero);
        var affected = await _astraClient.ClearAsync();
        Assert.That(originalAmount, Is.EqualTo(affected));
        var newAmount = await _astraClient.CountAllAsync();
        Assert.That(newAmount, Is.Zero);
    }
}