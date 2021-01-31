// ---------------- Fields ----------------

const string buildTarget = "build";
const string debugTarget = "debug";
const string publishLinux64Target = "publish_linux";
const string publishRaspPi = "publish_pi";
string target = Argument( "target", buildTarget );

const string linuxX64Rid = "linux-x64";
const string linuxArmRid = "linux-arm";

// This is the version of this software,
// update before making a new release.
const string version = "1.0.0";

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

Task( publishLinux64Target )
.Does(
    () =>
    {
        Publish( linuxX64Rid, linuxX64DistDir );
    }
).Description( "Publishes for Linux x64" );

Task( publishRaspPi )
.Does(
    () =>
    {
        Publish( linuxArmRid, linuxArmDistDir );
    }
).Description( "Publishes for Linux Arm" );

// ---------------- Run Targets ----------------

Task( buildTarget )
    .IsDependentOn( debugTarget );
RunTarget( target );
