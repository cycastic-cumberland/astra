using Astra.Client;
using Astra.Client.Aggregator;
using Astra.Common;
using Astra.Server;
using Astra.Server.Authentication;

namespace Astra.Tests.Astra;

[TestFixture]
public class AggregationTestFixture
{
    private readonly ColumnSchemaSpecifications[] _columns =
    [
        new()
        {
            Name = "col1",
            DataType = DataType.DWordMask,
            Indexer = IndexerType.Range,
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
            Indexer = IndexerType.Generic,
        },
        new()
        {
            Name = "col5",
            DataType = DataType.SingleMask,
            Indexer = IndexerType.Range,
        },
    ];
    private AstraConnectionSettings _connectionSettings;
    
    private TcpServer _server = null!;
    private Task _serverTask = null!;
    private SimpleAstraClient _simpleAstraClient = null!;
    private AstraTable<int, string, string, byte[], float> _table = null!;
    
    [OneTimeSetUp]
    public async Task SetUp()
    {
        const string password = "helloWorld";
        _connectionSettings = new()
        {
            Address = "127.0.0.1",
            Port = TcpServer.DefaultPort + 1,
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
            },
            Port = TcpServer.DefaultPort + 1
        }, AuthenticationHelper.SaltedSha256Authentication(password));
        _serverTask = _server.RunAsync();
        _table = new();
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


    private async Task CleanUp()
    {
        await _simpleAstraClient.ClearAsync();
        Assert.That(await _simpleAstraClient.CountAllAsync(), Is.EqualTo(0));
    }
    
    [Test]
    public async Task SimpleFetchTest()
    {
        await _simpleAstraClient.BulkInsertSerializableAsync(new SimpleSerializableStruct[]
        {
            new()
            {
                Value1 = 1,
                Value2 = "test1",
                Value3 = "𝄞",
                Value4 = [1, 2, 3, 4]
            },
            new()
            {
                Value1 = 1,
                Value2 = "test1",
                Value3 = "🇵🇱",
                Value4 = [1, 2, 3, 4]
            },
            new()
            {
                Value1 = 2,
                Value2 = "𝄞",
                Value3 = "🇵🇱",
                Value4 = [1, 2, 3, 4]
            },
            new()
            {
                Value1 = 2,
                Value2 = "test4",
                Value3 = "🇵🇱",
                Value4 = [1, 2, 3, 4]
            },
            new()
            {
                Value1 = 2,
                Value2 = "𝄞",
                Value3 = "test4",
                Value4 = [1, 2, 3, 4]
            },
        });
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
        var count3 = fetch3.Count();
        Assert.That(count3, Is.EqualTo(3));
        await CleanUp();
    }

    [Test]
    public async Task RangeAggregationTest()
    {
        await _simpleAstraClient.BulkInsertSerializableAsync(new SimpleSerializableStruct[]
        {
            new()
            {
                Value1 = 1,
                Value2 = string.Empty,
                Value3 = string.Empty,
                Value4 = Array.Empty<byte>(),
                Value5 = 1.2f
            },
            new()
            {
                Value1 = 2,
                Value2 = string.Empty,
                Value3 = string.Empty,
                Value4 = Array.Empty<byte>(),
                Value5 = 1.4f
            },
            new()
            {
                Value1 = 3,
                Value2 = string.Empty,
                Value3 = string.Empty,
                Value4 = Array.Empty<byte>(),
                Value5 = 1.6f
            },
            new()
            {
                Value1 = 4,
                Value2 = string.Empty,
                Value3 = string.Empty,
                Value4 = Array.Empty<byte>(),
                Value5 = 1.8f
            },
            new()
            {
                Value1 = 5,
                Value2 = string.Empty,
                Value3 = string.Empty,
                Value4 = Array.Empty<byte>(),
                Value5 = -1.2f
            },
        });
        
        Assert.That(await _simpleAstraClient.CountAllAsync(), Is.EqualTo(5));

        var fetch = await _simpleAstraClient.AggregateAsync<SimpleSerializableStruct>(
            _table.Column5.LesserThan(0));

        var count = 0;
        foreach (var value in fetch)
        {
            Assert.That(value.Value5, Is.EqualTo(-1.2f));
            Assert.That(value.Value1, Is.EqualTo(5));
            count++;
        }
        
        Assert.That(count, Is.EqualTo(1));

        count = 0;
        fetch = await _simpleAstraClient.AggregateAsync<SimpleSerializableStruct>(
            _table.Column5.Between(1.1f, 1.3f));
        foreach (var value in fetch)
        {
            Assert.That(value.Value5, Is.EqualTo(1.2f));
            Assert.That(value.Value1, Is.EqualTo(1));
            count++;
        }
        
        Assert.That(count, Is.EqualTo(1));
        
        count = 0;
        fetch = await _simpleAstraClient.AggregateAsync<SimpleSerializableStruct>(
            _table.Column5.Between(1.3f, 1.5f));
        foreach (var value in fetch)
        {
            Assert.That(value.Value5, Is.EqualTo(1.4f));
            Assert.That(value.Value1, Is.EqualTo(2));
            count++;
        }
        
        Assert.That(count, Is.EqualTo(1));
        
        count = 0;
        fetch = await _simpleAstraClient.AggregateAsync<SimpleSerializableStruct>(
            _table.Column5.Between(1.5f, 1.9f));
        foreach (var value in fetch)
        {
            switch (count)
            {
                case 0:
                    Assert.That(value.Value5, Is.EqualTo(1.6f));
                    Assert.That(value.Value1, Is.EqualTo(3));
                    break;
                case 1:
                    Assert.That(value.Value5, Is.EqualTo(1.8f));
                    Assert.That(value.Value1, Is.EqualTo(4));
                    break;
            }

            count++;
        }
        
        Assert.That(count, Is.EqualTo(2));
        
        await CleanUp();
    }
}