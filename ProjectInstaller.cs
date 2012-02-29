using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
namespace SiteMonitorService
{	
	[RunInstaller(true)]
	public class MyInstaller : Installer
	{ 
		public MyInstaller()
		{
			ServiceProcessInstaller spi=new ServiceProcessInstaller();
			spi.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
			spi.Password = null;
			spi.Username = null;
			ServiceInstaller si = new ServiceInstaller();
			si.ServiceName="SiteMonitor";
			this.Installers.Add(spi);
			this.Installers.Add(si);			 
		}	 
	}
}
