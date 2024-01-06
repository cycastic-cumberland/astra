using Astra.Client;
using Astra.Engine;
using Astra.Server;

namespace Astra.Tests;

[TestFixture]
public class ClientTestFixture
{
    private readonly ColumnSchemaSpecifications[] _columns = {
        new()
        {
            Name = "col1",
            DataType = DataType.DWordMask,
            Indexed = true,
        },
        new()
        {
            Name = "col2",
            DataType = DataType.StringMask,
            Indexed = false,
        },
        new()
        {
            Name = "col3",
            DataType = DataType.StringMask,
            Indexed = true,
        },
        new()
        {
            Name = "col4",
            DataType = DataType.BytesMask,
            Indexed = false,
        },
    };
    private AstraConnectionSettings _connectionSettings;
    
    private TcpServer _server = null!;
    private Task _serverTask = null!;
    private SimpleAstraClient _simpleAstraClient = null!;
    
    [OneTimeSetUp]
    public async Task SetUp()
    {
        _connectionSettings = new()
        {
            Address = "127.0.0.1",
            Port = TcpServer.DefaultPort,
            Schema = new()
            {
                Columns = _columns
            }
        };
        _server = new(new()
        {
            LogLevel = "debug",
            Schema = new()
            {
                Columns = _columns
            }
        });
        _serverTask = _server.RunAsync();
        await Task.Delay(100);
        _simpleAstraClient = new SimpleAstraClient();
        await _simpleAstraClient.ConnectAsync(_connectionSettings);
    }
    
    [OneTimeTearDown]
    public Task TearDown()
    {
        _simpleAstraClient.Dispose();
        _server.Kill();
        return _serverTask;
    }

    private static int _num;

    
    private async Task SimpleValueTypeInsertionTestAsync()
    {
        var originalAmount = await _simpleAstraClient.CountAllAsync();
        var inserted = await _simpleAstraClient.BulkInsertSerializableAsync(new SimpleSerializableStruct[]
        {
            new()
            {
                Value1 = ++_num,
                Value2 = "test1",
                Value3 = "test2",
                Value4 = new byte[] { 1, 2, 3, 4 }
            },
            new()
            {
                Value1 = ++_num,
                Value2 = "test1",
                Value3 = "test2",
                Value4 = new byte[] { 1, 2, 3, 4 }
            },
            new()
            {
                Value1 = _num,
                Value2 = "test1",
                Value3 = "test2",
                Value4 = new byte[] { 1, 2, 3, 4 }
            },
        });
        var newAmount = await _simpleAstraClient.CountAllAsync();
        Assert.That(inserted, Is.EqualTo(2));
        Assert.That(newAmount - originalAmount, Is.EqualTo(2));
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
    public async Task ConsecutiveInsertionTest()
    {
        const int taskCount = 42;
        for (var i = 0; i < taskCount; i++)
        {
            await SimpleValueTypeInsertionTestAsync();
        }
    }
}