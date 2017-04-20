﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace _1SCodeAnalyze{

	public class КоманднаяСтрока{

		public static КоманднаяСтрока singletone = null;
		public static bool isAnalyzeCode = true;
		public static bool isCreateLogFile = true;
		public static bool isCountStrings = true;
		public static bool isSearchChainCalls = false;
		public static string ext1 = "";
		public static String ИмяПапки;

		public КоманднаяСтрока (string [] args)
		{
			ИмяПапки = AppDomain.CurrentDomain.BaseDirectory;
			AnalyzeCmdString (args);
		}

		void AnalyzeCmdString (string[] args)
		{
			if (args.Count () == 0) {
				System.Console.WriteLine ("cmd keys: -e ext, -noanal, -chaincall");
				return;
			}
			string ключ = args[0];
			
			switch (ключ) {
			case "-e":
				ext1 = args[1];
				System.Console.WriteLine ("расширение "+ext1);
				break;
				
			case "-noanal":
				isAnalyzeCode = false;
				break;

			case "-chaincall":
				isAnalyzeCode = false;
				break;

			case "":

				break;
				
			default:
				ИмяПапки = ключ;
				break;
			}
		}
	}
}

