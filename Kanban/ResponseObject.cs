using System;

namespace Inedo.BuildMasterExtensions.LeanKit.Kanban
{
    internal sealed class ResponseObject<TData>
    {
        public ResponseObject(object parsedJson)
        {
            if (parsedJson == null)
                throw new ArgumentNullException("parsedJson");

            var obj = parsedJson as JavaScriptObject;
            if (obj == null)
                throw new ArgumentException("Expected object as response; got " + obj.GetType().Name + " instead.");

            object replyCode;
            if (!obj.TryGetValue("ReplyCode", out replyCode))
                throw new ArgumentException("ReplyCode property was not found in the response.");

            this.ReplyCode = (ReplyCode)Convert.ToInt32(replyCode);

            object replyText;
            obj.TryGetValue("ReplyText", out replyText);
            this.ReplyText = (replyText ?? string.Empty).ToString();

            object replyData;
            if (obj.TryGetValue("ReplyData", out replyData) && replyData != null)
            {
                if (replyData is TData)
                    this.ReplyData = (TData)replyData;
                else
                {
                    try
                    {
                        this.ReplyData = (TData)Convert.ChangeType(replyData, typeof(TData));
                    }
                    catch
                    {
                        if (this.ReplyCode == ReplyCode.DataRetrievalSuccess)
                            throw new ArgumentException("Expected response data type " + typeof(TData).Name + "; got " + replyData.GetType().Name + " instead.");
                    }
                }
            }
        }

        public ReplyCode ReplyCode { get; private set; }
        public string ReplyText { get; private set; }
        public TData ReplyData { get; private set; }
    }
}
