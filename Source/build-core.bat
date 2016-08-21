gitversion /output buildserver /updateassemblyinfo
set basePath=%~dp0
cd %basePath%EasyNetQ
call build.cmd
cd %basePath%EasyNetQ.DI.Autofac
call build.cmd
cd %basePath%EasyNetQ.DI.LightInject
call build.cmd
cd %basePath%EasyNetQ.DI.Ninject
call build.cmd
cd %basePath%EasyNetQ.DI.SimpleInjector
call build.cmd
cd %basePath%EasyNetQ.DI.StructureMap
call build.cmd
cd %basePath%EasyNetQ.DI.Windsor
call build.cmd
cd %basePath%EasyNetQ.Serilog
call build.cmd