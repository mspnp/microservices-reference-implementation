# Instructions for building docker image
1. Make sure maven is installed and in the PATH.
2. Make sure 'Docker for Windows' is installed.
3. Open a command prompt and CD into the project folder. Make sure POM.xml is in the same folder.
4. Run 'mvn clean install'.
5. Run 'docker build -t dispatcher:<version> .' where <version> is the version you want to assign, e.g. v1.0.
6. Run 'docker run -it dispatcher:<version>' where <version> is the version you assigned in the previous step.