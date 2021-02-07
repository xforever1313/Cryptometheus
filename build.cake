// ---------------- Addins ----------------

#addin nuget:?package=Cake.ArgumentBinder&version=0.2.2

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
    ( context ) =>
    {
        PushDocker( linuxX64DockerArch, context );
    }
).Description( "Pushes the linux x64 image.  Requires a user to be logged in first." );

Task( "docker_push_linux_arm" )
.Does(
    ( context ) =>
    {
        PushDocker( linuxArmDockerArch, context );
    }
).Description( "Pushes the linux arm image.  Requires a user to be logged in first." );

Task( "docker_push_manifest" )
.Does(
    ( context ) =>
    {
        using( DockerLogin login = new DockerLogin( context ) )
        {
            login.Login();
            foreach( string tag in dockerTags )
            {
                string arguments = $"manifest push {dockerImageName}:{tag}";
                ProcessArgumentBuilder argumentsBuilder = ProcessArgumentBuilder.FromString( arguments );
                RunDocker( arguments );
            }
        }
    }
).Description( "Pushes the manifest image.  Requires a user to be logged in first." );

void PushDocker( string arch, ICakeContext context )
{
    using( DockerLogin login = new DockerLogin( context ) )
    {
        login.Login();
        foreach( string tag in dockerTags )
        {
            string arguments = $"push {dockerImageName}:{tag}-{arch}";
            ProcessArgumentBuilder argumentsBuilder = ProcessArgumentBuilder.FromString( arguments );
            RunDocker( arguments );
        }
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

class DockerLoginConfig
{
    // ---------------- Properties ----------------

    [StringArgument(
        "username_env_var",
        Description = "Environment Variable that contains the username to login as.",
        DefaultValue = ""
    )]
    public string UsernameEnvVar { get; set; }

    [StringArgument(
        "password_env_var",
        Description = "Environment Variable that contains the password to login as.",
        DefaultValue = ""
    )]
    public string PasswordEnvVar { get; set; }

    [StringArgument(
        "new_docker_config_path",
        Description = "Where to put the new docker config path when loggin in.",
        DefaultValue = ""
    )]
    public string NewDockerConfigPath { get; set; }

    // ---------------- Functions ----------------

    public bool ShouldLogin()
    {
        return
            ( string.IsNullOrWhiteSpace( this.UsernameEnvVar ) == false ) &&
            ( string.IsNullOrWhiteSpace( this.PasswordEnvVar ) == false );
    }
}

class DockerLogin : IDisposable
{
    // ---------------- Fields ----------------

    private const string dockerConfigVar = "DOCKER_CONFIG";

    private readonly ICakeContext context;
    private readonly DockerLoginConfig config;
    private readonly DirectoryPath newDockerConfigPath;
    private string oldDockerConfigPath;

    // ---------------- Constructor ----------------

    public DockerLogin( ICakeContext context ) :
        this( context, context.CreateFromArguments<DockerLoginConfig>() )
    {
    }

    public DockerLogin( ICakeContext context, DockerLoginConfig config )
    {
        this.config = config;
        this.context = context;

        if( string.IsNullOrWhiteSpace( this.config.NewDockerConfigPath ) == false )
        {
            this.newDockerConfigPath = new DirectoryPath( this.config.NewDockerConfigPath );
        }
    }

    // ---------------- Functions ----------------

    public void Login()
    {
        if( this.newDockerConfigPath != null )
        {
            this.context.Information( $"Setting {dockerConfigVar} to {this.newDockerConfigPath}" );
            this.oldDockerConfigPath = this.context.EnvironmentVariable<string>( dockerConfigVar, string.Empty );
            System.Environment.SetEnvironmentVariable( dockerConfigVar, this.newDockerConfigPath.ToString() );
            this.context.EnsureDirectoryExists( this.newDockerConfigPath );
            this.context.CleanDirectory( this.newDockerConfigPath );
        }

        if( this.config.ShouldLogin() == false )
        {
            this.context.Information( "Skipping Login" );
            return;
        }

        string userName = this.context.EnvironmentVariable<string>( this.config.UsernameEnvVar, string.Empty );
        string password = this.context.EnvironmentVariable<string>( this.config.PasswordEnvVar, string.Empty );

        string arguments = $"login --username {userName} --password-stdin";
        System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo
        {
            Arguments = arguments,
            CreateNoWindow = true,
            FileName = "docker",
            RedirectStandardInput = true,
            UseShellExecute = false
        };

        using( System.Diagnostics.Process process = new System.Diagnostics.Process() )
        {
            process.StartInfo = info;
            process.Start();
            process.StandardInput.Write( password );
            process.StandardInput.Close();
            process.WaitForExit();

            int exitCode = process.ExitCode;
            if( exitCode != 0 )
            {
                throw new ApplicationException(
                    "Error when running docker.  Got exit code: " + exitCode
                );
            }
        }

        this.context.Information( "Logged in!" );
    }

    public void Dispose()
    {
        if( this.oldDockerConfigPath != null )
        {
            System.Environment.SetEnvironmentVariable( dockerConfigVar, this.oldDockerConfigPath.ToString() );
            this.context.Information( $"Setting {dockerConfigVar} to {this.oldDockerConfigPath}" );
        }

        if( this.newDockerConfigPath != null && this.context.DirectoryExists( this.newDockerConfigPath ) )
        {
            DeleteDirectorySettings settings = new DeleteDirectorySettings
            {
                Force = true,
                Recursive = true
            };
            this.context.DeleteDirectory( this.newDockerConfigPath, settings );
        }
    }
}

// ---------------- Run Targets ----------------

Task( buildTarget )
    .IsDependentOn( debugTarget );
RunTarget( target );
