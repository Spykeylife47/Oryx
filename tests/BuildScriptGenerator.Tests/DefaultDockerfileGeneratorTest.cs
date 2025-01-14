﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Oryx.BuildScriptGenerator.DotNetCore;
using Microsoft.Oryx.BuildScriptGenerator.Exceptions;
using Microsoft.Oryx.BuildScriptGenerator.Node;
using Microsoft.Oryx.Tests.Common;
using Xunit;

namespace Microsoft.Oryx.BuildScriptGenerator.Tests
{
    public class DefaultDockerfileGeneratorTest : IClassFixture<TestTempDirTestFixture>
    {
        private const string _buildImageFormat = "mcr.microsoft.com/oryx/build:{0}";
        private const string _argRuntimeFormat = "ARG RUNTIME={0}:{1}";

        private const string _buildImageTag = "azfunc-jamstack";
        private const string _runtimeImageTag = "dynamic";

        private readonly string _tempDirRoot;

        public DefaultDockerfileGeneratorTest(TestTempDirTestFixture testFixture)
        {
            _tempDirRoot = testFixture.RootDirPath;
        }

        [Fact]
        public void GenerateDockerfile_Throws_IfNoPlatformIsCompatible()
        {
            // Arrange
            var commonOptions = new BuildScriptGeneratorOptions();
            var generator = CreateDefaultDockerfileGenerator(platforms: new IProgrammingPlatform[] { }, commonOptions);
            var ctx = CreateDockerfileContext();

            // Act & Assert
            var exception = Assert.Throws<UnsupportedPlatformException>(
                () => generator.GenerateDockerfile(ctx));
        }

        [Theory]
        [InlineData("dotnet", "2.0")]
        [InlineData("dotnet", "2.1")]
        [InlineData("dotnet", "3.0")]
        [InlineData("nodejs", "6")]
        [InlineData("nodejs", "8")]
        [InlineData("nodejs", "10")]
        [InlineData("nodejs", "12")]
        [InlineData("php", "5.6")]
        [InlineData("php", "7.3")]
        [InlineData("python", "2.7")]
        [InlineData("python", "3.7")]
        [InlineData("python", "3.8")]
        public void GenerateDockerfile_GeneratesBuildTagAndRuntime_ForProvidedPlatformAndVersion(
            string platformName,
            string platformVersion)
        {
            // Arrange
            var detector = new TestPlatformDetectorUsingPlatformName(
                detectedPlatformName: platformName,
                detectedPlatformVersion: platformVersion);
            var platform = new TestProgrammingPlatform(
                platformName,
                new[] { platformVersion },
                detector: detector);
            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = platformName,
                PlatformVersion = platformVersion
            };
            var generator = CreateDefaultDockerfileGenerator(platform, commonOptions);
            var ctx = CreateDockerfileContext();

            // Act
            var dockerfile = generator.GenerateDockerfile(ctx);

            // Assert
            Assert.NotNull(dockerfile);
            Assert.NotEqual(string.Empty, dockerfile);
            Assert.Contains(string.Format(_buildImageFormat, _buildImageTag), dockerfile);
            Assert.Contains(string.Format(_argRuntimeFormat,
                ConvertToRuntimeName(platformName),
                _runtimeImageTag),
                dockerfile);
            Assert.True(detector.DetectInvoked);
        }

        [Theory]
        [InlineData("dotnet", "2.0")]
        [InlineData("dotnet", "2.1")]
        [InlineData("dotnet", "3.0")]
        [InlineData("nodejs", "6")]
        [InlineData("nodejs", "8")]
        [InlineData("nodejs", "10")]
        [InlineData("nodejs", "12")]
        [InlineData("php", "5.6")]
        [InlineData("php", "7.3")]
        [InlineData("python", "2.7")]
        [InlineData("python", "3.7")]
        [InlineData("python", "3.8")]
        public void GenerateDockerfile_GeneratesBuildTagAndRuntime_ForProvidedPlatform(
            string platformName,
            string detectedPlatformVersion)
        {
            // Arrange
            var detector = new TestPlatformDetectorUsingPlatformName(
                detectedPlatformName: platformName,
                detectedPlatformVersion: detectedPlatformVersion);
            var platform = new TestProgrammingPlatform(
                platformName,
                new[] { detectedPlatformVersion },
                detector: detector);
            var commonOptions = new BuildScriptGeneratorOptions
            {
                PlatformName = platformName,
            };
            var generator = CreateDefaultDockerfileGenerator(platform, commonOptions);
            var ctx = CreateDockerfileContext();

            // Act
            var dockerfile = generator.GenerateDockerfile(ctx);

            // Assert
            Assert.NotNull(dockerfile);
            Assert.NotEqual(string.Empty, dockerfile);
            Assert.Contains(string.Format(_buildImageFormat, _buildImageTag), dockerfile);
            Assert.Contains(string.Format(_argRuntimeFormat,
                ConvertToRuntimeName(platformName),
                _runtimeImageTag),
                dockerfile);
            Assert.True(detector.DetectInvoked);
        }

        [Theory]
        [InlineData("dotnet", "2.0")]
        [InlineData("dotnet", "2.1")]
        [InlineData("dotnet", "3.0")]
        [InlineData("nodejs", "6")]
        [InlineData("nodejs", "8")]
        [InlineData("nodejs", "10")]
        [InlineData("nodejs", "12")]
        [InlineData("php", "5.6")]
        [InlineData("php", "7.3")]
        [InlineData("python", "2.7")]
        [InlineData("python", "3.7")]
        [InlineData("python", "3.8")]
        public void GenerateDockerfile_GeneratesBuildTagAndRuntime_ForNoProvidedPlatform(
            string detectedPlatformName,
            string detectedPlatformVersion)
        {
            // Arrange
            var detector = new TestPlatformDetectorUsingPlatformName(
                detectedPlatformName: detectedPlatformName,
                detectedPlatformVersion: detectedPlatformVersion);
            var platform = new TestProgrammingPlatform(
                detectedPlatformName,
                new[] { detectedPlatformVersion },
                detector: detector);
            var commonOptions = new BuildScriptGeneratorOptions();
            var generator = CreateDefaultDockerfileGenerator(platform, commonOptions);
            var ctx = CreateDockerfileContext();

            // Act
            var dockerfile = generator.GenerateDockerfile(ctx);

            // Assert
            Assert.NotNull(dockerfile);
            Assert.NotEqual(string.Empty, dockerfile);
            Assert.Contains(string.Format(_buildImageFormat, _buildImageTag), dockerfile);
            Assert.Contains(string.Format(_argRuntimeFormat,
                ConvertToRuntimeName(detectedPlatformName),
                _runtimeImageTag),
                dockerfile);
            Assert.True(detector.DetectInvoked);
        }

        [Theory]
        [InlineData("nodejs", "8", "dotnet", "2.1")]
        [InlineData("nodejs", "8", "dotnet", "3.0")]
        [InlineData("nodejs", "12", "dotnet", "2.1")]
        [InlineData("nodejs", "12", "dotnet", "3.0")]
        [InlineData("nodejs", "8", "python", "3.7")]
        [InlineData("nodejs", "8", "python", "2.7")]
        [InlineData("python", "3.7", "dotnet", "2.1")]
        [InlineData("python", "3.7", "dotnet", "3.0")]
        [InlineData("dotnet", "2.1", "php", "5.6")]
        public void GenerateDockerfile_GeneratesBuildTagAndRuntime_ForMultiPlatformBuild(
            string platformName,
            string platformVersion,
            string runtimePlatformName,
            string runtimePlatformVersion)
        {
            // Arrange
            var detector = new TestPlatformDetectorUsingPlatformName(
                detectedPlatformName: platformName,
                detectedPlatformVersion: platformVersion);
            var platform = new TestProgrammingPlatform(
                platformName,
                new[] { platformVersion },
                detector: detector);

            var runtimeDetector = new TestPlatformDetectorUsingPlatformName(
                detectedPlatformName: runtimePlatformName,
                detectedPlatformVersion: runtimePlatformVersion);
            var runtimePlatform = new TestProgrammingPlatform(
                runtimePlatformName,
                new[] { runtimePlatformVersion },
                detector: runtimeDetector);
            var commonOptions = new BuildScriptGeneratorOptions
            {
                EnableMultiPlatformBuild = true,
                PlatformName = platformName,
                PlatformVersion = platformVersion,
                RuntimePlatformName = runtimePlatformName,
                RuntimePlatformVersion = runtimePlatformVersion,
            };
            var generator = CreateDefaultDockerfileGenerator(new[] { platform, runtimePlatform }, commonOptions);
            var ctx = CreateDockerfileContext();

            // Act
            var dockerfile = generator.GenerateDockerfile(ctx);

            // Assert
            Assert.NotNull(dockerfile);
            Assert.NotEqual(string.Empty, dockerfile);
            Assert.Contains(string.Format(_buildImageFormat, _buildImageTag), dockerfile);
            Assert.Contains(string.Format(_argRuntimeFormat,
                ConvertToRuntimeName(runtimePlatformName),
                runtimePlatformVersion),
                dockerfile);
        }

        private DockerfileContext CreateDockerfileContext()
        {
            return new DockerfileContext();
        }

        private DefaultDockerfileGenerator CreateDefaultDockerfileGenerator(
            IProgrammingPlatform platform,
            BuildScriptGeneratorOptions commonOptions)
        {
            return CreateDefaultDockerfileGenerator(new[] { platform }, commonOptions);
        }

        private DefaultDockerfileGenerator CreateDefaultDockerfileGenerator(
            IProgrammingPlatform[] platforms,
            BuildScriptGeneratorOptions commonOptions)
        {
            commonOptions = commonOptions ?? new BuildScriptGeneratorOptions();
            return new DefaultDockerfileGenerator(
                new DefaultCompatiblePlatformDetector(
                    platforms,
                    NullLogger<DefaultCompatiblePlatformDetector>.Instance,
                    Options.Create(commonOptions)),
                NullLogger<DefaultDockerfileGenerator>.Instance,
                Options.Create(commonOptions));
        }

        private string ConvertToRuntimeName(string platformName)
        {
            if (string.Equals(platformName, DotNetCoreConstants.PlatformName, StringComparison.OrdinalIgnoreCase))
            {
                platformName = "dotnetcore";
            }

            if (string.Equals(platformName, NodeConstants.PlatformName, StringComparison.OrdinalIgnoreCase))
            {
                platformName = "node";
            }

            return platformName;
        }
    }
}
