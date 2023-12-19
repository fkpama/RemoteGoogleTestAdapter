using System.Runtime.Serialization;
using System.ServiceModel;

namespace GoogleTestAdapter.Remote
{
    public enum DebuggerShutdownReason
    {
        NormalExit = 0,
        Error = 1
    }
    [ServiceContract]
    public interface IDebuggerLauncher
    {
        [OperationContract]
        [FaultContract(typeof(DebuggerFault))]
        void ReportTestResults(TestResultModel[] results);

        [OperationContract]
        [FaultContract(typeof(DebuggerFault))]
        void ReportTestStarted(string[] tests);

        [OperationContract]
        void Shutdown(DebuggerShutdownReason reason);

        [OperationContract]
        void ClientConnected();

        [OperationContract]
        [FaultContract(typeof(DebuggerFault))]
        void OutputReceived(string outputText);

        [OperationContract]
        [FaultContract(typeof(DebuggerFault))]
        void ErrorReceived(string errorText);
    }

    public enum Outcome
    {
        Passed,
        Failed,
        Skipped,
        None,
        NotFound
    }

    [DataContract]
    public class TestResultModel
    {
        [DataMember]
        public string? TestId { get; set; }
        [DataMember]
        public required string FullyQualifiedNameWithNamespace { get; set; }
        [DataMember]
        public required Outcome Outcome { get; init; }
    }

    [DataContract]
    public class DebuggerFault
    {
        [DataMember]
        public string? Message { get; set; }

        public DebuggerFault(string? message)
        {
            this.Message = message;
        }
    }
}