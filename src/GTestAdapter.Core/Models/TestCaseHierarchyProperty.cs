using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Remote.Models
{
    public sealed class TestCaseHierarchyProperty : TestProperty
    {
        public TestCaseHierarchyProperty(string @namespace,
                                         string @className,
                                         string methodName,
                                         string? container = null)
            : base(serialize(container, @namespace, @className, methodName))
        {
            this.Namespace = @namespace;
            this.ClassName = className;
            this.MethodName = methodName;
            this.Container = container;
        }

        public string? Container { get; set; }
        public string MethodName { get; set; }
        public string ClassName { get; set; }
        public string Namespace { get; set; }

        private static string serialize(string? container,
                                        string @namespace,
                                        string @class,
                                        string method)
        {
            var str = string.Join("|", new[] {@namespace, @class, method});
            if (container.IsPresent())
            {
                str = $"{container}|{str}";
            }
            return str;
        }
    }
}
