package mono.android.app;

public class ApplicationRegistration {

	public static void registerApplications ()
	{
				// Application and Instrumentation ACWs must be registered first.
		mono.android.Runtime.register ("cut_picture_maui.MainApplication, cut-picture-maui, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", crc64cf37e2b0c17b4d7c.MainApplication.class, crc64cf37e2b0c17b4d7c.MainApplication.__md_methods);
		mono.android.Runtime.register ("Microsoft.Maui.MauiApplication, Microsoft.Maui, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", crc6488302ad6e9e4df1a.MauiApplication.class, crc6488302ad6e9e4df1a.MauiApplication.__md_methods);
		
	}
}
