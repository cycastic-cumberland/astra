using Astra.Client;
using Astra.Client.Aggregator;
using Astra.Common;
using Astra.Engine;
using Astra.Server;
using Astra.Server.Authentication;

namespace Astra.Tests;

[TestFixture]
public class AggregationTestFixture
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
            Indexed = true,
        },
    };
    private AstraConnectionSettings _connectionSettings;
    
    private TcpServer _server = null!;
    private Task _serverTask = null!;
    private SimpleAstraClient _simpleAstraClient = null!;
    private AstraTable<int, string, string, byte[]> _table = null!;
    
    [SetUp]
    public async Task SetUp()
    {
        const string password = "helloWorld";
        _connectionSettings = new()
        {
            Address = "127.0.0.1",
            Port = TcpServer.DefaultPort,
            Schema = new()
            {
                Columns = _columns
            },
            Password = password
        };
        _server = new(new()
        {
            LogLevel = "debug",
            Schema = new()
            {
                Columns = _columns
            }
        }, AuthenticationHelper.Sha256Authentication(Hash256.HashSha256(password)));
        _serverTask = _server.RunAsync();
        _table = new();
        await Task.Delay(100);
        _simpleAstraClient = new SimpleAstraClient();
        await _simpleAstraClient.ConnectAsync(_connectionSettings);
        await _simpleAstraClient.BulkInsertSerializableAsync(new SimpleSerializableStruct[]
        {
            new()
            {
                Value1 = 1,
                Value2 = "test1",
                Value3 = "𝄞",
                Value4 = new byte[] { 1, 2, 3, 4 }
            },
            new()
            {
                Value1 = 1,
                Value2 = "test1",
                Value3 = "🇵🇱",
                Value4 = new byte[] { 1, 2, 3, 4 }
            },
            new()
            {
                Value1 = 2,
                Value2 = "𝄞",
                Value3 = "🇵🇱",
                Value4 = new byte[] { 1, 2, 3, 4 }
            },
            new()
            {
                Value1 = 2,
                Value2 = "test4",
                Value3 = "🇵🇱",
                Value4 = new byte[] { 1, 2, 3, 4 }
            },
            new()
            {
                Value1 = 2,
                Value2 = "𝄞",
                Value3 = "test4",
                Value4 = new byte[] { 1, 2, 3, 4 }
            },
        });
    }
    
    [TearDown]
    public Task TearDown()
    {
        _simpleAstraClient.Dispose();
        _server.Kill();
        return _serverTask;
    }

    
    [Test]
    public async Task SimpleFetchTest()
    {
        var fetch1 = await _simpleAstraClient.AggregateAsync<SimpleSerializableStruct>(
            _table.Column1.EqualsLiteral(2));
        var count1 = 0;
        foreach (var f in fetch1)
        {
            Assert.That(f.Value1, Is.EqualTo(2));
            count1++;
        }
        Assert.That(count1, Is.EqualTo(2));
        var fetch2 = await _simpleAstraClient.AggregateAsync<SimpleSerializableStruct>(
            _table.Column1.EqualsLiteral(2).And(_table.Column3.EqualsLiteral("🇵🇱")));
        var count2 = 0;
        foreach (var f in fetch2)
        {
            Assert.Multiple(() =>
            {
                Assert.That(f.Value1, Is.EqualTo(2));
                Assert.That(f.Value3, Is.EqualTo("🇵🇱"));
            });
            count2++;
        }
        Assert.That(count2, Is.EqualTo(1));
        var fetch3 = await _simpleAstraClient.AggregateAsync<SimpleSerializableStruct>(
            _table.Column1.EqualsLiteral(2).Or(_table.Column3.EqualsLiteral("🇵🇱")));
        var count3 = 0;
        foreach (var _ in fetch3)
        {
            count3++;
        }
        Assert.That(count3, Is.EqualTo(3));
    }
}