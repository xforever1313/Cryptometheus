// ---------------- Fields ----------------

const string buildTarget = "build";
const string debugTarget = "debug";
const string publishLinuxX64Target = "publish_linux";
const string publishLinuxArmTarget = "publish_linux_arm";
const string dockerLinuxX64Target = "docker_linux";
const string dockerLinuxArmTarget = "docker_linux_arm";

string target = Argument( "target", buildTarget );

const string linuxX64Rid = "linux-x64";
const string linuxArmRid = "linux-arm";

const string linuxX64DockerArch = "linux-amd64";
const string linuxArmDockerArch = "linux-arm";
IEnumerable<string> dockerArchs = new List<string>{
    linuxX64DockerArch,
    linuxArmDockerArch
}.AsReadOnly();

// This is the version of this software,
// update before making a new release.
const string version = "0.0.1";

IEnumerable<string> dockerTags = new List<string>{ "latest", version }.AsReadOnly();
const string dockerImageName = "xforever1313/cryptometheus";

DotNetCoreMSBuildSettings msBuildSettings = new DotNetCoreMSBuildSettings();
// Sets the assembly version.
msBuildSettings.WithProperty( "Version", version )
    .WithProperty( "AssemblyVersion", version )
    .SetMaxCpuCount( System.Environment.ProcessorCount )
    .WithProperty( "FileVersion", version );

DirectoryPath distDir = Directory( "dist" );
DirectoryPath linuxX64DistDir = distDir.Combine( Directory( linuxX64Rid ) );
DirectoryPath linuxArmDistDir = distDir.Combine( Directory( linuxArmRid ) );
FilePath sln = File( "./src/Cryptometheus.sln" );

// ---------------- Build Targets ----------------

Task( debugTarget )
.Does(
    () =>
    {
        DotNetCoreBuildSettings settings = new DotNetCoreBuildSettings
        {
            MSBuildSettings = msBuildSettings
        };
        DotNetCoreBuild( sln.ToString(), settings );
    }
).Description( "Builds with the debug configuration for the current system" );

// ---------------- Publish Targets ----------------

Task( publishLinuxX64Target )
.Does(
    () =>
    {
        Publish( linuxX64Rid, linuxX64DistDir );
    }
).Description( "Publishes for Linux x64" );

Task( publishLinuxArmTarget )
.Does(
    () =>
    {
        Publish( linuxArmRid, linuxArmDistDir );
    }
).Description( "Publishes for Linux Arm" );

void Publish( string runtime, DirectoryPath outputDir )
{
    EnsureDirectoryExists( distDir );
    EnsureDirectoryExists( outputDir );
    CleanDirectory( outputDir );

    DotNetCorePublishSettings settings = new DotNetCorePublishSettings
    {
        OutputDirectory = outputDir,
        Configuration = "Release",
        SelfContained = false,
        MSBuildSettings = msBuildSettings
    };

    if( string.IsNullOrWhiteSpace( runtime ) == false )
    {
        settings.Runtime = runtime;
    }

    DotNetCorePublish( "./src/Cryptometheus/Cryptometheus.csproj", settings );
    CopyFileToDirectory( "./Credits.md", outputDir );
    CopyFileToDirectory( "./Disclaimer.md", outputDir );
    CopyFileToDirectory( "./README.md", outputDir );
    CopyFileToDirectory( "./LICENSE_1_0.txt", outputDir );
}

// ---------------- Docker Targets ----------------

// -------- Build --------

Task( dockerLinuxX64Target )
.Does(
    () =>
    {
        BuildDocker( linuxX64DistDir, linuxX64DockerArch );
    }
).Description( "Builds the Linux x64 docker image" );

Task( dockerLinuxArmTarget )
.Does(
    () =>
    {
        BuildDocker( linuxArmDistDir, linuxArmDockerArch );
    }
).Description( "Builds the Linux arm docker image" );

void BuildDocker( DirectoryPath distDir, string arch )
{
    foreach( string tag in dockerTags )
    {
        string arguments = $"build -t {dockerImageName}:{tag}-{arch} -f docker/linux.dockerfile {distDir}";
        ProcessArgumentBuilder argumentsBuilder = ProcessArgumentBuilder.FromString( arguments );
        RunDocker( arguments );
    }
}

Task( "docker_manifest" )
.Does(
    () =>
    {
        foreach( string tag in dockerTags )
        {
            StringBuilder arguments = new StringBuilder();
            arguments.Append( $"manifest create {dockerImageName}:{tag}" );
            foreach( string arch in dockerArchs )
            {
                arguments.Append( $" {dockerImageName}:{tag}-{arch}" );
            }
            ProcessArgumentBuilder argumentsBuilder = ProcessArgumentBuilder.FromString( arguments.ToString() );
            RunDocker( argumentsBuilder );
        }
    }
).Description( "Creates the docker manifest.  Images must be built AND pushed first." );

// -------- Push --------

Task( "docker_push_linux" )
.Does(
    () =>
    {
        PushDocker( linuxX64DockerArch );
    }
).Description( "Pushes the linux x64 image.  Requires a user to be logged in first." );

Task( "docker_push_linux_arm" )
.Does(
    () =>
    {
        PushDocker( linuxArmDockerArch );
    }
).Description( "Pushes the linux arm image.  Requires a user to be logged in first." );

Task( "docker_push_manifest" )
.Does(
    () =>
    {
        PushDocker( null );
    }
).Description( "Pushes the manifest image.  Requires a user to be logged in first." );

void PushDocker( string arch )
{
    if( string.IsNullOrWhiteSpace( arch ) == false )
    {
        arch = $"-{arch}";
    }
    else
    {
        arch = string.Empty;
    }

    foreach( string tag in dockerTags )
    {
        string arguments = $"push {dockerImageName}:{tag}{arch}";
        ProcessArgumentBuilder argumentsBuilder = ProcessArgumentBuilder.FromString( arguments );
        RunDocker( arguments );
    }
}

void RunDocker( ProcessArgumentBuilder argumentsBuilder )
{
    ProcessSettings settings = new ProcessSettings
    {
        Arguments = argumentsBuilder
    };
    int exitCode = StartProcess( "docker", settings );
    if( exitCode != 0 )
    {
        throw new ApplicationException(
            "Error when running docker.  Got exit code: " + exitCode
        );
    }
}

// ---------------- Run Targets ----------------

Task( buildTarget )
    .IsDependentOn( debugTarget );
RunTarget( target );
