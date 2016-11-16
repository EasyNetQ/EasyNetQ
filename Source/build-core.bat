dotnet restore
gitversion /output buildserver /updateassemblyinfo
set basePath=%~dp0
set outputPath=..\..\artifacts

cd %basePath%EasyNetQ
dotnet gitversion
dotnet pack -c Release -o %outputPath%

cd %basePath%EasyNetQ.DI.Autofac
dotnet gitversion
dotnet pack -c Release -o %outputPath%

cd %basePath%EasyNetQ.DI.LightInject
dotnet gitversion
dotnet pack -c Release -o %outputPath%

cd %basePath%EasyNetQ.DI.Ninject
dotnet gitversion
dotnet pack -c Release -o %outputPath%

cd %basePath%EasyNetQ.DI.SimpleInjector
dotnet gitversion
dotnet pack -c Release -o %outputPath%

cd %basePath%EasyNetQ.DI.StructureMap
dotnet gitversion
dotnet pack -c Release -o %outputPath%

cd %basePath%EasyNetQ.DI.Windsor
dotnet gitversion
dotnet pack -c Release -o %outputPath%

cd %basePath%EasyNetQ.Serilog
dotnet gitversion
dotnet pack -c Release -o %outputPath%
