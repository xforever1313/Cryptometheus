// https://github.com/xforever1313/X13JenkinsLib
@Library( "X13JenkinsLib" )_

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
    parameters
    {
        booleanParam( name: "Deploy", defaultValue: true, description "Should we deploy as well?" );
    }
    stages
    {
        stage( 'build' )
        {
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
                                sh "~/.dotnet/dotnet tool update Cake.Tool --tool-path ./Cake";
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
        } // End Build
        stage( 'deploy' )
        {
            stages
            {
                stage( 'deploy_linux_x64' )
                {
                    agent
                    {
                        label "windows";
                    }
                    stages
                    {
                        stage( "Push" )
                        {
                            X13DockerLogin( "dockerhub" );
                            CallCakeOnWindows( "--target=docker_push_linux" );
                        }
                    }
                }
                stage( 'deploy_linux_arm' )
                {
                    agent
                    {
                        label "linux && arm32";
                    }
                    stages
                    {
                        stage( "Push" )
                        {
                            X13DockerLogin( "dockerhub" );
                            CallCakeOnUnix( "--target=docker_push_linux_arm" );
                        }
                    }
                }
                stage( "docker_manifest" )
                {
                    agent
                    {
                        label "linux && arm32";
                    }
                    stages
                    {
                        stage( "Build" )
                        {
                            CallCakeOnUnix( "--target=docker_manifest" );
                        }
                        stage( "deploy" )
                        {
                            X13DockerLogin( "dockerhub" );
                            CallCakeOnUnix( "--target=docker_push_manifest" );
                        }
                    }
                }
            }
            when
            {
                expression
                {
                    return params.Deploy;
                }
            }
        } // End Deploy
    }
}
