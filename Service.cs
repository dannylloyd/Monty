using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Configuration.Install;
using System.Reflection;
using System.Windows.Forms;

namespace SiteMonitorService
{
	public class SiteMonitor : System.ServiceProcess.ServiceBase
	{
		private System.ComponentModel.Container components = null;
		private SiteMonitorService.Engine _engine;

		public SiteMonitor()
		{			 
			InitializeComponent();		
		}
		
		static void Main(string[] args)
		{ 
			string opt=null;	 
			if(args.Length >0 )
			{ 
				opt=args[0];
			}

			if(opt!=null && opt.ToLower()=="/install")
			{
				TransactedInstaller ti= new TransactedInstaller();
				MyInstaller pi = new MyInstaller();
				ti.Installers.Add(pi);
				String path=String.Format("/assemblypath={0}",
					System.Reflection.Assembly.GetExecutingAssembly().Location);
				String[] cmdline={path};
				InstallContext ctx = new InstallContext("",cmdline);			 
				ti.Context =ctx;				 		
				ti.Install(new Hashtable());
			}
			else if (opt !=null && opt.ToLower()=="/uninstall")
			{
				TransactedInstaller ti=new TransactedInstaller();
				MyInstaller mi=new MyInstaller();
				ti.Installers.Add(mi);
				String path = String.Format("/assemblypath={0}",
					System.Reflection.Assembly.GetExecutingAssembly().Location);
				String[] cmdline={path};
				InstallContext ctx = new InstallContext("",cmdline);
				ti.Context=ctx;
				ti.Uninstall(null);
			 
			}				

			if(opt==null)  // e.g. ,nothing on the command line
			{
				System.ServiceProcess.ServiceBase[] ServicesToRun;
				ServicesToRun = new System.ServiceProcess.ServiceBase[] { new SiteMonitor() };
				System.ServiceProcess.ServiceBase.Run(ServicesToRun);
			}
		}
		
		private void InitializeComponent()
		{
		 	this._engine = new Engine();			
			this.CanHandlePowerEvent = true;
			this.CanPauseAndContinue = true;
			this.CanShutdown = true;
			this.ServiceName = "SiteMonitor";
		}

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}		
		protected override void OnStart(string[] args)		
		{
			// Debugger.Launch(); // use this to help debug - attach to a running service	
		    _engine.BeginMonitor();		 
			EventLog.WriteEntry("SiteMonitorService starting. " );
		}	
		protected override void OnStop()
		{
			_engine.EndMonitor();
		}
	}
}