using Astra.Engine.v2.Data;
using Astra.TypeErasure;
using Astra.TypeErasure.Data;

namespace Astra.Tests.Misc;

[TestFixture]
public class CellArithmeticTest
{
    private static readonly DataCell Cell1 = new(1);
    private static readonly DataCell Cell2 = new(1L);
    private static readonly DataCell Cell3 = new(1.0f);
    private static readonly DataCell Cell4 = new(1.0);
    
    private static readonly DataCell Cell1A = new(2);
    private static readonly DataCell Cell2A = new(2L);
    private static readonly DataCell Cell3A = new(2.0f);
    private static readonly DataCell Cell4A = new(2.0);
    
    private static readonly DataCell Cell1B = new(-1);
    private static readonly DataCell Cell2B = new(-1L);
    private static readonly DataCell Cell3B = new(-1.0f);
    private static readonly DataCell Cell4B = new(-1.0);
    
    private static readonly DataCell Cell5 = new("Hello World!");
    private static readonly DataCell Cell6 = new([1, 2, 3, 4, 5]);

    [Test]
    public void SameTypeEqualityTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Cell1, Is.EqualTo(new DataCell(1)));
            Assert.That(Cell2, Is.EqualTo(new DataCell(1L)));
            Assert.That(Cell3, Is.EqualTo(new DataCell(1.0f)));
            Assert.That(Cell4, Is.EqualTo(new DataCell(1.0)));
        });
        Assert.Multiple(() =>
        {
            Assert.That(Cell5, Is.EqualTo(new DataCell("Hello World!")));
            Assert.That(Cell6, Is.EqualTo(new DataCell([1, 2, 3, 4, 5])));
        });
    }
    
    [Test]
    public void CrossEqualityTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Cell1, Is.EqualTo(Cell2));
            Assert.That(Cell1, Is.EqualTo(Cell3));
            Assert.That(Cell1, Is.EqualTo(Cell4));
        });
        Assert.Multiple(() =>
        {
            Assert.That(Cell2, Is.EqualTo(Cell1));
            Assert.That(Cell2, Is.EqualTo(Cell3));
            Assert.That(Cell2, Is.EqualTo(Cell4));
        });
        Assert.Multiple(() =>
        {
            Assert.That(Cell3, Is.EqualTo(Cell1));
            Assert.That(Cell3, Is.EqualTo(Cell2));
            Assert.That(Cell3, Is.EqualTo(Cell4));
        });
        Assert.Multiple(() =>
        {
            Assert.That(Cell4, Is.EqualTo(Cell1));
            Assert.That(Cell4, Is.EqualTo(Cell2));
            Assert.That(Cell4, Is.EqualTo(Cell3));
        });
    }
    
    [Test]
    public void SameTypeComparisonTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Cell1, Is.GreaterThan(Cell1B));
            Assert.That(Cell2, Is.GreaterThan(Cell2B));
            Assert.That(Cell3, Is.GreaterThan(Cell3B));
            Assert.That(Cell4, Is.GreaterThan(Cell4B));
        });
        
        Assert.Multiple(() =>
        {
            Assert.That(Cell1, Is.LessThan(Cell1A));
            Assert.That(Cell2, Is.LessThan(Cell2A));
            Assert.That(Cell3, Is.LessThan(Cell3A));
            Assert.That(Cell4, Is.LessThan(Cell4A));
        });
    }
    
    [Test]
    public void CrossTypeComparisonTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Cell1, Is.GreaterThan(Cell2B));
            Assert.That(Cell1, Is.GreaterThan(Cell3B));
            Assert.That(Cell1, Is.GreaterThan(Cell4B));
        });
        
        Assert.Multiple(() =>
        {
            Assert.That(Cell2, Is.GreaterThan(Cell1B));
            Assert.That(Cell2, Is.GreaterThan(Cell3B));
            Assert.That(Cell2, Is.GreaterThan(Cell4B));
        });
        
        Assert.Multiple(() =>
        {
            Assert.That(Cell3, Is.GreaterThan(Cell1B));
            Assert.That(Cell3, Is.GreaterThan(Cell2B));
            Assert.That(Cell3, Is.GreaterThan(Cell4B));
        });
        
        Assert.Multiple(() =>
        {
            Assert.That(Cell4, Is.GreaterThan(Cell1B));
            Assert.That(Cell4, Is.GreaterThan(Cell2B));
            Assert.That(Cell4, Is.GreaterThan(Cell3B));
        });
        
        Assert.Multiple(() =>
        {
            Assert.That(Cell1, Is.LessThan(Cell2A));
            Assert.That(Cell1, Is.LessThan(Cell3A));
            Assert.That(Cell1, Is.LessThan(Cell4A));
        });
        
        Assert.Multiple(() =>
        {
            Assert.That(Cell2, Is.LessThan(Cell1A));
            Assert.That(Cell2, Is.LessThan(Cell3A));
            Assert.That(Cell2, Is.LessThan(Cell4A));
        });
        
        Assert.Multiple(() =>
        {
            Assert.That(Cell3, Is.LessThan(Cell1A));
            Assert.That(Cell3, Is.LessThan(Cell2A));
            Assert.That(Cell3, Is.LessThan(Cell4A));
        });
        
        Assert.Multiple(() =>
        {
            Assert.That(Cell4, Is.LessThan(Cell1A));
            Assert.That(Cell4, Is.LessThan(Cell2A));
            Assert.That(Cell4, Is.LessThan(Cell3A));
        });
    }

    [Test]
    public void SameTypeArithmeticTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Cell1A + Cell1B, Is.EqualTo(Cell1));
            Assert.That(Cell2A + Cell2B, Is.EqualTo(Cell2));
            Assert.That(Cell3A + Cell3B, Is.EqualTo(Cell3));
            Assert.That(Cell4A + Cell4B, Is.EqualTo(Cell4));
        });
        Assert.Multiple(() =>
        {
            Assert.That(Cell1A - Cell1, Is.EqualTo(Cell1));
            Assert.That(Cell2A - Cell2, Is.EqualTo(Cell2));
            Assert.That(Cell3A - Cell3, Is.EqualTo(Cell3));
            Assert.That(Cell4A - Cell4, Is.EqualTo(Cell4));
        });
        
        Assert.Multiple(() =>
        {
            Assert.That(-Cell1, Is.EqualTo(Cell1B));
            Assert.That(-Cell2, Is.EqualTo(Cell2B));
            Assert.That(-Cell3, Is.EqualTo(Cell3B));
            Assert.That(-Cell4, Is.EqualTo(Cell4B));
        });
    }
    
    [Test]
    public void CrossTypeArithmeticTest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Cell1A + Cell4B, Is.EqualTo(Cell3));
            Assert.That(Cell2A + Cell1B, Is.EqualTo(Cell4));
            Assert.That(Cell3A + Cell2B, Is.EqualTo(Cell1));
            Assert.That(Cell4A + Cell3B, Is.EqualTo(Cell2));
        });
        Assert.Multiple(() =>
        {
            Assert.That(Cell1A - Cell4, Is.EqualTo(Cell3));
            Assert.That(Cell2A - Cell1, Is.EqualTo(Cell4));
            Assert.That(Cell3A - Cell2, Is.EqualTo(Cell1));
            Assert.That(Cell4A - Cell3, Is.EqualTo(Cell2));
        });
        
        Assert.Multiple(() =>
        {
            Assert.That(-Cell1, Is.EqualTo(Cell2B));
            Assert.That(-Cell2, Is.EqualTo(Cell3B));
            Assert.That(-Cell3, Is.EqualTo(Cell4B));
            Assert.That(-Cell4, Is.EqualTo(Cell1B));
        });
    }

    [Test]
    public void NumericHelpersTest()
    {
        _ = (DataCell)(typeof(DataCell).GetField("MinValue")!.GetValue(null)!);
        _ = (DataCell)(typeof(DataCell).GetField("MaxValue")!.GetValue(null)!);
    }
}