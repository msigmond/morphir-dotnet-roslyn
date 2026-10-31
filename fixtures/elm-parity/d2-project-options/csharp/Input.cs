namespace Example.CurrentSupported;public static partial class CurrentSupportedExample{public static int ConfigurationValue()=>
#if DEBUG
1;
#else
2;
#endif
public static int SharedValue(int value)=>Example.Parity.SharedRules.Double(value);}
