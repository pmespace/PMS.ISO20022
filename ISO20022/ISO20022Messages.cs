using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using COMMON;
using ISO20022.Properties;

namespace ISO20022
{
	#region exceptions
	/// <summary>
	/// excaption raised when an invalid value is assigned to an enumerated variable
	/// </summary>
	public class ISO20022MessageNoGenericConstructorException : Exception
	{
		public ISO20022MessageNoGenericConstructorException(Type t) : base(Resources.ExceptionMessageNoGenericConstructor.Format(t.Name)) { }
	}

	/// <summary>
	/// excaption raised when an invalid value is assigned to an enumerated variable
	/// </summary>
	public class ISO20022MessageNoDocumentCreatedException : Exception
	{
		public ISO20022MessageNoDocumentCreatedException(Type t) : base(Resources.ExceptionMessageNoDocumentCreated.Format(t.Name)) { }
	}
	#endregion

	#region generic ISO20022 messages
	/// <summary>
	/// Describes an ISO20022 generic message
	/// </summary>
	/// <typeparam name="TDocument">Type of Document object (usually Document)</typeparam>
	/// <typeparam name="TMessage">Type of Message object inside the Document</typeparam>
	public abstract class ISO20022Message<TDocument, TMessage> : ISO20022Core
	{
		#region constructors
		public ISO20022Message() { Initialize(); }
		void Initialize()
		{
#if OLD_SERIALIZE
			// create generic serializer and deserializer for this nexo object
			MethodInfo method = typeof(NexoCore).GetMethod(nameof(NexoCore.Serialize), BindingFlags.Static | BindingFlags.Public);
			genericSerializer = method.MakeGenericMethod(typeof(TDocument));
			method = typeof(NexoCore).GetMethod(nameof(NexoCore.Deserialize), BindingFlags.Static | BindingFlags.Public);
			genericDeserializer = method.MakeGenericMethod(typeof(TDocument));
#endif
			// find generic constructor
			ConstructorInfo constructor = typeof(TDocument).GetConstructor(Type.EmptyTypes);
			if (null == constructor) throw new ISO20022MessageNoGenericConstructorException(typeof(TDocument));
			// arrived here a generic constructor has been found, let's create the Document object
			Document = (TDocument)constructor.Invoke(null);
			if (null == Document) throw new ISO20022MessageNoDocumentCreatedException(typeof(TDocument));
		}
		#endregion

		#region properties
		/// <summary>
		/// Main Document object
		/// </summary>
		public TDocument Document { get => _document; set => _document = (typeof(TDocument) == value.GetType() ? value : default /*_document*/); }
		TDocument _document = default;
		/// <summary>
		/// Message specific object contained within the <see cref="Document"/>
		/// </summary>
		public TMessage Message { get => null == Document ? default : (TMessage)getMessage(); }
		protected abstract object getMessage();
		#endregion

		#region public methods
		/// <summary>
		/// Serialize the message from the <see cref="Document"/> object
		/// </summary>
		/// <param name="toJSON">true if serialization must be made in JSON, false if XML, default is true</param>
		/// <returns>The serialized object as s string if successful, an empty string otherwise</returns>
		public string Serialize(bool toJSON = true) => Serialize<TDocument>(Document, toJSON);
		/// <summary>
		/// Deserialize a string inside an ISO20022 message, settings the <see cref="Document"/> object
		/// </summary>
		/// <param name="document">the string to deserialize</param>
		/// <param name="isJSON">true if deserialization must be made from JSON, false if from XML, default is true</param>
		/// <returns>true if the process succeeded, false otherwise</returns>
		public bool Deserialize(string document, bool isJSON = true) => null != (Document = Deserialize<TDocument>(document, isJSON));
		#endregion

		#region private methods
		#endregion
	}
	#endregion
}
