using FluentAssertions;
using Memish_Tutor.Domain.Entities;
using Memish_Tutor.Services.Lessons.Delivery;

namespace Memish_Tutor.Tests.Services
{
    public class LessonMessageFormatterTests
    {
        private readonly LessonMessageFormatter _formatter;

        public LessonMessageFormatterTests()
        {
            _formatter = new LessonMessageFormatter();
        }

        [Fact]
        public void BuildLessonMessage_ShouldFormatCorrectly()
        {
            // Arrange
            var lesson = new Lesson
            {
                Id = Guid.NewGuid(),
                OriginalText = "Hello World",
                HeroPhrase = "World",
                Translation = "Привет мир",
                Explanation = "Simple greeting",
                UsageExample = "Hello World, how are you?",
                Pronunciation = "həˈləʊ wɜːld"
            };

            // Act
            var (caption, followUp) = _formatter.BuildLessonMessage(lesson);

            // Assert
            caption.Should().Contain("📸 Hello World");
            caption.Should().Contain("🎯 World (həˈləʊ wɜːld) — Привет мир");
            caption.Should().Contain("Simple greeting");
            caption.Should().Contain("💬 Примеры:\nHello World, how are you?");
            followUp.Should().BeNull();
        }

        [Fact]
        public void BuildLessonMessage_WithMissingPronunciation_ShouldNotIncludeBrackets()
        {
            // Arrange
            var lesson = new Lesson
            {
                Id = Guid.NewGuid(),
                OriginalText = "Hello",
                HeroPhrase = "Hello",
                Translation = "Привет",
                Explanation = "Greeting",
                UsageExample = "Hello there",
                Pronunciation = null // Missing
            };

            // Act
            var (caption, _) = _formatter.BuildLessonMessage(lesson);

            // Assert
            caption.Should().Contain("🎯 Hello — Привет");
            caption.Should().NotContain("()");
        }
    }
}
