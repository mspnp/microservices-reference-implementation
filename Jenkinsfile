#!/usr/bin/env groovy

pipeline {
    agent any
	tools {
		maven 'Maven 3.5.0'
		jdk 'jdk8'
	}
    stages {
		stage('Build maven image...'){
			steps{
				sh '''
					if [ -z "$(docker images -q my-maven:1)" ];	then
						echo 'Maven image my-maven:1 not found. Building one...'
						docker build -t my-maven:1 -f dockerfile_build_maven .
					fi
				'''
			}
		}
        stage('Build delivery scheduler service...') {
            steps {
				sh 'docker run --rm --name akka-scheduler-jar -v $PWD/src/bc-shipping/scheduler:/usr/app -v $PWD/src/bc-shipping/scheduler/target:/usr/app/target -w /usr/app my-maven:1 mvn package'
            }
        }
		stage('Build and push delivery ingestion and scheduler images...'){
			steps{
				script{
						docker.withRegistry('', '38c49f33-15c3-4749-99a0-a9a607da3653') {
							def app = docker.build("kirpasingh/poltergeist:akka-scheduler${env.BUILD_ID}", "-f ./src/bc-shipping/scheduler/Dockerfile ./src/bc-shipping/scheduler")
							app.push()
							
							//def app1 = docker.build("kirpasingh/poltergeist:rest-api${env.BUILD_ID}", "-f ./deliveryscheduler.delivery-scheduler-rest-api/Dockerfile ./deliveryscheduler.delivery-scheduler-rest-api")
							//app1.push()
						}
				}
			}
        }
    }
}
