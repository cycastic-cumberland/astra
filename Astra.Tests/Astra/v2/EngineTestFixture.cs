using System.Reflection;
using Astra.Client.Simple.Aggregator;
using Astra.Common.Data;
using Astra.Common.StreamUtils;
using Astra.Engine.Data;
using Astra.Engine.Data.Attributes;
using Astra.Engine.v2.Data;
using Astra.TypeErasure.Planners;
using Microsoft.Extensions.Logging;

namespace Astra.Tests.Astra.v2;

[TestFixture]
public class EngineTestFixture
{
    public struct TinySerializableStruct : IAstraSerializable
    {
        [Indexed(Indexer = IndexerType.BTree)]
        public int Value1 { get; set; }
        [Indexed(Indexer = IndexerType.None)]
        public string Value2 { get; set; }
        [Indexed(Indexer = IndexerType.Generic)]
        public string Value3 { get; set; }

        public void SerializeStream<TStream>(TStream writer) where TStream : IStreamWrapper
        {
            writer.SaveValue(Value1);
            writer.SaveValue(Value2);
            writer.SaveValue(Value3);
        }

        public void DeserializeStream<TStream>(TStream reader) where TStream : IStreamWrapper
        {
            Value1 = reader.LoadInt();
            Value2 = reader.LoadString();
            Value3 = reader.LoadString();
        }
    }
    private ILoggerFactory _loggerFactory = null!;
    private ShinDataRegistry<TinySerializableStruct> _registry = null!;
    private ShinDataRegistry _typeErasedRegistry = null!;
    
    private static ColumnSchemaSpecifications[] GetSchema<T>(IndexerType defaultType)
    {
        return (from pi in TypeHelpers.ToAccessibleProperties<T>()
            let type = DataType.DotnetTypeToAstraType(pi.PropertyType)
            let indexerAttr = pi.GetCustomAttribute<IndexedAttribute>()
            select new ColumnSchemaSpecifications
            {
                Name = pi.Name,
                DataType = type,
                Indexer = indexerAttr?.Indexer ?? defaultType,
            }).ToArray();
    }
    
    [OneTimeSetUp]
    public void Setup()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
        _registry = new(new(), _loggerFactory);
        _typeErasedRegistry = new(new()
        {
            Columns = GetSchema<TinySerializableStruct>(IndexerType.None),
            BinaryTreeDegree = 1_000
        });
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _registry.Dispose();
        _typeErasedRegistry.Dispose();
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
        Assert.That(inserted, Is.EqualTo(3));
        var deserialized = from o in _registry where o.Value1 > 1 && o.Value1 < 3 select o;
        var stage = 1U;
        foreach (var row in deserialized)
        {
            switch (stage)
            {
                case 0:
                {
                    stage++;
                    break;
                }
                case 1:
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(row.Value1, Is.EqualTo(2));
                        Assert.That(row.Value2, Is.EqualTo("🇵🇱"));
                        Assert.That(row.Value3, Is.EqualTo("test4"));
                    });
                    goto case 0U;
                }
                case 2:
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(row.Value1, Is.EqualTo(2));
                        Assert.That(row.Value2, Is.EqualTo("test6"));
                        Assert.That(row.Value3, Is.EqualTo("test4"));
                    });
                    goto case 0U;
                }
                default:
                    Assert.Fail();
                    break;
            }
        }

        var deleted = _registry.Delete(from o in _registry where o.Value1 == 2 select o);
        Assert.That(deleted, Is.EqualTo(2));
        Assert.That(_registry.Count, Is.EqualTo(1));
    }

    [Test]
    public void BenchmarkValidationTest()
    {
        var data = new TinySerializableStruct[10_000 + 5_000];
        for (var i = 0; i < 10_000; i++)
        {
            data[i] = new()
            {
                Value1 = 1,
                Value2 = "test",
                Value3 = i.ToString()
            };
        }

        for (var i = 10_000; i < 10_000 + 5_000; i++)
        {
            data[i] = new()
            {
                Value1 = 2 + i,
                Value2 = "test",
                Value3 = i.ToString()
            };
        }

        Assert.That(_typeErasedRegistry.RowsCount, Is.Zero);
        _typeErasedRegistry.BulkInsertCompat(data);
        Assert.That(_typeErasedRegistry.RowsCount, Is.EqualTo(data.Length));
        using var plan = PhysicalPlanBuilder.Column<int>(0).GreaterThan(0)
            .And(PhysicalPlanBuilder.Column<int>(0).LessThan(2)).Build();
        ref readonly var planRef = ref plan;
        {
            var count = 0;
            var fetched = _typeErasedRegistry.AggregateCompat<TinySerializableStruct>(in planRef);
            foreach (var f in fetched)
            {
                count += 1;
                Assert.That(f.Value1, Is.EqualTo(1));
            }

            Assert.That(count, Is.EqualTo(10_000));
        }
        {
            var count = 0;
            var fetched = _typeErasedRegistry.Aggregate<TinySerializableStruct>(in planRef);
            foreach (var f in fetched)
            {
                count += 1;
                Assert.That(f.Value1, Is.EqualTo(1));
            }

            Assert.That(count, Is.EqualTo(10_000));
        }
    }
}