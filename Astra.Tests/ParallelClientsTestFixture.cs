using Astra.Client;
using Astra.Engine;
using Astra.Server;

namespace Astra.Tests;

[TestFixture]
public class ParallelClientsTestFixture
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
        }
    };
    private AstraConnectionSettings _connectionSettings;
    
    private TcpServer _server = null!;
    private Task _serverTask = null!;
    
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
    }
    
    [OneTimeTearDown]
    public Task TearDown()
    {
        _server.Kill();
        return _serverTask;
    }

    private static int _num;

    
    private async Task SimpleValueTypeInsertionTestAsync()
    {
        using var simpleAstraClient = new SimpleAstraClient();
        await simpleAstraClient.ConnectAsync(_connectionSettings);
        var inserted = await simpleAstraClient.InsertSerializableValueAsync(new SimpleSerializableStruct[]
        {
            new()
            {
                Value1 = ++_num,
                Value2 = "test1",
                Value3 = "test2",
            },
            new()
            {
                Value1 = ++_num,
                Value2 = "test1",
                Value3 = "test2",
            },
            new()
            {
                Value1 = _num,
                Value2 = "test1",
                Value3 = "test2",
            },
        });
        Assert.That(inserted, Is.EqualTo(2));
    }
    
    private async Task SimpleClassTypeInsertionTestAsync()
    {
        using var simpleAstraClient = new SimpleAstraClient();
        await simpleAstraClient.ConnectAsync(_connectionSettings);
        var inserted = await simpleAstraClient.InsertSerializableAsync(new SimpleSerializableClass[]
        {
            new()
            {
                Value1 = ++_num,
                Value2 = "test1",
                Value3 = "test2",
            },
            new()
            {
                Value1 = ++_num,
                Value2 = "test1",
                Value3 = "test2",
            },
            new()
            {
                Value1 = _num,
                Value2 = "test1",
                Value3 = "test2",
            },
        });
        Assert.That(inserted, Is.EqualTo(2));
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
    [Parallelizable]
    public Task ClassTypeBulkInsertionOne()
    {
        return SimpleClassTypeInsertionTestAsync();
    }
    
    [Test]
    [Parallelizable]
    public Task ClassTypeBulkInsertionTwo()
    {
        return SimpleClassTypeInsertionTestAsync();
    }
    
    [Test]
    [Parallelizable]
    public Task ClassTypeBulkInsertionThree()
    {
        return SimpleClassTypeInsertionTestAsync();
    }
}