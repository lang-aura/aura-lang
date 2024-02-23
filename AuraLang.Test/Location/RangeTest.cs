using AuraLang.Location;
using Range = AuraLang.Location.Range;

namespace AuraLang.Test.Location;

public class RangeTest
{
	[Test]
	public void TestRange_Init()
	{
		// Arrange
		var start = new Position(5, 0);
		var end = new Position(10, 3);
		// Act
		var range = new Range(start, end);
		// Assert
		Assert.Multiple(
			() =>
			{
				Assert.That(range, Is.Not.Null);
				Assert.That(range.Start, Is.EqualTo(start));
				Assert.That(range.End, Is.EqualTo(end));
			}
		);
	}

	[Test]
	public void TestRange_Init_ZeroValue()
	{
		// Arrange
		var position = new Position(0, 0);
		// Act
		var range = new Range();
		// Assert
		Assert.Multiple(
			() =>
			{
				Assert.That(range, Is.Not.Null);
				Assert.That(range.Start, Is.EqualTo(position));
				Assert.That(range.End, Is.EqualTo(position));
			}
		);
	}

	[Test]
	public void TestRange_Contains()
	{
		// Arrange
		var start = new Position();
		var end = new Position(10, 10);
		var position = new Position(5, 5);
		// Act
		var range = new Range(start, end);
		// Assert
		Assert.That(range.Contains(position), Is.True);
	}

	[Test]
	public void TestRange_DoesNotContain()
	{
		// Arrange
		var start = new Position();
		var end = new Position(10, 10);
		var position = new Position(15, 15);
		// Act
		var range = new Range(start, end);
		// Assert
		Assert.That(range.Contains(position), Is.False);
	}

	[Test]
	public void TestRange_Contains_Start()
	{
		// Arrange
		var start = new Position();
		var end = new Position(10, 10);
		var position = new Position();
		// Act
		var range = new Range(start, end);
		// Assert
		Assert.That(range.Contains(position), Is.True);
	}

	[Test]
	public void TestRange_DoesNotContain_End()
	{
		// Arrange
		var start = new Position();
		var end = new Position(10, 10);
		var position = new Position(10, 10);
		// Act
		var range = new Range(start, end);
		// Assert
		Assert.That(range.Contains(position), Is.False);
	}
}
