using AuraLang.Location;
using MsPosition = Microsoft.VisualStudio.LanguageServer.Protocol.Position;

namespace AuraLang.Test.Location;

public class PositionTest
{
	[Test]
	public void TestPosition_Init()
	{
		// Arrange
		const int character = 5;
		const int line = 1;
		// Act
		var position = new Position(character, line);
		// Assert
		MakeAssertions(
			position,
			character,
			line
		);
	}

	[Test]
	public void TestPosition_Init_ZeroValues()
	{
		// Arrange
		const int character = 0;
		const int line = 0;
		// Act
		var position = new Position(character, line);
		// Assert
		MakeAssertions(
			position,
			character,
			line
		);
	}

	[Test]
	public void TestPosition_FromMicrosoftPosition()
	{
		// Arrange
		const int character = 10;
		const int line = 3;
		var msPosition = new MsPosition { Character = character, Line = line };
		// Act
		var position = Position.FromMicrosoftPosition(msPosition);
		// Assert
		MakeAssertions(
			position,
			character,
			line
		);
	}

	[Test]
	public void TestPosition_Equality()
	{
		// Arrange
		const int character = 5;
		const int line = 4;
		// Act
		var position = new Position(character, line);
		var other = new Position(character, line);
		// Assert
		Assert.That(position, Is.EqualTo(other));
	}

	[Test]
	public void TestPosition_Inequality()
	{
		// Act
		var position = new MsPosition(5, 5);
		var other = new MsPosition(1, 1);
		// Assert
		Assert.That(position, Is.Not.EqualTo(other));
	}

	private void MakeAssertions(Position position, int expectedCharacter, int expectedLine)
	{
		Assert.Multiple(
			() =>
			{
				Assert.That(position, Is.Not.Null);
				Assert.That(position.Character, Is.EqualTo(expectedCharacter));
				Assert.That(position.Line, Is.EqualTo(expectedLine));
			}
		);
	}
}
