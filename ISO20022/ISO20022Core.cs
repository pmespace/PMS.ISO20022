using System.Globalization;
using System.Collections;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Resources;
using System.Text;
using ISO20022.Properties;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace ISO20022
{
	#region exceptions
	/// <summary>
	/// excaption raised when an invalid value is assigned to an enumerated variable
	/// </summary>
	public class ISO20022XmlSchemaValiderException : Exception
	{
		public ISO20022XmlSchemaValiderException(IList<ValidationEventArgs> t) { Evts = t; }
		IList<ValidationEventArgs> Evts;
		public
	}
	#endregion

	/// <summary>
	/// Object allowing to load a complete XML schema with multiple files and to validate an XML
	/// </summary>
	public class XmlSchemaValider
	{
		#region constructor
		public XmlSchemaValider() { ValidationEvents = new ReadOnlyCollection<ValidationEventArgs>(_validationEvents); }
		#endregion

		#region properties
		/// <summary>
		/// XML schema to use for validation
		/// </summary>
		public XmlSchemaSet XmlSchemaSet { get => _xmlSchemaSet; }
		XmlSchemaSet _xmlSchemaSet = new XmlSchemaSet();
		/// <summary>
		/// List of events while verifying a XML file
		/// </summary>
		public ReadOnlyCollection<ValidationEventArgs> ValidationEvents { get; }
		List<ValidationEventArgs> _validationEvents = new List<ValidationEventArgs>();
		/// <summary>
		/// List of errors while verifying a XML file
		/// </summary>
		public IEnumerable<ValidationEventArgs> ErrorEvents { get => _validationEvents.Where(severity => XmlSeverityType.Error == severity.Severity); }
		/// <summary>
		/// List of warnings while verifying a XML file
		/// </summary>
		public IEnumerable<ValidationEventArgs> WarningEvents { get => _validationEvents.Where(severity => XmlSeverityType.Warning == severity.Severity); }
		#endregion

		#region public XML specific methods
		/// <summary>
		/// Reste the schema set to restart with another set of XSD
		/// </summary>
		public void ResetXmlSchemaSet()
		{
			_xmlSchemaSet = new XmlSchemaSet();
			_validationEvents.Clear();
		}
#if RESOURCES
		/// <summary>
		/// Loads the specified list of XSD stored in resources pointed by <paramref name="resources"/> and stored as string.
		/// If <paramref name="xsds"/> is null or empty the function tries to read all string resources from the <paramref name="resources"/>.
		/// </summary>
		/// <param name="resources">Resources to look into</param>
		/// <param name="xsds">List of names of resources containing XSD to load from resources</param>
		/// <returns>true if the XSD have been loaded without error and warning, false otherwise</returns>
		public bool LoadAndSetXSD(ResourceManager resources, List<string> xsds = default)
		{
			try
			{
				if (default == resources) return false;
				ResourceSet resourceSet = resources.GetResourceSet(CultureInfo.CurrentCulture, true, true);
				if (default == resourceSet) return false;
				XmlSchemaSet = new XmlSchemaSet();
				bool ok = true;
				if (default == xsds || 0 == xsds.Count)
					foreach (DictionaryEntry entry in resourceSet)
						ok &= loadAndSetXSD(resources, entry.Key.ToString());
				else
					foreach (string entry in resourceSet)
						ok &= loadAndSetXSD(resources, entry);
				return ok;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return false;
		}
		/// <summary>
		/// Loads the specified array of XSD stored in resources pointed by <paramref name="resources"/> and stored as string.
		/// If <paramref name="xsds"/> is null or empty the function tries to read all string resources from the <paramref name="resources"/>.
		/// </summary>
		/// <param name="resources">Resources to look into</param>
		/// <param name="xsds">Array of names of resources containing XSD to load from resources</param>
		/// <returns>true if the XSD have been loaded without error and warning, false otherwise</returns>
		public bool LoadAndSetXSD(ResourceManager resources, string[] xsds) => LoadAndSetXSD(resources, new List<string>(xsds));
		/// <summary>
		/// Loads the specified XSD
		/// </summary>
		/// <param name="resources">Resources to lokk into</param>
		/// <param name="xsd">Name of the XSD to load</param>
		/// <returns>true if the XSD has been loaded without error and warning, false otherwise</returns>
		bool loadAndSetXSD(ResourceManager resources, string xsd)
		{
			try
			{
				var byteArray = resources.GetObject(xsd);
				return loadAndSetXSD(byteArray.ToString());
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex, xsd);
			}
			return false;
		}
#endif
		/// <summary>
		/// Loads the specified list of XSD files
		/// </summary>
		/// <param name="xsds">List of XSD files to load from resources</param>
		/// <returns>true if all the XSD have been loaded without error and warning, false otherwise</returns>
		public bool LoadAndSetXSD(string[] xsds) => LoadAndSetXSD(new List<string>(xsds));
		/// <summary>
		/// Loads the specified list of XSD files
		/// </summary>
		/// <param name="xsds">List of XSD files to load from resources</param>
		/// <returns>true if all the XSD have been loaded without error and warning, false otherwise</returns>
		public bool LoadAndSetXSD(List<string> xsds)
		{
			try
			{
				bool ok = true;
				foreach (string entry in xsds)
					ok &= LoadAndSetXSD(entry);
				return ok;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return false;
		}
		/// <summary>
		/// Loads the XSD files stored inside a folder.
		/// </summary>
		/// <param name="folder">Top folder to look for XSD files</param>
		/// <param name="searchPattern">Search pattern to filter files to load, default is "*.xsd"</param>
		/// <param name="searchOption">Indicates whether search must be inside the top folder or must include sub-folders, default is only top folder</param>
		/// <returns>true if all the XSD have been loaded without error and warning, false otherwise</returns>
		public bool LoadAndSetXSD(string folder, string searchPattern = "*.xsd", SearchOption searchOption = SearchOption.TopDirectoryOnly)
		{
			try
			{
				DirectoryInfo di = new DirectoryInfo(folder);
				bool ok = true;
				foreach (FileInfo k in di.EnumerateFiles(searchPattern, searchOption))
					ok &= LoadAndSetXSD(k.FullName);
				return ok;
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return false;
		}
		/// <summary>
		/// Loads the specified XSD file
		/// </summary>
		/// <param name="xsd">Name of the XSD file to load</param>
		/// <returns>true if the XSD file has been loaded without error and warning, false otherwise</returns>
		public bool LoadAndSetXSD(string xsd)
		{
			try
			{
				using (StreamReader sr = new StreamReader(xsd))
				{
					var xmlSchema = new XmlSchema();
					xmlSchema = XmlSchema.Read(sr, null);
					XmlSchemaSet.Add(xmlSchema);
					return true;
				}
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex, Resources.ErrorInvalidXSDFile.Format(xsd));
			}
			return false;
		}
		/// <summary>
		/// Validate a XML message
		/// </summary>
		/// <param name="xml">XML message to validate</param>
		/// <returns>The XML message if valid, an empty string if not. Check properties to determine why</returns>
		public string ValidateXML(string xml)
		{
			XDocument x = XDocument.Parse(xml);
			try
			{
				//x.Validate(XmlSchemaSet, SchemaValidationHandler);
				x.Validate(XmlSchemaSet, null);
				return x.ToString();
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex, xml);
			}
			return string.Empty;
		}
		#endregion
	}

	public static class ISO20022CoreExtensions
	{
		public static string ValidateXML(this ISO20022Core iso, string xml) => iso.XmlValider.ValidateXML(xml);
	}

	/// <summary>
	/// base class for any nexo object
	/// </summary>
	public abstract class ISO20022Core
	{
		#region constructors
		protected ISO20022Core() { }
		protected ISO20022Core(string version, int year) { ProtocolVersion = version; ProtocolYear = year; }
		#endregion

		#region internal classes
		protected class UTF8StringWriter : StringWriter
		{
			bool BOM;
			public UTF8StringWriter(bool bom) { BOM = bom; }
			public override Encoding Encoding => new UTF8Encoding(BOM);
		}
		#endregion

		#region properties
		/// <summary>
		/// indicates if JSON notation must be used (true) or not (false, in that case XML notation is used)
		/// </summary>
		public static bool UseJSON { get; set; } = true;
		/// <summary>
		/// indicates whether a validation of the messages must be done against any available schema before sending it
		/// </summary>
		public bool UseValidation { get; set; } = true;
		/// <summary>
		/// version of the protocol being used
		/// </summary>
		public static string ProtocolVersion { get; protected set; } = string.Empty;
		/// <summary>
		/// year of issuance of the protocol
		/// </summary>
		public static int ProtocolYear { get; set; } = 0;
		#endregion

		#region XSD management properties
		/// <summary>
		/// Events during XSD validation
		/// </summary>
		public XmlSchemaValider XmlValider { get; set; } = default;
		/// <summary>
		/// Indicates whether a message can be used or not if errors were reported after having applied XSD
		/// </summary>
		public bool XmlUseWithErrors { get; set; } = false;
		/// <summary>
		/// Indicates whether a message can be used or not if warnings were reported after having applied XSD
		/// </summary>
		public bool XmlUseWithWarnings { get; set; } = true;
		#endregion

		#region protected methods
		protected virtual object GetValue(object currentValue) => currentValue;
		protected virtual object SetValue(object currentValue, object newValue) => newValue;
		#endregion

		#region static public methods
		/// <summary>
		/// Generic serializer
		/// </summary>
		/// <typeparam name="NxT">the class type to serialize</typeparam>
		/// <param name="data">the object to serialize</param>
		/// <param name="toJSON">if true serialisation will produce json string (default), xml string if false</param>
		/// <param name="bom">[XML only] true if BOM must be added, false otherwise (default)</param>
		/// <param name="ns">[XML only] true if namespace must be added, false otherwise (default)</param>
		/// <returns>a serialized string or an empty string if an error occurred</returns>
		public static string Serialize<NxT>(NxT data, bool toJSON = true, bool bom = false, bool ns = false)
		{
			if (null == data)
			{
				CLog.WARNING(Resources.ErrorNoDataToSerialize.Format(typeof(NxT).Name));
				return null;
			}

			try
			{
				if (toJSON)
				{
					return JsonConvert.SerializeObject(data,
						Newtonsoft.Json.Formatting.None,
						new JsonSerializerSettings()
						{
							MissingMemberHandling = MissingMemberHandling.Ignore,
							NullValueHandling = NullValueHandling.Ignore
						});
				}
				else
				{
					// remove version
					XmlWriterSettings settings = new XmlWriterSettings();
					settings.Indent = false;
					settings.CloseOutput = true;
					settings.OmitXmlDeclaration = true;

					XmlSerializer xsSubmit = new XmlSerializer(typeof(NxT));
					using (StringWriter sw = new UTF8StringWriter(bom))
					using (XmlWriter writer = XmlWriter.Create(sw, settings))
					{
						var xmlns = new XmlSerializerNamespaces();
						// removes namespace if requested
						if (!ns)
							xmlns.Add(string.Empty, string.Empty);
						// serialize
						xsSubmit.Serialize(writer, data, xmlns);
						return sw.ToString();
					}
				}
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
				return null;
			}
		}
		/// <summary>
		/// Generic deserializer
		/// </summary>
		/// <typeparam name="NxT">the class type to serialize</typeparam>
		/// <param name="isJSON">if true deserialisation will be made from json string, from xml string if false</param>
		/// <param name="data">the object to deserialize</param>
		/// <param name="bom">[XML only] true if BOM must be used, false otherwise (default)</param>
		/// <returns>a deserialized object or null if an error occurred</returns>
		public static NxT Deserialize<NxT>(string data, bool isJSON = true, bool bom = false)
		{
			if (data.IsNullOrEmpty(true))
			{
				CLog.WARNING(Resources.ErrorNoDataToDeserialize.Format(typeof(NxT).Name));
				return default(NxT);
			}

			try
			{
				if (isJSON)
				{
					return JsonConvert.DeserializeObject<NxT>(data,
						new JsonSerializerSettings()
						{
							MissingMemberHandling = MissingMemberHandling.Ignore,
							NullValueHandling = NullValueHandling.Ignore
						});
				}
				else
				{
					// remove version
					XmlReaderSettings settings = new XmlReaderSettings();
					settings.IgnoreComments = true;
					settings.IgnoreProcessingInstructions = true;
					settings.IgnoreWhitespace = true;
					settings.CloseInput = true;

					XmlSerializer xsSubmit = new XmlSerializer(typeof(NxT));
					using (StreamReader stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(data)), Encoding.UTF8, bom))
					using (XmlReader reader = XmlReader.Create(stream, settings))
						try
						{
							return (NxT)xsSubmit.Deserialize(reader);
						}
						catch (Exception ex)
						{
							// no specific processing as we may not be processing the requested class thus generating an exception, in this case just return null
							CLog.EXCEPT(ex);
						}
				}
			}
			catch (Exception ex)
			{
				CLog.EXCEPT(ex);
			}
			return default(NxT);
		}
		/// <summary>
		/// <see cref="Deserialize{NxT}(string, bool, bool)"/>
		/// </summary>
		/// <typeparam name="NxT"></typeparam>
		/// <param name="data"></param>
		/// <param name="isJSON"></param>
		/// <param name="bom"></param>
		/// <returns></returns>
		public static NxT Deserialize<NxT>(byte[] data, bool isJSON = true, bool bom = false) => Deserialize<NxT>(Encoding.UTF8.GetString(data), isJSON, bom);
		/// <summary>
		/// Returns the real <see cref="Type"/> of the object stored in a System.Object
		/// </summary>
		/// <param name="o">object to analyze</param>
		/// <returns>The real <see cref="Type"/> of the object stored inside a System.Object, null otherwise</returns>
		public static Type GetRealObjectType(object o)
		{
			if (default == o) return null;

			Func<Type, bool> IsArray = (Type xtype) => { return xtype.IsArray; };
			Func<Type, bool> IsSystemType = (Type xtype) => { return (0 == string.Compare("system", xtype.Namespace, true)); };
			Func<Type, bool> IsSystemObject = (Type xtype) => { return IsSystemType(xtype) && (0 == string.Compare("object", xtype.Name, true)); };

			// get the type of the property to check
			Type type = o.GetType();
			if (IsSystemObject(type))
			{
				object obj = o;
				type = obj.GetType();
			}
			return type;
		}
	}
	#endregion
}
