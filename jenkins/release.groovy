def CallCakeOnWindows( String cmd )
{
    bat ".\\Cake\\dotnet-cake.exe .\\Cryptometheus\\build.cake ${cmd}"
}

def CallCakeOnUnix( String cmd )
{
    sh "./Cake/dotnet-cake ./Cryptometheus/build.cake ${cmd}"
}

pipeline
{
    agent none
    stages
    {
        stage( 'build_linux_x64' )
        {
            agent
            {
                label "windows";
            }
            stages
            {
                stage( 'setup' )
                {
                    steps
                    {
                        bat "dotnet tool update Cake.Tool --tool-path .\\Cake"
                        bat 'C:\\"Program Files"\\Docker\\Docker\\DockerCli.exe -Version';
                        bat 'C:\\"Program Files"\\Docker\\Docker\\DockerCli.exe -SwitchLinuxEngine';
                    }
                }
                stage( 'build_debug' )
                {
                    steps
                    {
                        CallCakeOnWindows( "--target=build" );
                    }
                }
                stage( 'publish_linux_x64' )
                {
                    steps
                    {
                        CallCakeOnWindows( "--target=publish_linux" );
                    }
                }
                stage( 'publish_linux_arm' )
                {
                    steps
                    {
                        CallCakeOnWindows( "--target=publish_linux_arm" );
                        stash includes: "Cryptometheus\\dist\\linux-arm\\**\\*", name: "linux_arm_build";
                    }
                }
                stage( 'build_docker_x64' )
                {
                    steps
                    {
                        CallCakeOnWindows( "--target=docker_linux" );
                    }
                }
            }
        }
        stage( 'build_linux_arm32' )
        {
            agent
            {
                label "linux && arm32";
            }
            stages
            {
                stage( 'setup' )
                {
                    steps
                    {
                        sh "dotnet tool update Cake.Tool --tool-path ./Cake";
                    }
                }
                stage( "build_docker_arm32" )
                {
                    steps
                    {
                        unstash "linux_arm_build";
                        CallCakeOnUnix( "--target=docker_linux_arm" );
                    }
                }
            }
        }
    }
}