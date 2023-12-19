#if false
using System.Text.Json;
#else
using Newtonsoft.Json;
#endif
using Sodiware;

namespace GoogleTestAdapter.Remote.Debugger
{
    public class TestLaunchBatch
    {
        public int ConnectionId { get; init; }
        public required string ProjectId { get; init; }
        public required string CommandLine { get; init; }
    }
    public class DebuggerLaunchParameters
    {
#if false
        static JsonSerializerOptions? s_options;
        internal static JsonSerializerOptions SerializerOptions => s_options ??= new()
        {
            WriteIndented = true
        };
#else
        static JsonSerializerSettings? s_options;
        internal static JsonSerializerSettings SerializerOptions => s_options ??= new()
        {
        };
#endif

        public string ServiceId { get; init; }
        public int Timeout { get; init; }
        public List<TestLaunchBatch> Batches { get; init; }
        public string? WorkingDir { get; set; }
        public string? BaseDir { get; set; }

        public DebuggerLaunchParameters(Guid serviceId, List<TestLaunchBatch>batches)
        {
            Guard.NotNull(serviceId);
            this.ServiceId = serviceId.ToString();
            this.Batches = batches;
        }
        public DebuggerLaunchParameters()
        {
            this.Batches = new();
            this.ServiceId = null!;
        }

        public string Serialize()
        {
#if false
            return JsonSerializer.Serialize(this, options: SerializerOptions);
#else
            return JsonConvert.SerializeObject(this);
#endif
        }

        public static DebuggerLaunchParameters? Deserialize(string text)
        {
#if false
            return JsonSerializer.Deserialize<DebuggerLaunchParameters>(text, SerializerOptions);
#else
            return JsonConvert.DeserializeObject<DebuggerLaunchParameters>(text);
#endif
        }
    }
}
