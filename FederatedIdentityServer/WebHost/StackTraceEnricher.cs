using System;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Text;
using System.Threading;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace WebHost
{
    public partial class StackTraceEnricher : ILogEventEnricher
    {
        public const string UnknownPropertyValue = "?";

        [Flags]
        public enum Properties
        {
            None = 0,
            AssemblyName = 1,
            ClassName = 2,
            MethodName = 4,
            ILOffset = 8,
            NativeOffset = 16,
            FileName = 32,
            LineNumber = 64,
            ColumnNumber = 128,
            StackTrace = 256,

            Caller = ClassName | MethodName,
            Source = FileName | LineNumber | ColumnNumber,
            CallerAndSource = Caller | Source,

            Default = Caller
        }

        public static class PropertyNames
        {
            public const string StackTraceDepth = "StackTraceDepth";
            public const string AssemblyName = "AssemblyName";
            public const string AssemblyFullName = "AssemblyFullName";
            public const string ClassName = "ClassName";
            public const string MethodName = "MethodName";
            public const string ILOffset = "ILOffset";
            public const string NativeOffset = "NativeOffset";
            public const string FileName = "FileName";
            public const string LineNumber = "LineNumber";
            public const string ColumnNumber = "ColumnNumber";
            public const string StackTrace = "StackTrace";
            public const string StackTraceDetail = "StackTraceDetail";
        }
    }

    public partial class StackTraceEnricher : ILogEventEnricher
    {
        public Properties EnricheWithProperties
        {
            get { return _properties; }
        }

        public LogEventLevel MinimumLogEventLevelToEnrich
        {
            get { return _minimumLogEventLevelToEnrich; }
        }

        public int StackTraceDepth
        {
            get { return _stackTraceDepth; }
        }

        internal Type CallerStackBoundaryDeclaringType
        {
            get { return _callerStackBoundaryDeclaringType.Value; }
        }

        public StackTraceEnricher(Properties properties = Properties.Default,
            LogEventLevel minimumLogEventLevelToEnrich = LogEventLevel.Verbose, int stackTraceDepth = 1,
            Type callerStackBoundaryDeclaringType = null)
        {
            if (stackTraceDepth < 1) throw new ArgumentOutOfRangeException("stackTraceDepth", stackTraceDepth, "stackTraceDepth must be at least 1");

            _properties = Fixup(properties);
            _minimumLogEventLevelToEnrich = minimumLogEventLevelToEnrich;
            _stackTraceDepth = stackTraceDepth;
            _nullLocationInfos = new LocationInfo[_stackTraceDepth];

            _needFileInfo = _properties.HasFlag(Properties.FileName);

            if (callerStackBoundaryDeclaringType != null)
            {
                SetCallerStackBoundaryDeclaringType(callerStackBoundaryDeclaringType);
            }
            else
            {
                _callerStackBoundaryDeclaringType = new Lazy<Type>(InferCallerStackBoundaryDeclaringType,
                    LazyThreadSafetyMode.PublicationOnly);
            }
        }

        public void SetCallerStackBoundaryDeclaringType(Type callerStackBoundaryDeclaringType)
        {
            _callerStackBoundaryDeclaringType = new Lazy<Type>(() => callerStackBoundaryDeclaringType,
                LazyThreadSafetyMode.PublicationOnly);
#pragma warning disable 168
            var forceValue = _callerStackBoundaryDeclaringType.Value;
#pragma warning restore 168
        }

        public void SetCallerStackBoundaryDeclaringTypeFromLogger(ILogger logger)
        {
            SetCallerStackBoundaryDeclaringType(logger.GetType());
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent == null) throw new ArgumentNullException("logEvent");
            if (propertyFactory == null) throw new ArgumentNullException("propertyFactory");

            var callerStackBoundaryDeclaringType = _callerStackBoundaryDeclaringType.Value;
            bool enrich = logEvent.Level >= _minimumLogEventLevelToEnrich;

            LocationInfo[] locationInfos = null;

            if (enrich)
            {
                try
                {
                    var stackTrace = new StackTrace(_needFileInfo);
                    int frameIndex = 0;

                    while (frameIndex < stackTrace.FrameCount)
                    {
                        StackFrame frame = stackTrace.GetFrame(frameIndex);
                        if (frame != null && frame.GetMethod().DeclaringType == callerStackBoundaryDeclaringType)
                        {
                            break;
                        }
                        frameIndex++;
                    }

                    while (frameIndex < stackTrace.FrameCount)
                    {
                        StackFrame frame = stackTrace.GetFrame(frameIndex);
                        if (frame != null && frame.GetMethod().DeclaringType != callerStackBoundaryDeclaringType)
                        {
                            break;
                        }
                        frameIndex++;
                    }

                    if (frameIndex < stackTrace.FrameCount)
                    {
                        int adjustedFrameCount = Math.Min(stackTrace.FrameCount - frameIndex, _stackTraceDepth);
                        locationInfos = new LocationInfo[adjustedFrameCount];
                        for (int i = 0; i < adjustedFrameCount; i++)
                        {
                            locationInfos[i] = new LocationInfo(stackTrace.GetFrame(i + frameIndex), _needFileInfo);
                        }
                    }
                }
                catch (SecurityException e)
                {
                    SelfLog.WriteLine(String.Format("{0} failed to enrich with stack trace because of security error {1}",
                        GetType().Name, e.Message));
                }
            }

            if (locationInfos == null)
            {
                locationInfos = _nullLocationInfos;
            }

            var stackTraceDepth = locationInfos.Length;
            for (int i = 0; i < stackTraceDepth; i++)
            {
                AddStackTracePropertiesToLogEvent(logEvent, _properties, locationInfos[i], i);
            }

            if (_properties.HasFlag(Properties.StackTrace))
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty(PropertyNames.StackTrace,
                    new ScalarValue(enrich ? GetStackTracePropertyValue(locationInfos, stackTraceDepth) : UnknownPropertyValue)));
            }

            logEvent.AddPropertyIfAbsent(new LogEventProperty(PropertyNames.StackTraceDepth,
                new ScalarValue(enrich ? (object)locationInfos.Length : UnknownPropertyValue)));
        }

        private static Properties Fixup(Properties properties)
        {
            if (properties.HasFlag(Properties.ColumnNumber))
            {
                properties |= Properties.LineNumber;
            }

            if (properties.HasFlag(Properties.LineNumber))
            {
                properties |= Properties.FileName;
            }

            return properties;
        }

        private static void AddStackTracePropertiesToLogEvent(LogEvent logEvent, Properties properties,
            LocationInfo locationInfo, int depth)
        {
            if (properties.HasFlag(Properties.AssemblyName))
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty(GetPropertyForDepth(PropertyNames.AssemblyName, depth),
                    new ScalarValue((locationInfo != null) ? (locationInfo.AssemblyName ?? UnknownPropertyValue) : UnknownPropertyValue)));
                logEvent.AddPropertyIfAbsent(new LogEventProperty(GetPropertyForDepth(PropertyNames.AssemblyFullName, depth),
                    new ScalarValue((locationInfo != null) ? (locationInfo.AssemblyFullName ?? UnknownPropertyValue) : UnknownPropertyValue)));
            }
            if (properties.HasFlag(Properties.ClassName))
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty(GetPropertyForDepth(PropertyNames.ClassName, depth),
                    new ScalarValue((locationInfo != null) ? (locationInfo.ClassName ?? UnknownPropertyValue) : UnknownPropertyValue)));
            }
            if (properties.HasFlag(Properties.MethodName))
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty(GetPropertyForDepth(PropertyNames.MethodName, depth),
                    new ScalarValue((locationInfo != null) ? (locationInfo.MethodName ?? UnknownPropertyValue) : UnknownPropertyValue)));
            }
            if (properties.HasFlag(Properties.ILOffset))
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty(GetPropertyForDepth(PropertyNames.ILOffset, depth),
                    new ScalarValue((locationInfo != null) ? (object)(locationInfo.ILOffset) : UnknownPropertyValue)));
            }
            if (properties.HasFlag(Properties.NativeOffset))
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty(GetPropertyForDepth(PropertyNames.NativeOffset, depth),
                    new ScalarValue((locationInfo != null) ? (object)(locationInfo.NativeOffset) : UnknownPropertyValue)));
            }
            if (properties.HasFlag(Properties.FileName))
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty(GetPropertyForDepth(PropertyNames.FileName, depth),
                    new ScalarValue((locationInfo != null) ? (locationInfo.FileName ?? UnknownPropertyValue) : UnknownPropertyValue)));
            }
            if (properties.HasFlag(Properties.LineNumber))
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty(GetPropertyForDepth(PropertyNames.LineNumber, depth),
                    new ScalarValue((locationInfo != null) ? (object)(locationInfo.LineNumber) : UnknownPropertyValue)));
            }
            if (properties.HasFlag(Properties.ColumnNumber))
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty(GetPropertyForDepth(PropertyNames.ColumnNumber, depth),
                    new ScalarValue((locationInfo != null) ? (object)(locationInfo.ColumnNumber) : UnknownPropertyValue)));
            }
        }

        public static string GetPropertyForDepth(string propertyNameBase, int depth)
        {
            if (depth == 0) return propertyNameBase;
            return String.Format(CultureInfo.InvariantCulture, "{0}_{1}", propertyNameBase, depth);
        }

        private string GetStackTracePropertyValue(LocationInfo[] locationInfos, int stackTraceDepth)
        {
            var sb = new StringBuilder(stackTraceDepth * 50);
            for (int i = stackTraceDepth - 1; i >= 0; i--)
            {
                var locationInfo = locationInfos[i];
                sb.Append(locationInfo.ClassName).Append('.').Append(locationInfo.MethodName);
                if (i > 0)
                {
                    sb.Append(" > ");
                }
            }
            return sb.ToString();
        }

        private static Type InferCallerStackBoundaryDeclaringType()
        {
            Type inferedCallerStackBoundaryDeclaringType = Log.Logger.GetType();
            return inferedCallerStackBoundaryDeclaringType;
        }

        private Lazy<Type> _callerStackBoundaryDeclaringType;
        private readonly bool _needFileInfo;

        private readonly Properties _properties = Properties.Caller;
        private readonly LogEventLevel _minimumLogEventLevelToEnrich;
        private readonly int _stackTraceDepth = 1;
        private readonly LocationInfo[] _nullLocationInfos;
    }
}