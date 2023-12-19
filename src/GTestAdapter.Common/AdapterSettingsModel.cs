using System.Xml;
using System.Xml.Serialization;
using Sodiware;

namespace GoogleTestAdapter.Remote
{
    public enum DeploymentStrategy
    {
        OutDir = 1,
        Outputs = 2
    }
    public sealed class ConnectionId
    {
        [XmlAttribute]
        public bool UpToDate { get; set; }
        public bool UpToDateEnabled => UpToDate;
        [XmlAttribute]
        public required string TargetPath { get; init; }
        [XmlAttribute]
        public required string RemotePath { get; init; }
        [XmlAttribute]
        public int Id { get; init; }
    }
    public sealed class SourceMap
    {
        [XmlAttribute]
        public required string EditorPath { get; init; }
        [XmlAttribute]
        public required string CompilerPath { get; init; }
    }

    [XmlRoot(ElementName = ElementName, Namespace = Namespace)]
    public sealed class AdapterSettingsModel
    {
        public const string ElementName = "RemoteGoogleTestAdapter";
        public const string Namespace = "http://schemas.sodiware.com/developer/remote-google-test-adapter/v1";
        static XmlSerializerNamespaces? m_nss;
        internal static XmlSerializerNamespaces Namespaces
        {
            get
            {
                if (m_nss == null)
                {
                    m_nss = new XmlSerializerNamespaces();
                    m_nss.Add(string.Empty, string.Empty);
                }
                return m_nss;
            }
        }

        public List<SourceMap>? SourceMap { get; set; }
        public List<ConnectionId>? Connections { get; set; }

        [XmlAttribute("DebuggerPipeId")]
        public string? RemoteDebuggerPipeId { get; set; }

        [XmlIgnore]
        public DeploymentStrategy? DeploymentMethod { get; set; }

        [XmlAttribute(nameof(DeploymentMethod))]
        public string? DeploymentMethodString
        {
            get => this.DeploymentMethod.HasValue
                ? this.DeploymentMethod.Value.ToString()
                : null;
            set => this.DeploymentMethod = value.IsMissing()
                ? null
#if !NETFRAMEWORK
                : Enum.Parse<DeploymentStrategy>(value);
#else
                : (DeploymentStrategy)Enum.Parse(typeof(DeploymentStrategy), value);
#endif

        }

        public static AdapterSettingsModel Deserialize(XmlNode node)
            => Deserialize(node.OuterXml);
        public static AdapterSettingsModel Deserialize(string xml)
        {
            var serializer = new XmlSerializer(typeof(AdapterSettingsModel));
            using var ms = new StringReader(xml);
            var model = (AdapterSettingsModel)serializer.Deserialize(ms);
            return model;
        }

        public static string Serialize(AdapterSettingsModel settings)
        {
            var serializer = new XmlSerializer(typeof(AdapterSettingsModel));
            using var ms = new StringWriter();
            serializer.Serialize(ms, settings, Namespaces);
            var doc = new XmlDocument();
            doc.LoadXml(ms.ToString());
            return doc.DocumentElement.OuterXml;
        }
    }
}
