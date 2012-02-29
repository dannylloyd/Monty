using System;
using SiteMonitorService;
using System.Net.NetworkInformation;

namespace ConsoleApplication2
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Class1
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			SiteMonitorService.Engine eng = null;
            
			try
			{
				eng = new SiteMonitorService.Engine();
                eng.Settings.siteUrls = new System.Collections.Generic.List<string>();

                //TODO add your own settings

				eng.CheckWebPages();

				if (System.Diagnostics.Debugger.IsAttached) // when debugger attached, we need to wait for the user
				{
					Console.WriteLine("This pause allows the async processes (like IM) to work\r\n");
					// and you the user time to see what's up (when running the debugger)
				
					Console.WriteLine("Press ENTER key to continue.");
					Console.ReadLine();
				} 
				else // if debugger is not attached, we can wait a quick sec before quitting :)
				{
					// :) sleep works just as well :)
					System.Threading.Thread.Sleep(4000);
				}
			}			
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				Console.WriteLine("Press ENTER key to continue.");
				Console.ReadLine();
			}

			try
			{
				eng.EndMonitor(); // clean up messenger stuff especially
			} 
			catch {}
		}
	}
}
