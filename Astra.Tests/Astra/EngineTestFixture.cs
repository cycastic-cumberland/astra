using Astra.Common;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Common.StreamUtils;
using Astra.Engine.Data;
using Astra.Engine.Data.Attributes;
using Microsoft.Extensions.Logging;

namespace Astra.Tests.Astra;

[TestFixture]
public class EngineTestFixture
{
    public struct TinySerializableStruct
    {
        [Indexed(Indexer = IndexerType.Generic)]
        public int Value1 { get; set; }
        [Indexed(Indexer = IndexerType.None)]
        public string Value2 { get; set; }
        [Indexed(Indexer = IndexerType.Generic)]
        public string Value3 { get; set; }
    }
    private ILoggerFactory _loggerFactory = null!;
    private DataRegistry<TinySerializableStruct> _registry = null!;

    
    [OneTimeSetUp]
    public void Setup()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
        _registry = new(new(), _loggerFactory);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _registry.Dispose();
        _loggerFactory.Dispose();
    }
    [Test]
    public void LocalRegistryTest()
    {
        var inserted = _registry.Add([
            new()
            {
                Value1 = 1,
                Value2 = "test1",
                Value3 = "𝄞",
            },
            new()
            {
                Value1 = 2,
                Value2 = "🇵🇱",
                Value3 = "test4",
            },
            new()
            {
                Value1 = 2,
                Value2 = "test6",
                Value3 = "test4",
            },
        ]);
        Assert.That(inserted, Is.EqualTo(2));
        var deserialized = from o in _registry where o.Value1 == 2 select o;
        var pass = true;
        foreach (var row in deserialized)
        {
            if (!pass) Assert.Fail();
            Assert.Multiple(() =>
            {
                Assert.That(row.Value1, Is.EqualTo(2));
                Assert.That(row.Value2, Is.EqualTo("🇵🇱"));
                Assert.That(row.Value3, Is.EqualTo("test4"));
            });
            pass = false;
        }

        var deleted = _registry.Delete(from o in _registry where o.Value1 == 2 select o);
        Assert.That(deleted, Is.EqualTo(1));
        Assert.That(_registry.Count, Is.EqualTo(1));
    }
}