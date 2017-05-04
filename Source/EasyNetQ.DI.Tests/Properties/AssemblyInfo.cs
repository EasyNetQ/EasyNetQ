using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("EasyNetQ.DI.Tests")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("EasyNetQ.DI.Tests")]
[assembly: AssemblyCopyright("Copyright ©  2013")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("cf3af8a8-79c8-4d1a-b747-4e9dabf50f3b")]

[assembly: AssemblyVersion("2.0.4.0")]
[assembly: AssemblyInformationalVersion("2.0.4-netcore.1435+Branch.feature/netcore.Sha.a997be04162dfa34c99681e265733c91c0d564f3")]
[assembly: AssemblyFileVersion("2.0.4.0")]

// NOTE: Forcing xUnit to not run tests in parallel. This is because the 
// tests call RegisterAsEasyNetQContainerFactory which results in calling
// static method RabbitHutch.SetContainerFactory.  As a result, the same 
// ConnectionConfiguration can be added twice to the same static function.
// This results in a Castle.Windsor.ComponentRegistrationException.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
