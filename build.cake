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

// This is the version of this software,
// update before making a new release.
const string version = "0.0.1";

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

Task( dockerLinuxX64Target )
.Does(
    () =>
    {
        BuildDocker( linuxX64DistDir );
    }
).Description( "Builds the Linux x64 docker image" );

Task( dockerLinuxArmTarget )
.Does(
    () =>
    {
        BuildDocker( linuxArmDistDir );
    }
).Description( "Builds the Linux arm docker image" );

void BuildDocker( DirectoryPath distDir )
{
    List<string> tags = new List<string> { "latest", version };
    foreach( string tag in tags )
    {
        string arguments = $"build -t cryptometheus:{tag} -f docker/linux.dockerfile {distDir}";
        ProcessArgumentBuilder argumentsBuilder = ProcessArgumentBuilder.FromString( arguments );
        ProcessSettings settings = new ProcessSettings
        {
            Arguments = argumentsBuilder
        };
        int exitCode = StartProcess( "docker", settings );
        if( exitCode != 0 )
        {
            throw new ApplicationException(
                "Error when running docker to build.  Got error: " + exitCode
            );
        }
    }
}

// ---------------- Run Targets ----------------

Task( buildTarget )
    .IsDependentOn( debugTarget );
RunTarget( target );
