﻿using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using MirrorSharp.Advanced;
using MirrorSharp.Internal;
using MirrorSharp.Testing;
using MirrorSharp.Testing.Results;
using Moq;
using Xunit;

// ReSharper disable HeapView.ClosureAllocation

namespace MirrorSharp.Tests {
    using static CommandIds;

    public class SetOptionsHandlerTests {
        [Theory]
        [InlineData(LanguageNames.CSharp)]
        [InlineData(LanguageNames.VisualBasic)]
        public async void ExecuteAsync_UpdatesSessionLanguage(string languageName) {
            var driver = MirrorSharpTestDriver.New(new MirrorSharpOptions().EnableVisualBasic());
            await driver.SendAsync(SetOptions, "language=" + languageName);
            Assert.Equal(languageName, driver.Session.Language.Name);
        }

        [Theory]
        [InlineData("debug",   OptimizationLevel.Debug)]
        [InlineData("release", OptimizationLevel.Release)]
        public async void ExecuteAsync_UpdatesSessionCompilationOptimizationsLevel(string value, OptimizationLevel expectedLevel) {
            var driver = MirrorSharpTestDriver.New();
            await driver.SendAsync(SetOptions, "optimize=" + value);
            Assert.Equal(expectedLevel, driver.Session.Roslyn.Project.CompilationOptions.OptimizationLevel);
        }

        [Fact]
        public async void ExecuteAsync_PreservesSessionWorkspace_WhenUpdatingOptimizeToTheSameValue() {
            var driver = MirrorSharpTestDriver.New().SetText("test");
            driver.Session.ChangeOptimizationLevel(OptimizationLevel.Release);
            var workspace = driver.Session.Roslyn.Workspace;
            await driver.SendAsync(SetOptions, "optimize=release");
            Assert.Same(workspace, driver.Session.Roslyn.Workspace);
        }

        [Fact]
        public async void ExecuteAsync_PreservesSessionSourceText_WhenUpdatingOptions() {
            var driver = MirrorSharpTestDriver.New().SetText("test");
            await driver.SendAsync(SetOptions, "optimize=debug");
            Assert.Equal("test", driver.Session.GetText());
        }

        [Fact]
        public async void ExecuteAsync_PreservesSessionCursorPosition_WhenUpdatingOptions() {
            var driver = MirrorSharpTestDriver.New().SetTextWithCursor("test|");
            await driver.SendAsync(SetOptions, "optimize=debug");
            Assert.Equal(4, driver.Session.CursorPosition);
        }

        [Fact]
        public async void ExecuteAsync_CallsSetOptionExtension_IfOptionHasExtensionPrefix() {
            var extensionMock = new Mock<ISetOptionsFromClientExtension>();
            extensionMock.SetReturnsDefault(true);

            var driver = MirrorSharpTestDriver.New(new MirrorSharpOptions { SetOptionsFromClient = extensionMock.Object });
            await driver.SendAsync(SetOptions, "x-testkey=testvalue");
            extensionMock.Verify(x => x.TrySetOption(driver.Session, "x-testkey", "testvalue"));
        }

        [Fact]
        public async void ExecuteAsync_EchoesOptionsIncludingPreviousCalls() {
            var extensionMock = new Mock<ISetOptionsFromClientExtension>();
            extensionMock.SetReturnsDefault(true);

            var driver = MirrorSharpTestDriver.New(new MirrorSharpOptions { SetOptionsFromClient = extensionMock.Object });
            await driver.SendAsync(SetOptions, "optimize=release,x-key1=value1");
            var optionsEcho = await driver.SendAsync<OptionsEchoResult>(SetOptions, "x-key2=value2");
            Assert.Equal(
                new Dictionary<string, string> {
                    ["optimize"] = "release",
                    ["x-key1"] = "value1",
                    ["x-key2"] = "value2"
                },
                optionsEcho.Options
            );
        }
    }
}
