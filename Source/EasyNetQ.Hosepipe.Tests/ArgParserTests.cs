// ReSharper disable InconsistentNaming

namespace EasyNetQ.Hosepipe.Tests;

public class ArgParserTests
{
    private readonly ArgParser argParser = new();

    [Fact]
    public void Should_be_able_to_retrieve_args_by_position()
    {
        var args = new[]
        {
            "one",
            "two",
            "three"
        };

        var arguments = argParser.Parse(args);

        var one = "";
        var two = "";
        var three = "";
        var threeFailed = false;

        arguments.At(0, a => one = a.Value).FailWith(() => Assert.Fail("should succeed"));
        arguments.At(1, a => two = a.Value).FailWith(() => Assert.Fail("should succeed"));
        arguments.At(2, a => three = a.Value).FailWith(() => Assert.Fail("should succeed"));
        arguments.At(3, _ => Assert.Fail("Should not be an arg at 3")).FailWith(() => threeFailed = true);

        one.ShouldEqual(args[0]);
        two.ShouldEqual(args[1]);
        three.ShouldEqual(args[2]);
        threeFailed.ShouldBeTrue();
    }

    [Fact]
    public void Should_be_able_to_retrieve_args_by_key()
    {
        var args = new string[]
        {
            "x:one",
            "y:two",
            "z:three",
            "f"
        };

        var arguments = argParser.Parse(args);
        var fNotFound = false;

        arguments.WithKey("z", a => a.Value.ShouldEqual("three")).FailWith(() => Assert.Fail("should succeed"));
        arguments.WithKey("x", a => a.Value.ShouldEqual("one")).FailWith(() => Assert.Fail("should succeed"));
        arguments.WithKey("y", a => a.Value.ShouldEqual("two")).FailWith(() => Assert.Fail("should succeed"));
        arguments.WithKey("f", _ => Assert.Fail()).FailWith(() => fNotFound = true);

        fNotFound.ShouldBeTrue();
    }

    [Fact]
    public void Should_be_able_to_retrieve_args_by_position_and_key()
    {
        var args = new string[]
        {
            "command",
            "abc"
        };

        var arguments = argParser.Parse(args);
        var commandDetected = false;
        var abcDetected = false;

        arguments.At(0, "command", () => commandDetected = true).FailWith(() => Assert.Fail("should succeed"));
        arguments.At(0, "notCommand", () => Assert.Fail("should not succeed"));
        arguments.At(1, "command", () => Assert.Fail("should not succeed"));
        arguments.At(1, "abc", () => abcDetected = true).FailWith(() => Assert.Fail("should succeed"));

        commandDetected.ShouldBeTrue();
        abcDetected.ShouldBeTrue();
    }
}

// ReSharper restore InconsistentNaming
