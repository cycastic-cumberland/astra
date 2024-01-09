using System.Security.Cryptography;
using Astra.Client;
using Astra.Client.Aggregator;
using Astra.Engine;
using Astra.Server;
using Astra.Server.Authentication;
using Microsoft.Extensions.Logging;

namespace Astra.Example;

internal struct SimpleSerializableStruct : IAstraSerializable
{
    public int Value1 { get; set; }
    public string Value2 { get; set; }
    public string Value3 { get; set; }
    public byte[] Value4 { get; set; }
        
    public void SerializeStream<TStream>(TStream writer) where TStream : Stream
    {
        writer.WriteValue(Value1);
        writer.WriteValue(Value2);
        writer.WriteValue(Value3);
        writer.WriteValue(Value4);
    }

    public void DeserializeStream<TStream>(TStream reader) where TStream : Stream
    {
        Value1 = reader.ReadInt();
        Value2 = reader.ReadString();
        Value3 = reader.ReadString();
        Value4 = reader.ReadSequence();
    }

    
    public override string ToString()
    {
        return $"{{ Value1 = {Value1}, Value2 = {Value2}, Value3 = {Value3}, Value4 = {Value4.ToHexString()} }}";
    }
}

public class EmbeddedServer
{
    public static async Task Main()
    {
        const int port = TcpServer.DefaultPort + 42;
        ColumnSchemaSpecifications[] columns = {
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
        string publicKey;
        string privateKey;
        using (var rsa = new RSACryptoServiceProvider())
        {
            publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
            privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        }
        var connectionSettings = new AstraConnectionSettings
        {
            Address = "127.0.0.1",
            Port = port,
            Schema = new()
            {
                Columns = columns
            },
            PrivateKey = privateKey
        };
        var server = new TcpServer(new()
        {
            LogLevel = "information",
            Port = port,
            Schema = new()
            {
                Columns = columns
            }
        }, AuthenticationHelper.RSA(publicKey));
        var logger = server.GetLogger<EmbeddedServer>();
        var table = new AstraTable<int, string, string, byte[]>();
        var serverTask = Task.Run(server.RunAsync);
        await Task.Delay(100);
        
        logger.LogInformation("Example: Embedded TCP server with Public-private key authentication");
        
        using var client = new SimpleAstraClient();
        await client.ConnectAsync(connectionSettings);
        await client.BulkInsertSerializableAsync(new SimpleSerializableStruct[]
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
        var fetch1 = await client.AggregateAsync<SimpleSerializableStruct>(
            table.Column1.EqualsLiteral(2));
        var count1 = 0;
        logger.LogInformation("Fetch 1: col1 == 2");
        foreach (var f in fetch1)
        {
            logger.LogInformation("{}", f);
            count1++;
        }
        logger.LogInformation("Fetched rows count: {}", count1);
        
        logger.LogInformation("Fetch 2: col1 == 2 AND col3 == 'test3'");
        var fetch2 = await client.AggregateAsync<SimpleSerializableStruct>(
            table.Column1.EqualsLiteral(2).And(table.Column3.EqualsLiteral("🇵🇱")));
        var count2 = 0;
        foreach (var f in fetch2)
        {
            logger.LogInformation("{}", f);
            count2++;
        }
        logger.LogInformation("Fetched rows count: {}", count2);
        
        logger.LogInformation("Fetch 3: col1 == 2 OR col3 == 'test3'");
        var fetch3 = await client.AggregateAsync<SimpleSerializableStruct>(
            table.Column1.EqualsLiteral(2).Or(table.Column3.EqualsLiteral("🇵🇱")));
        var count3 = 0;
        foreach (var f in fetch3)
        {
            logger.LogInformation("{}", f);
            count3++;
        }
        
        logger.LogInformation("Fetched rows count: {}", count3);
        
        logger.LogInformation("Current rows count: {}", await client.CountAllAsync());
        logger.LogInformation("Deleting: col1 == 2");
        var deleted = await client.ConditionalDeleteAsync(
            table.Column1.EqualsLiteral(2));
        logger.LogInformation("Affected: {} row(s)", deleted);
        logger.LogInformation("Current rows count: {}", await client.CountAllAsync());
        
        server.Kill();
        await serverTask;
    }
}