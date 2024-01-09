using System.Security.Cryptography;
using Astra.Client;
using Astra.Engine;
using Astra.Server;
using Astra.Server.Authentication;

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
    
    [OneTimeSetUp]
    public Task SetUp()
    {
        string publicKey;
        string privateKey;
        using (var rsa = new RSACryptoServiceProvider())
        {
            publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
            privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        }
        _connectionSettings = new()
        {
            Address = "127.0.0.1",
            Port = TcpServer.DefaultPort,
            Schema = new()
            {
                Columns = _columns
            },
            PrivateKey = privateKey
        };
        _server = new(new()
        {
            LogLevel = "debug",
            Schema = new()
            {
                Columns = _columns
            }
        }, AuthenticationHelper.RSA(publicKey));
        _serverTask = _server.RunAsync();
        return Task.Delay(100);
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
        var inserted = await simpleAstraClient.BulkInsertSerializableAsync(new SimpleSerializableStruct[]
        {
            new()
            {
                Value1 = Interlocked.Increment(ref _num),
                Value2 = "test1",
                Value3 = "𝄞",
                Value4 = new byte[] { 1, 2, 3, 4 }
            },
            new()
            {
                Value1 = Interlocked.Increment(ref _num),
                Value2 = "test1",
                Value3 = "𝄞",
                Value4 = new byte[] { 1, 2, 3, 4 }
            },
            new()
            {
                Value1 = Interlocked.Increment(ref _num),
                Value2 = "test1",
                Value3 = "𝄞",
                Value4 = new byte[] { 1, 2, 3, 4 }
            },
        });
        Assert.That(inserted, Is.EqualTo(3));
    }
    
    [Test, Repeat(10)]
    [Parallelizable]
    public Task ValueTypeBulkInsertion()
    {
        return SimpleValueTypeInsertionTestAsync();
    }
}