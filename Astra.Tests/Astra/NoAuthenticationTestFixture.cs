using Astra.Client;
using Astra.Common;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Server;
using Astra.Server.Authentication;

namespace Astra.Tests.Astra;

[TestFixture]
public class NoAuthenticationTestFixture
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
    
    [OneTimeSetUp]
    public Task SetUp()
    {
        _connectionSettings = new()
        {
            Address = "127.0.0.1",
            Port = TcpServer.DefaultPort,
        };
        _server = new(new()
        {
            LogLevel = "debug",
            Schema = new()
            {
                Columns = _columns
            }
        }, AuthenticationHelper.NoAuthentication());
        _serverTask = _server.RunAsync();
        return Task.Delay(100);
        
    }
    
    [OneTimeTearDown]
    public Task TearDown()
    {
        _server.Kill();
        return _serverTask;
    }

    [Test]
    public async Task ConnectionTest()
    {
        using var simpleAstraClient = new AstraClient();
        await simpleAstraClient.ConnectAsync(_connectionSettings);
    }
}