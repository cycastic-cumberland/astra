using Astra.Client;
using Astra.Engine;
using Astra.Server;

namespace Astra.Tests;

[TestFixture]
public class ClientTestFixture
{
    private TcpServer _server = null!;
    private SimpleAstraClient _simpleAstraClient = null!;
    private Task _serverTask = null!;
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

    [SetUp]
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
        _simpleAstraClient = await _connectionSettings.CreateSimpleClient();
    }

    [TearDown]
    public Task TearDown()
    {
        _simpleAstraClient.Dispose();
        _server.Kill();
        return _serverTask;
    }

    [Test]
    public async Task SimpleInsertionTest()
    {
        await using var inStream = MemoryStreamPool.Allocate();
        inStream.WriteValue(3); // Insert three rows
            
        inStream.WriteValue(11);
        inStream.WriteValue("test3");
        inStream.WriteValue("test4");
        inStream.WriteValue(12);
        inStream.WriteValue("test3");
        inStream.WriteValue("test4");
        inStream.WriteValue(11); // Duplicated!
        inStream.WriteValue("test3");
        inStream.WriteValue("test4");
        inStream.Position = 0;
        var inserted = await _simpleAstraClient.UnorderedInsertAsync(inStream);
        Assert.That(inserted, Is.EqualTo(2));
    }
}