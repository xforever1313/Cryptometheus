// https://github.com/xforever1313/X13JenkinsLib
@Library( "X13JenkinsLib" )_

def CallCake( String cmd )
{
    if( isUnix() )
    {
        sh "./Cake/dotnet-cake ./Cryptometheus/build.cake ${cmd}";
    }
    else
    {
        bat ".\\Cake\\dotnet-cake.exe .\\Cryptometheus\\build.cake ${cmd}";
    }
}

def DoDockerPush( String cakeTarget )
{
    withCredentials(
        [usernamePassword(
            credentialsId: "dockerhub",
            usernameVariable: "X13_DOCKER_LOGIN_USERNAME",
            passwordVariable: "X13_DOCKER_LOGIN_PASSWORD"
        )]
    )
    {
        String newConfigPath = pwd() + "/docker_config";
        CallCake(
            "--target=${cakeTarget} --username_env_var=X13_DOCKER_LOGIN_USERNAME --password_env_var=X13_DOCKER_LOGIN_PASSWORD --new_docker_config_path=\"${newConfigPath}\""
        );
    }
}

pipeline
{
    agent none
    parameters
    {
        booleanParam( name: "Build", defaultValue: true, description: "Should we build?" );
        booleanParam( name: "Deploy", defaultValue: true, description: "Should we deploy?" );
    }
    stages
    {
        stage( 'setup' )
        {
            stages
            {
                stage( 'windows' )
                {
                    agent
                    {
                        label "windows";
                    }
                    steps
                    {
                        bat "dotnet tool update Cake.Tool --version 0.37.0 --tool-path .\\Cake"
                        bat 'C:\\"Program Files"\\Docker\\Docker\\DockerCli.exe -Version';
                        bat 'C:\\"Program Files"\\Docker\\Docker\\DockerCli.exe -SwitchLinuxEngine';
                    }
                }
                stage( 'linux_arm' )
                {
                    agent
                    {
                        label "linux && arm32";
                    }
                    steps
                    {
                        sh "~/.dotnet/dotnet tool update Cake.Tool --version 0.37.0 --tool-path ./Cake";
                    }
                }
            }
        }
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
                        stage( 'build_debug' )
                        {
                            steps
                            {
                                CallCake( "--target=build" );
                            }
                        }
                        stage( 'publish_linux_x64' )
                        {
                            steps
                            {
                                CallCake( "--target=publish_linux" );
                            }
                        }
                        stage( 'publish_linux_arm' )
                        {
                            steps
                            {
                                CallCake( "--target=publish_linux_arm" );
                                stash includes: "Cryptometheus\\dist\\linux-arm\\**\\*", name: "linux_arm_build";
                            }
                        }
                        stage( 'build_docker_x64' )
                        {
                            steps
                            {
                                CallCake( "--target=docker_linux" );
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
                                sh "~/.dotnet/dotnet tool update Cake.Tool --version 0.37.0 --tool-path ./Cake";
                            }
                        }
                        stage( "build_docker_arm32" )
                        {
                            steps
                            {
                                unstash "linux_arm_build";
                                CallCake( "--target=docker_linux_arm" );
                            }
                        }
                    }
                }
            }
            when
            {
                expression
                {
                    return params.Build;
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
                            steps
                            {
                                DoDockerPush( "docker_push_linux" );
                            }
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
                            steps
                            {
                                DoDockerPush( "docker_push_linux_arm" );
                            }
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
                            steps
                            {
                                CallCake( "--target=docker_manifest" );
                            }
                        }
                        stage( "deploy" )
                        {
                            steps
                            {
                                DoDockerPush( "docker_push_manifest" );
                            }
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
