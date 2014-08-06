msbuild ..\SimpleProcessRunner.sln /target:Clean
msbuild ..\SimpleProcessRunner.sln /property:Configuration=Release
..\.nuget\nuget pack ..\SimpleProcessRunner\SimpleProcessRunner.csproj -Prop Configuration=Release
@pause