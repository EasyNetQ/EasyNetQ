namespace EasyNetQ.Tests
{
    /// <summary>
    /// Category specifying that the following test is an integration test.
    /// </summary>
    public class IntegrationAttribute : CategoryAttribute
    {
        public IntegrationAttribute()
            : base(Category.Integration)
        { }
    }
}
