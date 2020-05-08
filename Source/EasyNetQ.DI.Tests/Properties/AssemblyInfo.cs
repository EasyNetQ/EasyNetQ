using Xunit;

// NOTE: Forcing xUnit to not run tests in parallel. This is because the
// tests call RegisterAsEasyNetQContainerFactory which results in calling
// static method RabbitHutch.SetContainerFactory.  As a result, the same
// ConnectionConfiguration can be added twice to the same static function.
// This results in a Castle.Windsor.ComponentRegistrationException.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
