namespace MultiCloud.Test.Unit.Adapter.Platform.Services;

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiCloud.Adapter.Platform.Contracts;
using MultiCloud.Adapter.Platform.Services;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="PlatformDetector"/> class.
/// </summary>
/// <remarks>
/// One test per row of the platform table in docs/SDD.md section 5, then the order the rows are checked in and the
/// values a platform leaves out. The variable names are the table's; they are unverified against the real platforms
/// until each cloud phase checks its own row.
/// </remarks>
[TestClass]
public sealed class PlatformDetectorTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that a detector cannot be built without an environment to read.
    /// </summary>
    [TestMethod]
    public void Constructor_NullEnvironment_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        IProcessEnvironment? environment = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            _ = new PlatformDetector(environment!);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A null environment should be refused with ArgumentNullException.");
    }

    /// <summary>
    /// Unit test to verify that Cloud Run is recognized by its service variable, and that the service and revision
    /// are read while the region and instance, which Cloud Run does not supply, stay absent.
    /// </summary>
    [TestMethod]
    public void Detect_CloudRunVariablesSet_DescribesGoogleCloudRun()
    {
        // Arrange (Given)
        PlatformDetector detector = CreateDetector(new Dictionary<string, string>
        {
            ["K_SERVICE"] = "multi-cloud",
            ["K_REVISION"] = "multi-cloud-00001-abc",
            ["DOTNET_RUNNING_IN_CONTAINER"] = "true",
        });

        // Act (When)
        PlatformDescription description = detector.Detect();

        // Assert (Then)
        Assert.AreEqual(
            new PlatformName("Google Cloud Run"),
            description.Name,
            "The K_SERVICE variable should identify Google Cloud Run.");
        Assert.AreEqual(
            new CloudProvider("gcp"),
            description.CloudProvider,
            "Cloud Run's provider should be OpenTelemetry's gcp.");
        Assert.AreEqual(
            new CloudPlatform("gcp_cloud_run"),
            description.CloudPlatform,
            "Cloud Run's platform should be OpenTelemetry's gcp_cloud_run.");
        Assert.AreEqual(
            new DeployedServiceName("multi-cloud"),
            description.Service,
            "The service's name should be read from K_SERVICE.");
        Assert.AreEqual(
            new RevisionName("multi-cloud-00001-abc"),
            description.Revision,
            "The revision's name should be read from K_REVISION.");
        Assert.IsNull(
            description.Region,
            "Cloud Run supplies no region in the table, so it should be absent.");
        Assert.IsNull(
            description.Instance,
            "Cloud Run supplies no instance name in the table, so it should be absent.");
    }

    /// <summary>
    /// Unit test to verify that ECS is recognized by its metadata variable, and that the region is read.
    /// </summary>
    [TestMethod]
    public void Detect_EcsVariablesSet_DescribesAwsEcs()
    {
        // Arrange (Given)
        PlatformDetector detector = CreateDetector(new Dictionary<string, string>
        {
            ["ECS_CONTAINER_METADATA_URI_V4"] = "http://169.254.170.2/v4/example",
            ["AWS_REGION"] = "us-east-1",
            ["DOTNET_RUNNING_IN_CONTAINER"] = "true",
        });

        // Act (When)
        PlatformDescription description = detector.Detect();

        // Assert (Then)
        Assert.AreEqual(
            new PlatformName("AWS ECS"),
            description.Name,
            "The ECS metadata variable should identify AWS ECS.");
        Assert.AreEqual(
            new CloudProvider("aws"),
            description.CloudProvider,
            "ECS's provider should be OpenTelemetry's aws.");
        Assert.AreEqual(
            new CloudPlatform("aws_ecs"),
            description.CloudPlatform,
            "ECS's platform should be OpenTelemetry's aws_ecs.");
        Assert.AreEqual(
            new CloudRegion("us-east-1"),
            description.Region,
            "The region should be read from AWS_REGION.");
    }

    /// <summary>
    /// Unit test to verify that Kubernetes is recognized and named, that the pod's name is the instance, and that no
    /// cloud is claimed, since the cluster could be anyone's.
    /// </summary>
    [TestMethod]
    public void Detect_KubernetesVariablesSet_DescribesKubernetesWithoutCloud()
    {
        // Arrange (Given)
        PlatformDetector detector = CreateDetector(new Dictionary<string, string>
        {
            ["KUBERNETES_SERVICE_HOST"] = "10.0.0.1",
            ["HOSTNAME"] = "multi-cloud-7d9f8-x2kq",
            ["DOTNET_RUNNING_IN_CONTAINER"] = "true",
        });

        // Act (When)
        PlatformDescription description = detector.Detect();

        // Assert (Then)
        Assert.AreEqual(
            new PlatformName("Kubernetes"),
            description.Name,
            "The Kubernetes service variable should identify Kubernetes.");
        Assert.AreEqual(
            new InstanceName("multi-cloud-7d9f8-x2kq"),
            description.Instance,
            "The instance should be the pod's name, from HOSTNAME.");
        Assert.IsNull(
            description.CloudProvider,
            "A cluster that could be anyone's should claim no cloud provider.");
        Assert.IsNull(
            description.CloudPlatform,
            "A cluster that could be anyone's should claim no cloud platform.");
    }

    /// <summary>
    /// Unit test to verify that a container on no platform the table knows is named as such, with its host name as the
    /// instance.
    /// </summary>
    [TestMethod]
    public void Detect_ContainerVariableSet_DescribesUnidentifiedContainer()
    {
        // Arrange (Given)
        PlatformDetector detector = CreateDetector(new Dictionary<string, string>
        {
            ["DOTNET_RUNNING_IN_CONTAINER"] = "true",
            ["HOSTNAME"] = "3f2a9c1b7e4d",
        });

        // Act (When)
        PlatformDescription description = detector.Detect();

        // Assert (Then)
        Assert.AreEqual(
            new PlatformName("Container, platform not identified"),
            description.Name,
            "The .NET image's container variable alone should name an unidentified container.");
        Assert.AreEqual(
            new InstanceName("3f2a9c1b7e4d"),
            description.Instance,
            "The instance should be the container's host name.");
        Assert.IsNull(
            description.CloudProvider,
            "An unidentified container should claim no cloud provider.");
    }

    /// <summary>
    /// Unit test to verify that, with none of the table's variables set, the application is a local process named by
    /// the machine it runs on.
    /// </summary>
    [TestMethod]
    public void Detect_NoVariablesSet_DescribesLocalProcess()
    {
        // Arrange (Given)
        PlatformDetector detector = CreateDetector(new Dictionary<string, string>());

        // Act (When)
        PlatformDescription description = detector.Detect();

        // Assert (Then)
        Assert.AreEqual(
            new PlatformName("Local process"),
            description.Name,
            "No platform variable should mean a local process.");
        Assert.AreEqual(
            new InstanceName(StubProcessEnvironment.MachineName),
            description.Instance,
            "A local process's instance should be the machine's name.");
        Assert.IsNull(
            description.CloudPlatform,
            "A local process should claim no cloud platform.");
    }

    /// <summary>
    /// Unit test to verify that the rows are checked in the table's order: with every identifying variable set, the
    /// first row, Cloud Run, wins.
    /// </summary>
    [TestMethod]
    public void Detect_EveryIdentifyingVariableSet_ChoosesFirstRow()
    {
        // Arrange (Given)
        PlatformDetector detector = CreateDetector(new Dictionary<string, string>
        {
            ["K_SERVICE"] = "multi-cloud",
            ["ECS_CONTAINER_METADATA_URI_V4"] = "http://169.254.170.2/v4/example",
            ["KUBERNETES_SERVICE_HOST"] = "10.0.0.1",
            ["DOTNET_RUNNING_IN_CONTAINER"] = "true",
        });

        // Act (When)
        PlatformDescription description = detector.Detect();

        // Assert (Then)
        Assert.AreEqual(
            new PlatformName("Google Cloud Run"),
            description.Name,
            "The first row in the table's order should win.");
    }

    /// <summary>
    /// Unit test to verify that a later row is chosen over the container row, which every platform's container also
    /// matches.
    /// </summary>
    [TestMethod]
    public void Detect_KubernetesAndContainerVariablesSet_ChoosesKubernetes()
    {
        // Arrange (Given)
        PlatformDetector detector = CreateDetector(new Dictionary<string, string>
        {
            ["KUBERNETES_SERVICE_HOST"] = "10.0.0.1",
            ["DOTNET_RUNNING_IN_CONTAINER"] = "true",
        });

        // Act (When)
        PlatformDescription description = detector.Detect();

        // Assert (Then)
        Assert.AreEqual(
            new PlatformName("Kubernetes"),
            description.Name,
            "Kubernetes should be checked before the container row.");
    }

    /// <summary>
    /// Unit test to verify that an identifying variable set to white space does not identify its platform.
    /// </summary>
    [TestMethod]
    public void Detect_IdentifyingVariableBlank_SkipsItsRow()
    {
        // Arrange (Given)
        PlatformDetector detector = CreateDetector(new Dictionary<string, string>
        {
            ["K_SERVICE"] = " ",
            ["DOTNET_RUNNING_IN_CONTAINER"] = "true",
        });

        // Act (When)
        PlatformDescription description = detector.Detect();

        // Assert (Then)
        Assert.AreEqual(
            new PlatformName("Container, platform not identified"),
            description.Name,
            "A blank K_SERVICE should not identify Cloud Run.");
    }

    /// <summary>
    /// Unit test to verify that a value the platform's row names but the platform did not set is absent, never
    /// guessed.
    /// </summary>
    [TestMethod]
    public void Detect_EcsWithoutRegion_LeavesRegionAbsent()
    {
        // Arrange (Given)
        PlatformDetector detector = CreateDetector(new Dictionary<string, string>
        {
            ["ECS_CONTAINER_METADATA_URI_V4"] = "http://169.254.170.2/v4/example",
        });

        // Act (When)
        PlatformDescription description = detector.Detect();

        // Assert (Then)
        Assert.AreEqual(
            new PlatformName("AWS ECS"),
            description.Name,
            "The ECS metadata variable alone should still identify AWS ECS.");
        Assert.IsNull(
            description.Region,
            "A region the platform did not set should be absent.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a detector over an environment that holds only the given variables.
    /// </summary>
    /// <param name="variables">The variables that are set, by name.</param>
    /// <returns>The detector.</returns>
    private static PlatformDetector CreateDetector(Dictionary<string, string> variables)
    {
        StubProcessEnvironment environment = new StubProcessEnvironment(variables);
        return new PlatformDetector(environment);
    }

    #endregion Private Methods
}
