using System.Security.Cryptography;
using Astra.Client;
using Astra.Client.Simple;
using Astra.Client.Simple.Aggregator;
using Astra.Common;
using Astra.Common.Data;
using Astra.Common.Hashes;
using Astra.Common.Protocols;
using Astra.Common.StreamUtils;
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
        
    public void SerializeStream<TStream>(TStream writer) where TStream : IStreamWrapper
    {
        writer.SaveValue(Value1);
        writer.SaveValue(Value2);
        writer.SaveValue(Value3);
        writer.SaveValue(Value4);
    }

    public void DeserializeStream<TStream>(TStream reader) where TStream : IStreamWrapper
    {
        Value1 = reader.LoadInt();
        Value2 = reader.LoadString();
        Value3 = reader.LoadString();
        Value4 = reader.LoadBytes();
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
                Indexer = IndexerType.Generic,
            },
        };
        string publicKey;
        string privateKey;
        using (var rsa = new RSACryptoServiceProvider())
        {
            publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
            privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        }
        var connectionSettings = new SimpleAstraClientConnectionSettings
        {
            Address = "127.0.0.1",
            Port = port,
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
        var inserted = await client.BulkInsertSerializableCompatAsync(new SimpleSerializableStruct[]
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
        logger.LogInformation("Inserted: {}", inserted);
        var fetch1 = await client.AggregateCompatAsync<SimpleSerializableStruct>(
            table.Column1.EqualsLiteral(2));
        var count1 = 0;
        logger.LogInformation("Fetch 1: col1 == 2");
        foreach (var f in fetch1)
        {
            logger.LogInformation("{}", f);
            count1++;
        }
        logger.LogInformation("Fetched rows count: {}", count1);
        
        logger.LogInformation("Fetch 2: col1 == 2 AND col3 == '🇵🇱'");
        var fetch2 = await client.AggregateCompatAsync<SimpleSerializableStruct>(
            table.Column1.EqualsLiteral(2).And(table.Column3.EqualsLiteral("🇵🇱")));
        var count2 = 0;
        foreach (var f in fetch2)
        {
            logger.LogInformation("{}", f);
            count2++;
        }
        logger.LogInformation("Fetched rows count: {}", count2);
        
        logger.LogInformation("Fetch 3: col1 == 2 OR col3 == '🇵🇱'");
        var fetch3 = await client.AggregateCompatAsync<SimpleSerializableStruct>(
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