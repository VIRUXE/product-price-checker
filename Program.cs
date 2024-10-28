namespace ProductPriceChecker;

static class Program {
	[STAThread]
	static void Main() {
		SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
		ApplicationConfiguration.Initialize();
		Application.Run(new MainForm());
	}
}
